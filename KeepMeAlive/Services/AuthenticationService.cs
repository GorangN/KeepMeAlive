using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using KeepMeAlive.Models;
using KeepMeAlive.Services.Interfaces;

namespace KeepMeAlive.Services;

/// <summary>Runs public-client PKCE authentication and serializes refresh-token rotation.</summary>
public sealed class AuthenticationService : IAuthenticationService, IDisposable
{
    private readonly NeonOptions _options;
    private readonly HttpClient _http;
    private readonly ISecretStore _secrets;
    private readonly IBrowserLauncher _browser;
    private readonly ILoopbackCallbackListener _listener;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _expiresAt;
    private OAuthMetadata? _metadata;

    /// <summary>Creates an authentication service for a configured Neon branch.</summary>
    /// <param name="options">Public deployment endpoints and client registration.</param>
    /// <param name="http">An HTTP client with automatic redirects disabled.</param>
    /// <param name="secrets">Protected local credential persistence.</param>
    /// <param name="browser">System browser navigation.</param>
    /// <param name="listener">Loopback callback handling.</param>
    public AuthenticationService(NeonOptions options, HttpClient http, ISecretStore secrets, IBrowserLauncher browser, ILoopbackCallbackListener listener)
    {
        _options = options; _http = http; _secrets = secrets; _browser = browser; _listener = listener;
    }
    /// <inheritdoc/>
    public bool IsConfigured => _options.IsConfigured;

