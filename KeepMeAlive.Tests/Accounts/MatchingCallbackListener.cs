using KeepMeAlive.Services.Interfaces;

namespace KeepMeAlive.Tests.Accounts;

internal sealed class MatchingCallbackListener(TestBrowser browser) : ILoopbackCallbackListener
{
    public async Task<Uri> ListenAsync(Func<Uri, Task> launch, CancellationToken cancellationToken)
    {
        var redirect = new Uri("http://127.0.0.1:49152/callback/");
        await launch(redirect);
        var state = browser.LastUri!.Query.TrimStart('?').Split('&')
            .Single(part => part.StartsWith("state=", StringComparison.Ordinal))["state=".Length..];
        return new Uri(redirect, "?code=test-code&state=" + state);
    }
}
