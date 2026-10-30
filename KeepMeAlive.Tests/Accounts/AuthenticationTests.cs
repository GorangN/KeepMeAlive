using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using KeepMeAlive.Models;
using KeepMeAlive.Services;

namespace KeepMeAlive.Tests.Accounts;

public sealed class AuthenticationTests
{
    private static NeonOptions Options => new() { AuthIssuer = "https://auth.example.test/neondb/auth", ApiBaseUrl = "https://api.example.test/", ClientId = "desktop", Resource = "https://keepmealive.app/api" };

    [Fact]
    public void PkceMatchesRfc7636Vector()
    {
        Assert.Equal("E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM", OAuthProtocol.CreateChallenge("dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk"));
        Assert.NotEqual(OAuthProtocol.CreateRandomValue(), OAuthProtocol.CreateRandomValue());
    }

    [Theory]
    [InlineData("?code=code&state=wrong")]
    [InlineData("?code=code&state=expected&state=expected")]
    [InlineData("?error=access_denied&state=expected")]
    [InlineData("?code=&state=expected")]
    public void ForgedOrDeniedCallbackIsRejected(string query)
    {
        Assert.Throws<InvalidOperationException>(() => OAuthProtocol.ReadCode(new Uri("http://127.0.0.1:49152/callback/" + query), "expected"));
    }

    [Fact]
    public async Task UnconfiguredAccountMakesNoNetworkRequest()
    {
        using var http = new HttpClient(new AccountHttpHandler(_ => throw new InvalidOperationException("Network was called")));
        using var service = new AuthenticationService(new NeonOptions(), http, new MemorySecretStore(), new TestBrowser(), new TestCallbackListener());
        Assert.Null(await service.GetAccessTokenAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SignInAsync());
    }

