namespace KeepMeAlive.Models;

/// <summary>Identifies the currently active navigation page in the sidebar.</summary>
public enum NavigationPage
{
    /// <summary>Dashboard — status, start/stop controls.</summary>
    Dashboard,

    /// <summary>Methods — keep-alive method and interval configuration.</summary>
    Methods,

    /// <summary>Scheduling — scheduled system action configuration.</summary>
    Scheduling,

    /// <summary>Advanced — advanced technical options.</summary>
    Advanced,

    /// <summary>License — license key and application preferences.</summary>
    License,
}
