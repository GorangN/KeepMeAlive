using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeepMeAlive.Models;
using KeepMeAlive.Services.Interfaces;

namespace KeepMeAlive.ViewModels;

/// <summary>Coordinates explicit account actions and exposes honest offline and signed-out states.</summary>
public partial class AccountViewModel : ObservableObject, IAsyncDisposable
{
    private readonly IAccountService _accounts;
    private readonly IAuthenticationService _authentication;
    private CancellationTokenSource? _signInCancellation;
    private readonly CancellationTokenSource _lifetime = new();

    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private string _email = "Sign in to connect your account";
    [ObservableProperty] private string _accountStatus = "Signed out";
    [ObservableProperty] private string _lastSyncedDisplay = "Never";
    [ObservableProperty] private string _planName = "No subscription linked";
    [ObservableProperty] private string _licenseAccessStatus = "Local utility available";
    [ObservableProperty] private string _licenseExpiryDisplay = "No subscription linked";
    [ObservableProperty] private string _statusMessage = "Your keep-alive sessions work without an account.";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanSignIn))] private bool _isSignedIn;
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CanSignIn))] private bool _isRefreshing;

    /// <summary>Gets the account workspace label.</summary>
    public string WorkspaceName => "Personal account";
    /// <summary>Gets the identity provider label.</summary>
    public string AuthProvider => "Neon";
    /// <summary>Gets whether another browser sign-in may start.</summary>
    public bool CanSignIn => _authentication.IsConfigured && !IsSignedIn && !IsRefreshing;

    /// <summary>Creates the account page without initiating network access.</summary>
    /// <param name="accounts">Authenticated account operations.</param>
    /// <param name="authentication">Browser-based authentication and session persistence.</param>
    public AccountViewModel(IAccountService accounts, IAuthenticationService authentication)
    {
        _accounts = accounts;
        _authentication = authentication;
        if (!authentication.IsConfigured)
        {
            StatusMessage = "Account sign-in is being prepared for this release. You can use all local utility features.";
        }
    }

    [RelayCommand]
    private async Task SignInAsync()
    {
        if (!CanSignIn) { return; }
        IsRefreshing = true;
        _signInCancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        AccountStatus = "Waiting for browser";
        StatusMessage = "Complete sign-in in your browser, or cancel to return.";
        try
        {
            await _authentication.SignInAsync(_signInCancellation.Token).ConfigureAwait(false);
            Apply(await _accounts.GetCurrentAsync(_signInCancellation.Token).ConfigureAwait(false));
        }
        catch (OperationCanceledException) { AccountStatus = "Signed out"; StatusMessage = "Sign-in was cancelled. Your local utility is ready."; }
        catch (Exception error) when (IsAccountError(error)) { AccountStatus = "Connection error"; StatusMessage = UserMessage(error); }
        finally { IsRefreshing = false; _signInCancellation.Dispose(); _signInCancellation = null; }
    }

    [RelayCommand]
    private void CancelSignIn() => _signInCancellation?.Cancel();

    [RelayCommand]
    private async Task SignOutAsync()
    {
        _signInCancellation?.Cancel();
        IsRefreshing = true;
        try { await _authentication.SignOutAsync(_lifetime.Token).ConfigureAwait(false); _accounts.Clear(); Apply(null); }
        catch (Exception error) when (IsAccountError(error)) { StatusMessage = UserMessage(error); }
        finally { IsRefreshing = false; }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsRefreshing) { return; }
        IsRefreshing = true;
        try { Apply(await _accounts.GetCurrentAsync(_lifetime.Token).ConfigureAwait(false)); }
        catch (Exception error) when (IsAccountError(error))
        {
            AccountStatus = _accounts.Current is null ? "Connection error" : "Offline · cached account";
            StatusMessage = UserMessage(error) + " Your local session can continue.";
        }
        finally { IsRefreshing = false; }
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        if (!IsSignedIn || IsRefreshing) { return; }
        IsRefreshing = true;
        try { Apply(await _accounts.UpdateProfileAsync(DisplayName, _lifetime.Token).ConfigureAwait(false)); StatusMessage = "Profile saved."; }
        catch (Exception error) when (IsAccountError(error)) { StatusMessage = UserMessage(error); }
        finally { IsRefreshing = false; }
    }

    private void Apply(UserAccountProfile? profile)
    {
        IsSignedIn = profile is not null;
        DisplayName = profile?.DisplayName ?? string.Empty;
        Email = profile?.Email ?? "Sign in to connect your account";
        AccountStatus = profile is null ? "Signed out" : "Connected";
        LastSyncedDisplay = profile is null ? "Never" : profile.LastSyncedAtUtc.ToLocalTime().ToString("dd MMM yyyy, HH:mm");
        var entitlement = profile?.Entitlements.FirstOrDefault();
        PlanName = entitlement is null ? "No subscription linked" : "Subscription linked";
        LicenseAccessStatus = entitlement is null ? "Local utility available" : "Server-verified access";
        LicenseExpiryDisplay = entitlement?.ExpiresAt?.ToLocalTime().ToString("dd MMM yyyy") ?? (entitlement is null ? "No subscription linked" : "No expiration");
        StatusMessage = profile is null ? "Your keep-alive sessions work without an account." : "Account details are up to date.";
    }
    /// <summary>Refreshes the bound profile after an explicitly enabled startup synchronization.</summary>
    public void RefreshFromCache() => Apply(_accounts.Current);

    /// <summary>Cancels and awaits account operations before authentication resources are released.</summary>
    /// <returns>A task completing when no account command is running.</returns>
    public async ValueTask DisposeAsync()
    {
        _lifetime.Cancel();
        _signInCancellation?.Cancel();
        var tasks = new[] { SignInCommand.ExecutionTask, SignOutCommand.ExecutionTask, RefreshCommand.ExecutionTask, SaveProfileCommand.ExecutionTask }.OfType<Task>().ToArray();
        try { await Task.WhenAll(tasks).ConfigureAwait(false); }
        catch (OperationCanceledException) { }
        _lifetime.Dispose();
    }
    private static bool IsAccountError(Exception error) => error is HttpRequestException or InvalidOperationException or ArgumentException or System.IO.IOException or UnauthorizedAccessException or System.Security.Cryptography.CryptographicException or System.Text.Json.JsonException or OperationCanceledException;
    private static string UserMessage(Exception error) => error is ArgumentException ? "Enter a display name between 1 and 100 characters." : error is HttpRequestException ? "The account service could not be reached. Check your connection and try again." : error is InvalidOperationException ? error.Message : "Your account could not be updated. Please try again.";
}
