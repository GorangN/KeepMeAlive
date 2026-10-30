using System.IO;
using System.Text.Json;
namespace KeepMeAlive;
/// <summary>Writes bounded local diagnostic metadata without tokens, user names, or exception messages.</summary>
internal static class LocalDiagnostics
{
    private static readonly object Gate = new();
    public static void Write(string operation, Exception exception)
    {
        try
        {
            lock (Gate)
            {
                var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KeepMeAlive", "Logs");
                Directory.CreateDirectory(directory);
                var path = Path.Combine(directory, "diagnostics.jsonl");
                if (File.Exists(path) && new FileInfo(path).Length > 1048576) { File.Move(path, path + ".previous", true); }
                File.AppendAllText(path, JsonSerializer.Serialize(new { time = DateTimeOffset.UtcNow, operation, type = exception.GetType().Name, code = exception.HResult }) + Environment.NewLine);
            }
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException) { }
    }
}
