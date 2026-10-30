using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using KeepMeAlive.Models;
using KeepMeAlive.Services;
using KeepMeAlive.Tests.Accounts;

namespace KeepMeAlive.Tests.Persistence;

/// <summary>Verifies that logout clears its own credentials without altering unrelated installations or license recovery.</summary>
public sealed class SessionIsolationTests
{
    private const StorageMode Portable = StorageMode.PortableLocal;
    private const StorageMode Profile = StorageMode.ProfileAppData;

    /// <summary>Checks that a fresh portable logout cannot erase profile credentials from another installation.</summary>
    [Fact]
    public void FreshPortableLogoutPreservesEveryUnrelatedProfileFile()
    {
        using var fixture = new SessionStoreFixture();
        PopulateForeignProfile(fixture);
        var original = fixture.Snapshot(Profile);
        fixture.Store.DeleteSession();
        AssertUnchanged(original);
    }

    /// <summary>Checks that distinct account grants remain separate even when their endpoints are identical.</summary>
    [Fact]
    public void PortableLogoutPreservesForeignProfilePrimaryBackupAndRecoveryBytes()
    {
        using var fixture = new SessionStoreFixture();
        fixture.Store.SaveSession(SessionStoreFixture.Session("owned-token", "owned-family"));
        PopulateForeignProfile(fixture);
        var original = fixture.Snapshot(Profile);
        fixture.Store.DeleteSession();
        Assert.False(File.Exists(fixture.SessionPath(Portable)));
        AssertUnchanged(original);
    }

    /// <summary>Checks independent ownership of primary, backup, and recovery records after refresh rotation.</summary>
    [Fact]
    public void OwnedCopyDeletionPreservesForeignBackupAndRedactsOnlyOwnedRecovery()
    {
        using var fixture = new SessionStoreFixture();
        var old = SessionStoreFixture.Session("old-token", "owned-family");
        fixture.Store.SaveSession(old);
        fixture.Store.SaveSession(SessionStoreFixture.Session("rotated-token", "owned-family"));
        fixture.Write(fixture.SessionPath(Profile), old);
        fixture.Write(fixture.SessionPath(Profile, true), SessionStoreFixture.Session("foreign-token", "foreign-family"));
        fixture.WriteRecovery(Profile, old);
        fixture.WriteRecovery(Profile, SessionStoreFixture.Session("foreign-recovery-token", "foreign-family"), backup: true);
        var foreignBackup = File.ReadAllBytes(fixture.SessionPath(Profile, true));
        var foreignRecovery = File.ReadAllBytes(fixture.RecoveryPath(Profile, true));
        fixture.Store.DeleteSession();
        Assert.False(File.Exists(fixture.SessionPath(Portable)));
        Assert.False(File.Exists(fixture.SessionPath(Portable, true)));
        Assert.False(File.Exists(fixture.SessionPath(Profile)));
        Assert.Equal(foreignBackup, File.ReadAllBytes(fixture.SessionPath(Profile, true)));
        Assert.Equal(foreignRecovery, File.ReadAllBytes(fixture.RecoveryPath(Profile, true)));
        AssertLicenseOnlyRecovery(fixture, Profile);
    }

    /// <summary>Checks that a matching backup is removed independently of a foreign primary.</summary>
    [Fact]
    public void OwnedBackupIsRemovedWithoutChangingForeignPrimary()
    {
        using var fixture = new SessionStoreFixture();
        var owned = SessionStoreFixture.Session("owned-token", "owned-family");
        fixture.Store.SaveSession(owned);
        fixture.Write(fixture.SessionPath(Profile), SessionStoreFixture.Session("foreign-token", "foreign-family"));
        fixture.Write(fixture.SessionPath(Profile, true), owned);
        var primary = File.ReadAllBytes(fixture.SessionPath(Profile));
        fixture.Store.DeleteSession();
        Assert.Equal(primary, File.ReadAllBytes(fixture.SessionPath(Profile)));
        Assert.False(File.Exists(fixture.SessionPath(Profile, true)));
    }

