using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using KeepMeAlive.Messages;
using KeepMeAlive.Models;
using KeepMeAlive.Services.Interfaces;

namespace KeepMeAlive.ViewModels;

/// <summary>ViewModel for the account and license area.</summary>
public partial class AccountViewModel : ObservableObject, IRecipient<StorageChangedMessage>
{
    private readonly IAccountService _accountService;
    private readonly ISettingsService _settingsService;
    private readonly ISecretStore _secretStore;

    [ObservableProperty]
    private string _displayName = "Local User";

    [ObservableProperty]
    private string _email = "Available after Supabase sign-in";

    [ObservableProperty]
    private string _workspaceName = "Preview workspace";

    [ObservableProperty]
    private string _authProvider = "Local preview";

    [ObservableProperty]
    private string _planName = "Starter Preview";

    [ObservableProperty]
    private string _accountStatus = "Not connected";

    [ObservableProperty]
    private string _lastSyncedDisplay = "Never";

    [ObservableProperty]
    private string _licenseKey = string.Empty;

    [ObservableProperty]
    private string _licenseStatus = "No license key stored";

    [ObservableProperty]
    private string _licenseStorageLocation = "Windows Credential Manager";

    [ObservableProperty]
    private string _licenseAccessStatus = "Preview access";

    [ObservableProperty]
    private string _licenseExpiryDisplay = "Available after billing sync";

    [ObservableProperty]
    private bool _isRefreshing;

    /// <summary>Gets the production hand-off note shown in the UI.</summary>
    public string IntegrationHint =>
        "This page already uses an account service abstraction, so you can swap the local stub for Supabase auth and profile data later.";

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountViewModel"/> class.
    /// </summary>
    /// <param name="accountService">The account data service.</param>
    /// <param name="settingsService">The settings persistence service.</param>
    /// <param name="secretStore">The secure secret store.</param>
    /// <param name="messenger">The messenger used to observe storage mode changes.</param>
    public AccountViewModel(
        IAccountService accountService,
        ISettingsService settingsService,
        ISecretStore secretStore,
        IMessenger messenger)
    {
        _accountService = accountService;
        _settingsService = settingsService;
        _secretStore = secretStore;

        messenger.RegisterAll(this);

        LicenseKey = _secretStore.GetLicenseKey();
        UpdateLicenseState();

        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsRefreshing)
        {
            return;
        }

        IsRefreshing = true;

        try
        {
            var profile = await _accountService.GetCurrentAsync();
            DisplayName = profile.DisplayName;
            Email = profile.Email;
            WorkspaceName = profile.WorkspaceName;
            AuthProvider = profile.AuthProvider;
            PlanName = profile.PlanName;
            AccountStatus = profile.StatusLabel;
            LastSyncedDisplay = profile.LastSyncedAtUtc.ToLocalTime().ToString("dd MMM yyyy, HH:mm");

            LicenseKey = _secretStore.GetLicenseKey();
            UpdateLicenseState();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private void SaveLicense()
    {
        _secretStore.SaveLicenseKey(LicenseKey);
        LicenseKey = _secretStore.GetLicenseKey();
        UpdateLicenseState();
    }

    /// <inheritdoc/>
    public void Receive(StorageChangedMessage message)
    {
        LicenseKey = _secretStore.GetLicenseKey(message.StorageMode);
        UpdateLicenseState(message.StorageMode);
    }

    private void UpdateLicenseState()
    {
        UpdateLicenseState(_settingsService.CurrentStorageMode);
    }

    private void UpdateLicenseState(StorageMode storageMode)
    {
        bool hasLicenseKey = !string.IsNullOrWhiteSpace(LicenseKey);
        LicenseStorageLocation = storageMode switch
        {
            StorageMode.PortableLocal => "local protected storage",
            _ => "Windows Credential Manager"
        };

        LicenseStatus = hasLicenseKey
            ? $"Stored securely in {LicenseStorageLocation}."
            : $"No license key stored. New keys are saved to {LicenseStorageLocation}.";

        LicenseAccessStatus = hasLicenseKey
            ? "License key on file"
            : "Preview access";

        LicenseExpiryDisplay = hasLicenseKey
            ? "Validation starts after backend sync"
            : "Available after billing sync";
    }
}
