using KeepMeAlive.Services.Interfaces;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
namespace KeepMeAlive.Services;
/// <summary>Retrieves bounded, cancellable metadata from the canonical repository.</summary>
public sealed class UpdateService : IUpdateService
{
    private const string ApiUrl = "https://api.github.com/repos/GorangN/KeepMeAlive/releases/latest";
    private readonly HttpClient _http;
    /// <inheritdoc/>
    public string LatestVersion { get; private set; } = "";
    /// <inheritdoc/>
    public string ReleaseUrl => "https://github.com/GorangN/KeepMeAlive/releases/latest";
    /// <summary>Creates a release checker using the shared transport.</summary>
    /// <param name="http">The HTTP transport.</param>
    public UpdateService(HttpClient http) { _http = http; }
    /// <inheritdoc/>
    public async Task<bool> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        using var request = new HttpRequestMessage(HttpMethod.Get, ApiUrl);
        request.Headers.UserAgent.ParseAdd("KeepMeAlive/1.0");
        using var response = await _http.SendAsync(request, timeout.Token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await response.Content.LoadIntoBufferAsync(262144, timeout.Token).ConfigureAwait(false);
        var release = await response.Content.ReadFromJsonAsync<GithubRelease>(cancellationToken: timeout.Token).ConfigureAwait(false);
        LatestVersion = release?.TagName?.TrimStart('v') ?? "";
        var current = Assembly.GetExecutingAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "1.0.0";
        return Version.TryParse(LatestVersion, out var latest) && Version.TryParse(current, out var installed) && latest > installed;
    }
    private sealed record GithubRelease([property: System.Text.Json.Serialization.JsonPropertyName("tag_name")] string? TagName);
}
