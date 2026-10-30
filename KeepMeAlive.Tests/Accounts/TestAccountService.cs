using KeepMeAlive.Models;
using KeepMeAlive.Services.Interfaces;
using System.Net.Http;

namespace KeepMeAlive.Tests.Accounts;

internal sealed class TestAccountService : IAccountService
{
    public UserAccountProfile? Current { get; set; }
    public int RefreshCount { get; private set; }
    public HttpRequestException? RefreshFailure { get; set; }
    public Task<UserAccountProfile?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        RefreshCount++;
        return RefreshFailure is null ? Task.FromResult(Current) : Task.FromException<UserAccountProfile?>(RefreshFailure);
    }
    public Task<UserAccountProfile> UpdateProfileAsync(string displayName, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public void Clear() => Current = null;
}
