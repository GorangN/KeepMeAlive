using System.Diagnostics;
using KeepMeAlive.Services.Interfaces;

namespace KeepMeAlive.Services;

/// <summary>Uses Windows shell navigation for external authentication.</summary>
public sealed class SystemBrowserLauncher : IBrowserLauncher
{
    /// <inheritdoc/>
    public void Open(Uri uri)
    {
        if (!uri.IsAbsoluteUri || uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new ArgumentException("Authentication requires a valid HTTPS URL.", nameof(uri));
        }
        Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
    }
}
