using KeepMeAlive.Services.Interfaces;

namespace KeepMeAlive.Tests.Accounts;

internal sealed class TestCallbackListener : ILoopbackCallbackListener
{
    public async Task<Uri> ListenAsync(Func<Uri, Task> launch, CancellationToken cancellationToken)
    {
        await launch(new Uri("http://127.0.0.1:49152/callback/"));
        return new Uri("http://127.0.0.1:49152/callback/?code=untrusted&state=wrong");
    }
}
