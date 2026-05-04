using KeepMeAlive.Models;
using KeepMeAlive.Services.Interfaces;

namespace KeepMeAlive.Services;

/// <summary>
/// Local preview implementation for account data.
/// Replace this service with a Supabase-backed implementation in production.
/// </summary>
public class AccountService : IAccountService
{
    /// <inheritdoc/>
    public Task<UserAccountProfile> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var userName = Environment.UserName;
        var machineName = Environment.MachineName;

        return Task.FromResult(new UserAccountProfile
        {
            DisplayName = string.IsNullOrWhiteSpace(userName) ? "Local User" : userName,
            Email = "Available after Supabase sign-in",
            WorkspaceName = string.IsNullOrWhiteSpace(machineName) ? "Preview workspace" : machineName,
            AuthProvider = "Local preview",
            PlanName = "Starter Preview",
            StatusLabel = "Not connected",
            LastSyncedAtUtc = DateTime.UtcNow,
        });
    }
}
