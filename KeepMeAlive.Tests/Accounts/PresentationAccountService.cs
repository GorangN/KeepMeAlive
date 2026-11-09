using KeepMeAlive.Models;
using KeepMeAlive.Services.Interfaces;

namespace KeepMeAlive.Tests.Accounts;

internal sealed class PresentationAccountService : IAccountService
{
    public UserAccountProfile? Current { get; set; }
    public Task<UserAccountProfile?>? RefreshTask { get; set; }
    public Task<UserAccountProfile>? SaveTask { get; set; }

    public Task<UserAccountProfile?> GetCurrentAsync(CancellationToken cancellationToken = default) =>
        RefreshTask?.WaitAsync(cancellationToken) ?? Task.FromResult(Current);

    public Task<UserAccountProfile> UpdateProfileAsync(string displayName, CancellationToken cancellationToken = default) =>
        SaveTask?.WaitAsync(cancellationToken) ?? Task.FromResult(Current ?? throw new InvalidOperationException("No account is signed in."));

    public void Clear() => Current = null;
}
