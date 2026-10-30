using KeepMeAlive.Models;
using KeepMeAlive.ViewModels;
using System.Net;
using System.Net.Http;

namespace KeepMeAlive.Tests.Accounts;

public sealed class AccountViewModelTests
{
    [Fact]
    public async Task ConstructionDoesNotAccessNetworkAndStartupCacheUsesRealTimestamp()
    {
        var accounts = new TestAccountService();
        await using var model = new AccountViewModel(accounts, new TestAuthenticationService());
        Assert.Equal(0, accounts.RefreshCount);
        Assert.Equal("Never", model.LastSyncedDisplay);
        accounts.Current = new UserAccountProfile { UserId = "user", DisplayName = "Alice", Email = "alice@example.test", LastSyncedAtUtc = new DateTime(2026, 10, 29, 10, 0, 0, DateTimeKind.Utc) };
        model.RefreshFromCache();
        Assert.True(model.IsSignedIn);
        Assert.Equal("Alice", model.DisplayName);
        Assert.NotEqual("Never", model.LastSyncedDisplay);
    }

    [Fact]
    public async Task DisposingCancelsAndAwaitsInteractiveSignIn()
    {
        var model = new AccountViewModel(new TestAccountService(), new TestAuthenticationService());
        var signIn = model.SignInCommand.ExecuteAsync(null);
        Assert.True(model.IsRefreshing);
        await model.DisposeAsync();
        Assert.True(signIn.IsCompleted);
        Assert.False(model.IsRefreshing);
    }

    [Fact]
    public async Task RefreshPreservesCachedIdentityDuringOutageAndUsesRecoveredTimestamp()
    {
        var profile = new UserAccountProfile
        {
            UserId = "user-a", DisplayName = "Alice", Email = "alice@example.test",
            LastSyncedAtUtc = new DateTime(2026, 10, 29, 10, 0, 0, DateTimeKind.Utc),
            Entitlements = [new AccountEntitlement { Feature = "saved-feature", ExpiresAt = new DateTimeOffset(2030, 1, 15, 12, 0, 0, TimeSpan.Zero) }],
        };
        var accounts = new TestAccountService { Current = profile };
        await using var model = new AccountViewModel(accounts, new TestAuthenticationService());
        await model.RefreshCommand.ExecuteAsync(null);
        var lastSynced = profile.LastSyncedAtUtc.ToLocalTime().ToString("dd MMM yyyy, HH:mm");
        var expiry = profile.Entitlements[0].ExpiresAt!.Value.ToLocalTime().ToString("dd MMM yyyy");
        Assert.Equal("Connected", model.AccountStatus);
        Assert.Equal(lastSynced, model.LastSyncedDisplay);

        accounts.RefreshFailure = new HttpRequestException("staging service unavailable", null, HttpStatusCode.ServiceUnavailable);
        await model.RefreshCommand.ExecuteAsync(null);

        Assert.Equal("Offline · cached account", model.AccountStatus);
        Assert.True(model.IsSignedIn);
        Assert.False(model.IsRefreshing);
        Assert.False(model.CanSignIn);
        Assert.Equal("Alice", model.DisplayName);
        Assert.Equal("alice@example.test", model.Email);
        Assert.Equal(lastSynced, model.LastSyncedDisplay);
        Assert.Equal("Subscription linked", model.PlanName);
        Assert.Equal("Server-verified access", model.LicenseAccessStatus);
        Assert.Equal(expiry, model.LicenseExpiryDisplay);
        Assert.Same(profile, accounts.Current);
        Assert.Contains("Your local session can continue.", model.StatusMessage);
        Assert.DoesNotContain("staging service unavailable", model.StatusMessage);

        var recovered = new UserAccountProfile
        {
            UserId = profile.UserId, DisplayName = "Alice updated", Email = profile.Email,
            LastSyncedAtUtc = profile.LastSyncedAtUtc.AddMinutes(7),
        };
        accounts.Current = recovered;
        accounts.RefreshFailure = null;
        await model.RefreshCommand.ExecuteAsync(null);

        Assert.Equal(3, accounts.RefreshCount);
        Assert.Equal("Connected", model.AccountStatus);
        Assert.True(model.IsSignedIn);
        Assert.False(model.IsRefreshing);
        Assert.Equal("Alice updated", model.DisplayName);
        Assert.Equal(profile.Email, model.Email);
        Assert.Equal(recovered.LastSyncedAtUtc.ToLocalTime().ToString("dd MMM yyyy, HH:mm"), model.LastSyncedDisplay);
        Assert.NotEqual(lastSynced, model.LastSyncedDisplay);
        Assert.Equal("No subscription linked", model.PlanName);
        Assert.Equal("Local utility available", model.LicenseAccessStatus);
        Assert.Equal("No subscription linked", model.LicenseExpiryDisplay);
        Assert.Equal("Account details are up to date.", model.StatusMessage);
    }

    [Fact]
    public async Task InitialRefreshFailureDoesNotFabricateAnAccount()
    {
        var accounts = new TestAccountService { RefreshFailure = new HttpRequestException("service unavailable", null, HttpStatusCode.ServiceUnavailable) };
        await using var model = new AccountViewModel(accounts, new TestAuthenticationService());

        await model.RefreshCommand.ExecuteAsync(null);

        Assert.Equal("Connection error", model.AccountStatus);
        Assert.False(model.IsSignedIn);
        Assert.False(model.IsRefreshing);
        Assert.True(model.CanSignIn);
        Assert.Empty(model.DisplayName);
        Assert.Equal("Sign in to connect your account", model.Email);
        Assert.Equal("Never", model.LastSyncedDisplay);
        Assert.Equal("No subscription linked", model.PlanName);
        Assert.Equal("Local utility available", model.LicenseAccessStatus);
        Assert.Null(accounts.Current);
    }

    [Fact]
    public async Task LogoutDuringOutageClearsCachedIdentityAndEntitlements()
    {
        var accounts = new TestAccountService
        {
            Current = new UserAccountProfile
            {
                UserId = "user-a", DisplayName = "Alice", Email = "alice@example.test",
                LastSyncedAtUtc = new DateTime(2026, 10, 29, 10, 0, 0, DateTimeKind.Utc),
                Entitlements = [new AccountEntitlement { Feature = "saved-feature" }],
            },
        };
        await using var model = new AccountViewModel(accounts, new TestAuthenticationService());
        await model.RefreshCommand.ExecuteAsync(null);
        accounts.RefreshFailure = new HttpRequestException("service unavailable", null, HttpStatusCode.ServiceUnavailable);
        await model.RefreshCommand.ExecuteAsync(null);
        Assert.True(model.IsSignedIn);

        await model.SignOutCommand.ExecuteAsync(null);

        Assert.Null(accounts.Current);
        Assert.Equal("Signed out", model.AccountStatus);
        Assert.False(model.IsSignedIn);
        Assert.False(model.IsRefreshing);
        Assert.True(model.CanSignIn);
        Assert.Empty(model.DisplayName);
        Assert.Equal("Sign in to connect your account", model.Email);
        Assert.Equal("Never", model.LastSyncedDisplay);
        Assert.Equal("No subscription linked", model.PlanName);
        Assert.Equal("Local utility available", model.LicenseAccessStatus);
        Assert.Equal("No subscription linked", model.LicenseExpiryDisplay);

        await model.RefreshCommand.ExecuteAsync(null);

        Assert.Equal("Connection error", model.AccountStatus);
        Assert.False(model.IsSignedIn);
        Assert.Empty(model.DisplayName);
        Assert.Equal("Never", model.LastSyncedDisplay);
        Assert.Null(accounts.Current);
    }
}
