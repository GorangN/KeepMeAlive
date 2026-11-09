using KeepMeAlive.Services.Interfaces;

namespace KeepMeAlive.Tests.Accounts;

internal sealed class PresentationAuthenticationService(bool isConfigured = true) : IAuthenticationService
{
    public bool IsConfigured { get; } = isConfigured;
    public Task? SignInTask { get; set; }
    public Task SignOutTask { get; set; } = Task.CompletedTask;

    public Task SignInAsync(CancellationToken cancellationToken = default) =>
        SignInTask?.WaitAsync(cancellationToken) ?? Task.Delay(Timeout.Infinite, cancellationToken);

    public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);

    public Task SignOutAsync(CancellationToken cancellationToken = default) => SignOutTask.WaitAsync(cancellationToken);
}
