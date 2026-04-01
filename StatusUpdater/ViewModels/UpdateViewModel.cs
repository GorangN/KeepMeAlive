using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KeepMeAlive.Services.Interfaces;
using System.Diagnostics;
using System.Reflection;

namespace KeepMeAlive.ViewModels;

public partial class UpdateViewModel : ObservableObject
{
    private readonly IUpdateService _updateService;

    [ObservableProperty]
    private bool _updateAvailable;

    [ObservableProperty]
    private string _updateLabel = "";

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
        var hasUpdate = await _updateService.CheckForUpdateAsync();
        if (hasUpdate)
        {
            UpdateAvailable = true;
            UpdateLabel = $"v{_updateService.LatestVersion} available";
        }
    }

    [RelayCommand]
    private void OpenReleasePage()
    {
        var url = _updateService.ReleaseUrl;
        if (!string.IsNullOrEmpty(url))
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
