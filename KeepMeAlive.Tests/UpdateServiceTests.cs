using System.Net;
using System.Net.Http;
using KeepMeAlive.Services;

namespace KeepMeAlive.Tests;

public sealed class UpdateServiceTests
{
    [Fact]
    public async Task ExplicitChecksRepeatAndNeverTrustRemoteNavigationUrls()
    {
        var handler = new ReleaseHandler();
        using var client = new HttpClient(handler);
        var service = new UpdateService(client);
        await service.CheckForUpdateAsync();
        await service.CheckForUpdateAsync();
        Assert.Equal(2, handler.Requests);
        Assert.Equal("99.0.0", service.LatestVersion);
        Assert.Equal("https://github.com/GorangN/KeepMeAlive/releases/latest", service.ReleaseUrl);
    }

    [Fact]
    public async Task FailedChecksSurfaceTransportFailure()
    {
        using var client = new HttpClient(new ReleaseHandler { Status = HttpStatusCode.ServiceUnavailable });
        var service = new UpdateService(client);
        await Assert.ThrowsAsync<HttpRequestException>(() => service.CheckForUpdateAsync());
    }

    private sealed class ReleaseHandler : HttpMessageHandler
    {
        public int Requests { get; private set; }
        public HttpStatusCode Status { get; init; } = HttpStatusCode.OK;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            return Task.FromResult(new HttpResponseMessage(Status) { Content = new StringContent("{\"tag_name\":\"v99.0.0\",\"html_url\":\"https://untrusted.example/\"}", System.Text.Encoding.UTF8, "application/json") });
        }
    }
}