    /// <inheritdoc/>
    public async Task SignInAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) { throw new InvalidOperationException("Account sign-in is not available in this build yet. Your local utility is ready to use."); }
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var metadata = await GetMetadataAsync(cancellationToken).ConfigureAwait(false);
            var state = OAuthProtocol.CreateRandomValue();
            var verifier = OAuthProtocol.CreateRandomValue();
            Uri? redirect = null;
            var callback = await _listener.ListenAsync(uri =>
            {
                redirect = uri;
                var values = new Dictionary<string, string>
                {
                    ["response_type"] = "code", ["client_id"] = _options.ClientId, ["redirect_uri"] = uri.AbsoluteUri,
                    ["scope"] = "openid offline_access", ["state"] = state, ["code_challenge"] = OAuthProtocol.CreateChallenge(verifier),
                    ["code_challenge_method"] = "S256", ["resource"] = _options.Resource, ["prompt"] = "consent",
                };
                var query = string.Join("&", values.Select(item => $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value)}"));
                _browser.Open(new Uri(metadata.AuthorizationEndpoint + "?" + query));
                return Task.CompletedTask;
            }, cancellationToken).ConfigureAwait(false);
            var code = OAuthProtocol.ReadCode(callback, state);
            using var request = new HttpRequestMessage(HttpMethod.Post, metadata.TokenEndpoint)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "authorization_code", ["code"] = code, ["code_verifier"] = verifier,
                    ["redirect_uri"] = redirect!.AbsoluteUri, ["client_id"] = _options.ClientId, ["resource"] = _options.Resource,
                }),
            };
            using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var token = await response.Content.ReadFromJsonAsync<OAuthTokenResponse>(cancellationToken).ConfigureAwait(false);
            await AcceptTokensAsync(token, null, cancellationToken).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    /// <inheritdoc/>
    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) { return null; }
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_accessToken is not null && _expiresAt > DateTimeOffset.UtcNow.AddMinutes(1)) { return _accessToken; }
            var saved = ReadSession();
            if (saved is null) { return null; }
            var metadata = await GetMetadataAsync(cancellationToken).ConfigureAwait(false);
            using var response = await _http.PostAsync(metadata.TokenEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token", ["refresh_token"] = saved.RefreshToken,
                ["client_id"] = _options.ClientId, ["resource"] = _options.Resource,
            }), cancellationToken).ConfigureAwait(false);
            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
            {
                ClearSession();
                return null;
            }
            response.EnsureSuccessStatusCode();
            var token = await response.Content.ReadFromJsonAsync<OAuthTokenResponse>(cancellationToken).ConfigureAwait(false);
            await AcceptTokensAsync(token, saved, cancellationToken).ConfigureAwait(false);
            return _accessToken;
        }
        finally { _gate.Release(); }
    }

    /// <inheritdoc/>
    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            OAuthSession? session;
            try { session = ReadSession(); }
            catch (System.Security.Cryptography.CryptographicException) { session = null; }
            ClearSession();
            if (session is null || !IsConfigured) { return; }
            try
            {
                var metadata = await GetMetadataAsync(cancellationToken).ConfigureAwait(false);
                if (metadata.RevocationEndpoint is not { } endpoint) { return; }
                using var response = await _http.PostAsync(endpoint, new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["token"] = session.RefreshToken, ["token_type_hint"] = "refresh_token", ["client_id"] = _options.ClientId,
                }), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception error) when (error is HttpRequestException or TaskCanceledException or InvalidOperationException)
            {
                // Local logout must succeed when remote revocation is unavailable.
            }
        }
        finally { _gate.Release(); }
    }

    private OAuthSession? ReadSession()
    {
        var json = _secrets.GetSession();
        if (string.IsNullOrWhiteSpace(json)) { return null; }
        try
        {
            var session = JsonSerializer.Deserialize<OAuthSession>(json);
            if (session is null || session.Issuer != _options.AuthIssuer || session.ClientId != _options.ClientId ||
                session.ApiBaseUrl != _options.ApiBaseUrl || string.IsNullOrWhiteSpace(session.RefreshToken)) { return null; }
            session.SessionId = session.GetSessionId();
            return session;
        }
        catch (JsonException) { ClearSession(); return null; }
    }

    private async Task AcceptTokensAsync(OAuthTokenResponse? token, OAuthSession? previousSession, CancellationToken cancellationToken)
    {
        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken) || !string.Equals(token.TokenType, "Bearer", StringComparison.OrdinalIgnoreCase) || token.ExpiresIn is < 1 or > 86400)
        {
            throw new InvalidOperationException("The sign-in service returned an invalid session.");
        }
        var refreshToken = token.RefreshToken ?? previousSession?.RefreshToken;
        if (string.IsNullOrWhiteSpace(refreshToken)) { throw new InvalidOperationException("The sign-in service did not grant a persistent session."); }
        _secrets.SaveSession(JsonSerializer.Serialize(new OAuthSession
        {
            SessionId = previousSession?.GetSessionId() ?? Guid.NewGuid().ToString("N"),
            Issuer = _options.AuthIssuer, ClientId = _options.ClientId, ApiBaseUrl = _options.ApiBaseUrl, RefreshToken = refreshToken,
        }));
        using var validation = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(_options.ApiBaseUrl), "v1/account"));
        validation.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        using var response = await _http.SendAsync(validation, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) { ClearSession(); }
        response.EnsureSuccessStatusCode();
        _accessToken = token.AccessToken;
        _expiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn);
    }

    private async Task<OAuthMetadata> GetMetadataAsync(CancellationToken cancellationToken)
    {
        if (_metadata is not null) { return _metadata; }
        var metadata = await _http.GetFromJsonAsync<OAuthMetadata>(_options.AuthIssuer.TrimEnd('/') + "/.well-known/oauth-authorization-server", cancellationToken).ConfigureAwait(false);
        if (metadata is null || metadata.Issuer != _options.AuthIssuer || !IsIssuerEndpoint(metadata.AuthorizationEndpoint) ||
            !IsIssuerEndpoint(metadata.TokenEndpoint) || metadata.RevocationEndpoint is { } endpoint && !IsIssuerEndpoint(endpoint))
        {
            throw new InvalidOperationException("The sign-in service identity could not be verified.");
        }
        _metadata = metadata;
        return metadata;
    }

    private bool IsIssuerEndpoint(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps &&
        string.IsNullOrEmpty(uri.UserInfo) && uri.GetLeftPart(UriPartial.Authority) == new Uri(_options.AuthIssuer).GetLeftPart(UriPartial.Authority) &&
        uri.AbsolutePath.StartsWith(new Uri(_options.AuthIssuer).AbsolutePath.TrimEnd('/') + "/", StringComparison.Ordinal);
    private void ClearSession() { _accessToken = null; _expiresAt = default; _secrets.DeleteSession(); }
    /// <summary>Releases synchronization resources after the application has stopped.</summary>
    public void Dispose() => _gate.Dispose();
}
