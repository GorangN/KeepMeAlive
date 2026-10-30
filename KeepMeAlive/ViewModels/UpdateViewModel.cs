using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeepMeAlive.Services.Interfaces;
using System.Reflection;

namespace KeepMeAlive.ViewModels;

/// <summary>Exposes user-initiated release checks and their result.</summary>
public partial class UpdateViewModel : ObservableObject
{
    private const string DefaultReleaseUrl = "https://github.com/GorangN/KeepMeAlive/releases/latest";

    private readonly IUpdateService _updateService;
    private readonly IBrowserLauncher _browser;

    [ObservableProperty]
    private bool _updateAvailable;

    [ObservableProperty]
    private string _updateLabel = "Automatic update checks are off by default. Use Check Now to query GitHub releases.";

    [ObservableProperty]
    private bool _isChecking;

    /// <summary>Gets the running application version.</summary>
    public string AppVersion { get; } =
        Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion?.Split('+')[0] ?? "1.0.0";

    /// <summary>Creates the release-check controls.</summary>
    /// <param name="updateService">The release metadata service.</param>
    /// <param name="browser">The system browser adapter.</param>
    public UpdateViewModel(IUpdateService updateService, IBrowserLauncher browser)
    {
        _updateService = updateService;
        _browser = browser;
    }

    [RelayCommand]
    private async Task CheckAsync(CancellationToken cancellationToken)
    {
        if (IsChecking)
        {
            return;
        }

        IsChecking = true;
        UpdateLabel = "Checking for updates...";

        try
        {
            var hasUpdate = await _updateService.CheckForUpdateAsync(cancellationToken);
            if (hasUpdate)
            {
                UpdateAvailable = true;
                UpdateLabel = $"v{_updateService.LatestVersion} available";
                return;
            }

            UpdateAvailable = false;
            UpdateLabel = string.IsNullOrWhiteSpace(_updateService.LatestVersion)
                ? "Update check failed."
                : "You are up to date.";
        }
        catch (Exception exception) when (exception is System.Net.Http.HttpRequestException or OperationCanceledException or System.Text.Json.JsonException)
        {
            UpdateAvailable = false;
            UpdateLabel = "Could not reach GitHub. Check your connection and try again.";
        }
        finally
        {
            IsChecking = false;
        }
    }

    [RelayCommand]
    private void OpenReleasePage()
    {
        try { _browser.Open(new Uri(DefaultReleaseUrl)); }
        catch (System.ComponentModel.Win32Exception) { UpdateLabel = "Could not open your browser. Visit the KeepMeAlive releases page on GitHub."; }
    }
}