    /// <summary>Checks that recovery redaction preserves license data and creates no backup retaining the token.</summary>
    [Fact]
    public void RecoveryRedactionKeepsLicensesAndDoesNotCreateTokenBackups()
    {
        using var fixture = new SessionStoreFixture();
        var owned = SessionStoreFixture.Session("owned-token", "owned-family");
        fixture.Store.SaveSession(owned);
        fixture.WriteRecovery(Portable, owned);
        fixture.WriteRecovery(Portable, owned, backup: true);
        fixture.WriteRecovery(Profile, owned);
        fixture.Store.DeleteSession();
        AssertLicenseOnlyRecovery(fixture, Portable);
        AssertLicenseOnlyRecovery(fixture, Portable, backup: true);
        AssertLicenseOnlyRecovery(fixture, Profile);
        Assert.False(File.Exists(fixture.RecoveryPath(Profile, true)));
    }

    /// <summary>Checks that a recovery-file write failure cannot retain the active refresh credential or its backup.</summary>
    [Fact]
    public void UnwritableRecoveryCannotRetainActiveRefreshBackup()
    {
        using var fixture = new SessionStoreFixture();
        var owned = SessionStoreFixture.Session("owned-token", "owned-family");
        fixture.Store.SaveSession(owned);
        fixture.Store.SaveSession(SessionStoreFixture.Session("rotated-token", "owned-family"));
        fixture.WriteRecovery(Portable, owned);
        var recoveryPath = fixture.RecoveryPath(Portable);
        File.SetAttributes(recoveryPath, FileAttributes.ReadOnly);
        try
        {
            var failure = Record.Exception(fixture.Store.DeleteSession);
            Assert.True(failure is IOException or UnauthorizedAccessException);
            Assert.False(File.Exists(fixture.SessionPath(Portable)));
            Assert.False(File.Exists(fixture.SessionPath(Portable, true)));
        }
        finally { File.SetAttributes(recoveryPath, FileAttributes.Normal); }
    }

