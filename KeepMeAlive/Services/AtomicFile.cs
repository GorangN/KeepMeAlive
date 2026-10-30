using System.IO;

namespace KeepMeAlive.Services;

/// <summary>Replaces local files only after their complete contents have reached disk.</summary>
public static class AtomicFile
{
    /// <summary>Writes a file atomically and keeps the previous contents as a backup.</summary>
    /// <param name="path">The destination file.</param>
    /// <param name="contents">The complete replacement contents.</param>
    /// <exception cref="IOException">The destination could not be replaced.</exception>
    /// <exception cref="UnauthorizedAccessException">The destination cannot be written.</exception>
    public static void Write(string path, byte[] contents) => WriteCore(path, contents, preserveBackup: true);

    internal static void WriteWithoutBackup(string path, byte[] contents) => WriteCore(path, contents, preserveBackup: false);

    private static void WriteCore(string path, byte[] contents, bool preserveBackup)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(contents);
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(path))
            {
                File.Replace(temporaryPath, path, preserveBackup ? path + ".bak" : null, ignoreMetadataErrors: false);
            }
            else
            {
                File.Move(temporaryPath, path);
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
