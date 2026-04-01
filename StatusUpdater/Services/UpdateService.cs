using KeepMeAlive.Services.Interfaces;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;

namespace KeepMeAlive.Services;

public class UpdateService : IUpdateService
{
    private const string ApiUrl = "https://api.github.com/repos/GorangN/StatusUpdater/releases/latest";

    private readonly HttpClient _http;

    public string LatestVersion { get; private set; } = "";
    public string ReleaseUrl { get; private set; } = "";

    private bool _checked;

    public UpdateService(HttpClient http)
    {
        _http = http;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("KeepMeAlive/1.0");
    }

    public async Task<bool> CheckForUpdateAsync()
    {
        if (_checked) return !string.IsNullOrEmpty(LatestVersion) && IsNewer(LatestVersion);

        try
        {
            var release = await _http.GetFromJsonAsync<GithubRelease>(ApiUrl);
            if (release is null) return false;

            LatestVersion = release.TagName?.TrimStart('v') ?? "";
            ReleaseUrl = release.HtmlUrl ?? "";
            _checked = true;

            return IsNewer(LatestVersion);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsNewer(string latestVersion)
    {
        var current = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion?.Split('+')[0] ?? "0.0.0";

        return Version.TryParse(latestVersion, out var latest)
            && Version.TryParse(current, out var curr)
            && latest > curr;
    }

    private record GithubRelease(
        [property: System.Text.Json.Serialization.JsonPropertyName("tag_name")] string? TagName,
        [property: System.Text.Json.Serialization.JsonPropertyName("html_url")] string? HtmlUrl);
}
