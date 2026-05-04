using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeepMeAlive.Services.Interfaces;

namespace KeepMeAlive.ViewModels;

/// <summary>ViewModel for the account and license area.</summary>
public partial class AccountViewModel : ObservableObject
{
    private readonly IAccountService _accountService;
    private readonly ISettingsService _settingsService;

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
    public AccountViewModel(IAccountService accountService, ISettingsService settingsService)
    {
        _accountService = accountService;
        _settingsService = settingsService;

        LicenseKey = _settingsService.Current.LicenseKey;
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

            LicenseKey = _settingsService.Current.LicenseKey;
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
        var settings = _settingsService.Current;
        settings.LicenseKey = LicenseKey.Trim();
        _settingsService.Save(settings);

        LicenseKey = settings.LicenseKey;
        UpdateLicenseState();
    }

    private void UpdateLicenseState()
    {
        bool hasLicenseKey = !string.IsNullOrWhiteSpace(LicenseKey);

        LicenseStatus = hasLicenseKey
            ? "Key saved locally"
            : "No license key stored";

        LicenseAccessStatus = hasLicenseKey
            ? "License key on file"
            : "Preview access";

        LicenseExpiryDisplay = hasLicenseKey
            ? "Validation starts after backend sync"
            : "Available after billing sync";
    }
}