    [Fact]
    public async Task ForgedIssuerDiscoveryIsRejectedBeforeOpeningBrowser()
    {
        var browser = new TestBrowser();
        using var http = new HttpClient(new AccountHttpHandler(_ => Json(new { issuer = Options.AuthIssuer + "/wrong", authorization_endpoint = Options.AuthIssuer + "/authorize", token_endpoint = Options.AuthIssuer + "/token" })));
        using var service = new AuthenticationService(Options, http, new MemorySecretStore(), browser, new TestCallbackListener());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SignInAsync());
        Assert.Null(browser.LastUri);
    }

    [Fact]
    public async Task InteractiveStateMismatchNeverExchangesCode()
    {
        using var http = new HttpClient(new AccountHttpHandler(request => request.Method == HttpMethod.Get ? Metadata() : throw new InvalidOperationException("Token endpoint must not be called")));
        var browser = new TestBrowser();
        using var service = new AuthenticationService(Options, http, new MemorySecretStore(), browser, new TestCallbackListener());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SignInAsync());
        Assert.Contains("code_challenge_method=S256", browser.LastUri!.Query);
        Assert.Contains("resource=", browser.LastUri.Query);
        Assert.Contains("prompt=consent", browser.LastUri.Query);
    }

    [Fact]
    public async Task ConcurrentRefreshRotatesOnceAndOnlyStoresRefreshCredential()
    {
        var secrets = SavedSession();
        var refreshCount = 0;
        using var http = new HttpClient(new AccountHttpHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("oauth-authorization-server", StringComparison.Ordinal)) { return Metadata(); }
            if (request.Method == HttpMethod.Post)
            {
                refreshCount++;
                return Json(new { access_token = "new-access-token", refresh_token = "new-refresh-token", expires_in = 3600, token_type = "Bearer" });
            }
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            return Json(new { userId = "11111111-1111-4111-8111-111111111111" });
        }));
        using var service = new AuthenticationService(Options, http, secrets, new TestBrowser(), new TestCallbackListener());
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => service.GetAccessTokenAsync()));
        Assert.All(results, token => Assert.Equal("new-access-token", token));
        Assert.Equal(1, refreshCount);
        Assert.Contains("new-refresh-token", secrets.Session);
        Assert.DoesNotContain("new-access-token", secrets.Session);
    }

    [Fact]
    public async Task InvalidRefreshClearsProtectedCredentials()
    {
        var secrets = SavedSession();
        using var http = new HttpClient(new AccountHttpHandler(request => request.Method == HttpMethod.Get ? Metadata() : new HttpResponseMessage(HttpStatusCode.BadRequest)));
        using var service = new AuthenticationService(Options, http, secrets, new TestBrowser(), new TestCallbackListener());
        Assert.Null(await service.GetAccessTokenAsync());
        Assert.Empty(secrets.Session);
    }

    [Fact]
    public async Task OfflineLogoutStillClearsProtectedCredentials()
    {
        var secrets = SavedSession();
        using var http = new HttpClient(new AccountHttpHandler(_ => throw new HttpRequestException("offline")));
        using var service = new AuthenticationService(Options, http, secrets, new TestBrowser(), new TestCallbackListener());
        await service.SignOutAsync();
        Assert.Empty(secrets.Session);
    }

    [Fact]
    public async Task RotatedCredentialSurvivesApiOutageWithoutAcceptingAccessToken()
    {
        var secrets = SavedSession();
        using var http = new HttpClient(new AccountHttpHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("oauth-authorization-server", StringComparison.Ordinal)) { return Metadata(); }
            if (request.Method == HttpMethod.Post)
            {
                return Json(new { access_token = "unverified-access", refresh_token = "rotated-refresh", expires_in = 3600, token_type = "Bearer" });
            }
            throw new HttpRequestException("API unavailable");
        }));
        using var service = new AuthenticationService(Options, http, secrets, new TestBrowser(), new TestCallbackListener());
        await Assert.ThrowsAsync<HttpRequestException>(() => service.GetAccessTokenAsync());
        Assert.Contains("rotated-refresh", secrets.Session);
        Assert.DoesNotContain("unverified-access", secrets.Session);
    }

    [Fact]
    public async Task ApiAuthorizationRejectionClearsRotatedSession()
    {
        var secrets = SavedSession();
        using var http = new HttpClient(new AccountHttpHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("oauth-authorization-server", StringComparison.Ordinal)) { return Metadata(); }
            if (request.Method == HttpMethod.Post)
            {
                return Json(new { access_token = "invalid-access", refresh_token = "rotated-refresh", expires_in = 3600, token_type = "Bearer" });
            }
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        }));
        using var service = new AuthenticationService(Options, http, secrets, new TestBrowser(), new TestCallbackListener());
        await Assert.ThrowsAsync<HttpRequestException>(() => service.GetAccessTokenAsync());
        Assert.Empty(secrets.Session);
    }

    [Fact]
    public async Task InteractiveGrantsReceiveDistinctStableSessionIdentifiers()
    {
        var secrets = new MemorySecretStore();
        var browser = new TestBrowser();
        using var http = new HttpClient(new AccountHttpHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("oauth-authorization-server", StringComparison.Ordinal)) { return Metadata(); }
            return request.Method == HttpMethod.Post
                ? Json(new { access_token = "new-access", refresh_token = "new-refresh", expires_in = 3600, token_type = "Bearer" })
                : Json(new { userId = "test-user" });
        }));
        using var service = new AuthenticationService(Options, http, secrets, browser, new MatchingCallbackListener(browser));
        await service.SignInAsync();
        using var first = JsonDocument.Parse(secrets.Session);
        var firstId = first.RootElement.GetProperty("SessionId").GetString();
        Assert.True(Guid.TryParse(firstId, out _));
        await service.SignInAsync();
        using var second = JsonDocument.Parse(secrets.Session);
        Assert.NotEqual(firstId, second.RootElement.GetProperty("SessionId").GetString());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RefreshKeepsSessionFamilyWhenTokenRotatesOrIsRetained(bool rotate)
    {
        var secrets = new MemorySecretStore
        {
            Session = JsonSerializer.Serialize(new
            {
                SessionId = "retained-family", Issuer = Options.AuthIssuer, ClientId = Options.ClientId,
                ApiBaseUrl = Options.ApiBaseUrl, RefreshToken = "old-refresh",
            }),
        };
        using var http = new HttpClient(new AccountHttpHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("oauth-authorization-server", StringComparison.Ordinal)) { return Metadata(); }
            return request.Method == HttpMethod.Post
                ? Json(new { access_token = "new-access", refresh_token = rotate ? "new-refresh" : null, expires_in = 3600, token_type = "Bearer" })
                : Json(new { userId = "test-user" });
        }));
        using var service = new AuthenticationService(Options, http, secrets, new TestBrowser(), new TestCallbackListener());
        Assert.Equal("new-access", await service.GetAccessTokenAsync());
        using var session = JsonDocument.Parse(secrets.Session);
        Assert.Equal("retained-family", session.RootElement.GetProperty("SessionId").GetString());
        Assert.Equal(rotate ? "new-refresh" : "old-refresh", session.RootElement.GetProperty("RefreshToken").GetString());
    }

    private static MemorySecretStore SavedSession() => new()
    {
        Session = JsonSerializer.Serialize(new { Issuer = Options.AuthIssuer, ClientId = Options.ClientId, ApiBaseUrl = Options.ApiBaseUrl, RefreshToken = "old-refresh-token" }),
    };
    private static HttpResponseMessage Metadata() => Json(new { issuer = Options.AuthIssuer, authorization_endpoint = Options.AuthIssuer + "/oauth2/authorize", token_endpoint = Options.AuthIssuer + "/oauth2/token", revocation_endpoint = Options.AuthIssuer + "/oauth2/revoke" });
    private static HttpResponseMessage Json<T>(T value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(value) };
}
