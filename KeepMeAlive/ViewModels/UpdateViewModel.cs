using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeepMeAlive.Services.Interfaces;
using System.Diagnostics;
using System.Reflection;

namespace KeepMeAlive.ViewModels;

public partial class UpdateViewModel : ObservableObject
{
    private const string DefaultReleaseUrl = "https://github.com/GorangN/KeepMeAlive/releases/latest";

    private readonly IUpdateService _updateService;

    [ObservableProperty]
    private bool _updateAvailable;

    [ObservableProperty]
    private string _updateLabel = "Automatic update checks are off by default. Use Check Now to query GitHub releases.";

    [ObservableProperty]
    private bool _isChecking;

    public string AppVersion { get; } =
        Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion?.Split('+')[0] ?? "1.0.0";

    public UpdateViewModel(IUpdateService updateService)
    {
        _updateService = updateService;
    }

    [RelayCommand]
    private async Task CheckAsync()
    {
        if (IsChecking)
        {
            return;
        }

        IsChecking = true;
        UpdateLabel = "Checking for updates...";

        try
        {
            var hasUpdate = await _updateService.CheckForUpdateAsync();
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
        finally
        {
            IsChecking = false;
        }
    }

    [RelayCommand]
    private void OpenReleasePage()
    {
        var url = string.IsNullOrWhiteSpace(_updateService.ReleaseUrl)
            ? DefaultReleaseUrl
            : _updateService.ReleaseUrl;

        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
