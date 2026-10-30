using System.IO;
using System.Text.Json;
using KeepMeAlive.Models;

namespace KeepMeAlive.Services;

/// <summary>Loads nonsecret deployment configuration shipped beside the executable.</summary>
public static class NeonConfiguration
{
    /// <summary>Reads public deployment settings without initiating network access.</summary>
    /// <returns>The configured endpoints, or an unconfigured account feature.</returns>
    public static NeonOptions Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "neon.json");
        try
        {
            var options = File.Exists(path) ? JsonSerializer.Deserialize<NeonOptions>(File.ReadAllText(path), new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? new NeonOptions() : new NeonOptions();
            options.ClientId = Environment.GetEnvironmentVariable("KEEP_ME_ALIVE_OAUTH_CLIENT_ID") ?? options.ClientId;
            return options;
        }
        catch (Exception error) when (error is IOException or JsonException or UnauthorizedAccessException)
        {
            return new NeonOptions();
        }
    }
}
