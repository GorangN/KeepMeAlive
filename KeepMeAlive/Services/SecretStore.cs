using KeepMeAlive.Models;
using KeepMeAlive.Services.Interfaces;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;

namespace KeepMeAlive.Services;

/// <summary>
/// Stores sensitive values using the secure backend that matches the selected storage mode.
/// </summary>
public sealed class SecretStore : ISecretStore
{
    private const string CredentialTargetName = "KeepMeAlive/LicenseKey";
    private static readonly byte[] PortableSecretEntropy = Encoding.UTF8.GetBytes("KeepMeAlive-Portable-Secret");

    private readonly IAppRuntimeModeService _runtimeModeService;
    private readonly ISettingsService _settingsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretStore"/> class.
    /// </summary>
    /// <param name="runtimeModeService">The runtime mode service used to resolve secure storage paths.</param>
    public SecretStore(
        IAppRuntimeModeService runtimeModeService,
        ISettingsService settingsService)
    {
        _runtimeModeService = runtimeModeService;
        _settingsService = settingsService;
    }

    /// <inheritdoc/>
    public string GetLicenseKey()
    {
        return GetLicenseKey(_settingsService.CurrentStorageMode);
    }

    /// <inheritdoc/>
    public string GetLicenseKey(StorageMode storageMode)
    {
        try
        {
            return storageMode switch
            {
                StorageMode.PortableLocal => ReadPortableLocalSecret(),
                _ => ReadCredentialSecret(),
            };
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <inheritdoc/>
    public void SaveLicenseKey(string? licenseKey)
    {
        SaveLicenseKey(licenseKey, _settingsService.CurrentStorageMode);
    }

    /// <inheritdoc/>
    public void SaveLicenseKey(string? licenseKey, StorageMode storageMode)
    {
        if (string.IsNullOrWhiteSpace(licenseKey))
        {
            DeleteLicenseKey(storageMode);
            return;
        }

        try
        {
            switch (storageMode)
            {
                case StorageMode.PortableLocal:
                    SavePortableLocalSecret(licenseKey.Trim());
                    break;
                default:
                    SaveCredentialSecret(licenseKey.Trim());
                    break;
            }
        }
        catch
        {
            // Ignore secret persistence failures so the UI can remain responsive.
        }
    }

    /// <inheritdoc/>
    public void DeleteLicenseKey(StorageMode storageMode)
    {
        try
        {
            switch (storageMode)
            {
                case StorageMode.PortableLocal:
                    DeletePortableLocalSecret();
                    break;
                default:
                    DeleteCredentialSecret();
                    break;
            }
        }
        catch
        {
            // Ignore best-effort cleanup failures.
        }
    }

    private string PortableSecretDirectory =>
        Path.Combine(_runtimeModeService.GetDataDirectory(StorageMode.PortableLocal), "Secrets");

    private string PortableSecretPath =>
        Path.Combine(PortableSecretDirectory, "license.bin");

    private string ReadPortableLocalSecret()
    {
        if (!File.Exists(PortableSecretPath))
        {
            return string.Empty;
        }

        var protectedBytes = File.ReadAllBytes(PortableSecretPath);
        var plaintextBytes = ProtectedData.Unprotect(
            protectedBytes,
            PortableSecretEntropy,
            DataProtectionScope.CurrentUser);

        return Encoding.UTF8.GetString(plaintextBytes);
    }

    private void SavePortableLocalSecret(string licenseKey)
    {
        Directory.CreateDirectory(PortableSecretDirectory);

        var plaintextBytes = Encoding.UTF8.GetBytes(licenseKey);
        var protectedBytes = ProtectedData.Protect(
            plaintextBytes,
            PortableSecretEntropy,
            DataProtectionScope.CurrentUser);

        File.WriteAllBytes(PortableSecretPath, protectedBytes);
    }

    private void DeletePortableLocalSecret()
    {
        if (File.Exists(PortableSecretPath))
        {
            File.Delete(PortableSecretPath);
        }

        if (Directory.Exists(PortableSecretDirectory)
            && !Directory.EnumerateFileSystemEntries(PortableSecretDirectory).Any())
        {
            Directory.Delete(PortableSecretDirectory);
        }
    }

    private static string ReadCredentialSecret()
    {
        var value = CredentialManager.ReadSecret(CredentialTargetName);
        return value ?? string.Empty;
    }

    private static void SaveCredentialSecret(string licenseKey)
    {
        CredentialManager.WriteSecret(CredentialTargetName, licenseKey);
    }

    private static void DeleteCredentialSecret()
    {
        CredentialManager.DeleteSecret(CredentialTargetName);
    }

    private static class CredentialManager
    {
        private const int CredentialTypeGeneric = 1;
        private const uint CredentialPersistLocalMachine = 2;
        private const int ErrorNotFound = 1168;

        public static string? ReadSecret(string targetName)
        {
            if (!CredRead(targetName, CredentialTypeGeneric, 0, out var credentialPointer))
            {
                return null;
            }

            try
            {
                var credential = Marshal.PtrToStructure<NativeCredential>(credentialPointer);
                if (credential.CredentialBlob == IntPtr.Zero || credential.CredentialBlobSize == 0)
                {
                    return null;
                }

                var bytes = new byte[credential.CredentialBlobSize];
                Marshal.Copy(credential.CredentialBlob, bytes, 0, bytes.Length);
                return Encoding.UTF8.GetString(bytes);
            }
            finally
            {
                CredFree(credentialPointer);
            }
        }

        public static void WriteSecret(string targetName, string secret)
        {
            var credentialBlob = Encoding.UTF8.GetBytes(secret);
            var blobPointer = Marshal.AllocCoTaskMem(credentialBlob.Length);

            try
            {
                Marshal.Copy(credentialBlob, 0, blobPointer, credentialBlob.Length);

                var credential = new NativeCredential
                {
                    AttributeCount = 0,
                    Attributes = IntPtr.Zero,
                    Comment = null,
                    TargetAlias = null,
                    Type = CredentialTypeGeneric,
                    Persist = CredentialPersistLocalMachine,
                    CredentialBlobSize = (uint)credentialBlob.Length,
                    TargetName = targetName,
                    CredentialBlob = blobPointer,
                    UserName = Environment.UserName
                };

                _ = CredWrite(ref credential, 0);
            }
            finally
            {
                Marshal.FreeCoTaskMem(blobPointer);
            }
        }

        public static void DeleteSecret(string targetName)
        {
            if (CredDelete(targetName, CredentialTypeGeneric, 0))
            {
                return;
            }

            if (Marshal.GetLastWin32Error() == ErrorNotFound)
            {
                return;
            }
        }

        [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredRead(
            string target,
            int type,
            int reservedFlag,
            out IntPtr credentialPtr);

        [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredWrite(
            [In] ref NativeCredential userCredential,
            uint flags);

        [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredDelete(
            string target,
            int type,
            int flags);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern void CredFree([In] IntPtr cred);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NativeCredential
        {
            public uint Flags;
            public uint Type;
            public string TargetName;
            public string? Comment;
            public FILETIME LastWritten;
            public uint CredentialBlobSize;
            public IntPtr CredentialBlob;
            public uint Persist;
            public uint AttributeCount;
            public IntPtr Attributes;
            public string? TargetAlias;
            public string UserName;
        }
    }
}