    /// <summary>Checks that legacy migrated copies remain identifiable after the first upgraded token rotation.</summary>
    [Fact]
    public async Task LegacyMigratedCopiesAreRemovedAfterRefreshTokenRotation()
    {
        using var fixture = new SessionStoreFixture();
        var legacy = SessionStoreFixture.Session("initial-refresh-token");
        fixture.Store.SaveSession(legacy);
        fixture.Write(fixture.SessionPath(Profile), legacy);
        fixture.WriteRecovery(Profile, legacy);
        using var http = new HttpClient(new AccountHttpHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("oauth-authorization-server", StringComparison.Ordinal))
            {
                return Json(new { issuer = "https://auth.example.test/neondb/auth", authorization_endpoint = "https://auth.example.test/neondb/auth/authorize", token_endpoint = "https://auth.example.test/neondb/auth/token" });
            }
            return request.Method == HttpMethod.Post
                ? Json(new { access_token = "access-token", refresh_token = "rotated-token", expires_in = 3600, token_type = "Bearer" })
                : Json(new { userId = "test-user" });
        }));
        using var service = new AuthenticationService(new NeonOptions
        {
            AuthIssuer = "https://auth.example.test/neondb/auth", ApiBaseUrl = "https://api.example.test/", ClientId = "desktop", Resource = "https://keepmealive.app/api",
        }, http, fixture.Store, new TestBrowser(), new TestCallbackListener());
        Assert.Equal("access-token", await service.GetAccessTokenAsync());
        using (var session = JsonDocument.Parse(fixture.Store.GetSession()))
        {
            Assert.NotEmpty(session.RootElement.GetProperty("SessionId").GetString()!);
            Assert.Equal("rotated-token", session.RootElement.GetProperty("RefreshToken").GetString());
        }
        fixture.Store.DeleteSession();
        Assert.False(File.Exists(fixture.SessionPath(Profile)));
        AssertLicenseOnlyRecovery(fixture, Profile);
    }

    /// <summary>Checks that corrupt active material is cleared without guessing ownership of profile files.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CorruptActiveSessionDoesNotDeleteUnrelatedProfile(bool corruptProtection)
    {
        using var fixture = new SessionStoreFixture();
        fixture.Store.SaveSession("malformed-session-json");
        fixture.Store.SaveSession("malformed-session-json-again");
        if (corruptProtection) { File.WriteAllBytes(fixture.SessionPath(Portable), [1, 2, 3]); }
        PopulateForeignProfile(fixture);
        var original = fixture.Snapshot(Profile);
        fixture.Store.DeleteSession();
        Assert.False(File.Exists(fixture.SessionPath(Portable)));
        Assert.False(File.Exists(fixture.SessionPath(Portable, true)));
        AssertUnchanged(original);
    }

    /// <summary>Checks that an unreadable foreign primary or recovery cannot block cleanup or cause broader deletion.</summary>
    [Fact]
    public void CorruptForeignFilesArePreservedWhileOwnedBackupIsRemoved()
    {
        using var fixture = new SessionStoreFixture();
        var owned = SessionStoreFixture.Session("owned-token", "owned-family");
        fixture.Store.SaveSession(owned);
        fixture.Write(fixture.SessionPath(Profile), owned);
        File.WriteAllBytes(fixture.SessionPath(Profile), [1, 2, 3]);
        fixture.Write(fixture.SessionPath(Profile, true), owned);
        fixture.Write(fixture.RecoveryPath(Profile), "not-valid-recovery-json");
        var primary = File.ReadAllBytes(fixture.SessionPath(Profile));
        var recovery = File.ReadAllBytes(fixture.RecoveryPath(Profile));
        fixture.Store.DeleteSession();
        Assert.Equal(primary, File.ReadAllBytes(fixture.SessionPath(Profile)));
        Assert.Equal(recovery, File.ReadAllBytes(fixture.RecoveryPath(Profile)));
        Assert.False(File.Exists(fixture.SessionPath(Profile, true)));
    }

    /// <summary>Checks that a session-family label cannot cross a different issuer scope.</summary>
    [Fact]
    public void MatchingFamilyLabelDoesNotDeleteAnotherIssuerSession()
    {
        using var fixture = new SessionStoreFixture();
        fixture.Store.SaveSession(SessionStoreFixture.Session("owned-token", "family"));
        fixture.Write(fixture.SessionPath(Profile), SessionStoreFixture.Session("foreign-token", "family", "https://other.example.test/auth"));
        var original = fixture.Snapshot(Profile);
        fixture.Store.DeleteSession();
        AssertUnchanged(original);
    }

    private static void PopulateForeignProfile(SessionStoreFixture fixture)
    {
        fixture.Write(fixture.SessionPath(Profile), SessionStoreFixture.Session("foreign-current-token", "foreign-family"));
        fixture.Write(fixture.SessionPath(Profile, true), SessionStoreFixture.Session("foreign-old-token", "foreign-family"));
        fixture.WriteRecovery(Profile, SessionStoreFixture.Session("foreign-recovery-current-token", "foreign-family"));
        fixture.WriteRecovery(Profile, SessionStoreFixture.Session("foreign-recovery-old-token", "foreign-family"), backup: true);
    }
    private static void AssertUnchanged(Dictionary<string, byte[]> original)
    {
        foreach (var (path, bytes) in original) { Assert.Equal(bytes, File.ReadAllBytes(path)); }
    }
    private static void AssertLicenseOnlyRecovery(SessionStoreFixture fixture, StorageMode mode, bool backup = false)
    {
        using var recovered = JsonDocument.Parse(fixture.Read(fixture.RecoveryPath(mode, backup)));
        Assert.Equal("keep-this-license-recovery", recovered.RootElement.GetProperty("license").GetString());
        Assert.Equal("preserve-other-fields", recovered.RootElement.GetProperty("note").GetString());
        Assert.False(recovered.RootElement.TryGetProperty("session", out _));
    }
    private static HttpResponseMessage Json<T>(T value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };
}
