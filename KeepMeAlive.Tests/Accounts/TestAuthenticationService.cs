using KeepMeAlive.Services.Interfaces;

namespace KeepMeAlive.Tests.Accounts;

internal sealed class TestAuthenticationService : IAuthenticationService
{
    public bool IsConfigured => true;
    public Task SignInAsync(CancellationToken cancellationToken = default) => Task.Delay(Timeout.Infinite, cancellationToken);
    public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
    public Task SignOutAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
