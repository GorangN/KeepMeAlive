<div align="center">

<img src="docs/assets/banner.png" alt="KeepMeAlive Banner" width="100%" />

<br/>

# KeepMeAlive

**Keep your presence active â€” effortlessly.**

KeepMeAlive is a lightweight, privacy-respecting Windows utility that prevents Microsoft Teams and similar collaboration platforms from switching your status to *Away* during short breaks or hands-off periods.

<br/>

[![Release](https://img.shields.io/github/v/release/GorangN/KeepMeAlive?style=flat-square&color=0078D4&label=Latest%20Release)](https://github.com/GorangN/KeepMeAlive/releases/latest)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/WPF-Windows%20UI-0078D4?style=flat-square&logo=windows&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/)
[![License](https://img.shields.io/github/license/GorangN/KeepMeAlive?style=flat-square&color=107C10)](LICENSE)
[![Build](https://img.shields.io/github/actions/workflow/status/GorangN/KeepMeAlive/dotnet-desktop.yml?style=flat-square&label=Build)](https://github.com/GorangN/KeepMeAlive/actions)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11-0078D4?style=flat-square&logo=windows11&logoColor=white)](https://www.microsoft.com/windows)

<br/>

<img src="docs/assets/screenshot-dashboard.png" alt="KeepMeAlive Dashboard" width="680" />

</div>

---

## Why KeepMeAlive?

Remote work means presence matters. A 5-minute coffee break or a quick call shouldn't leave your status stuck on *Away* â€” costing you missed messages, awkward follow-ups, or the appearance of being offline.

KeepMeAlive solves this with **a single, silent process** that keeps your session alive exactly when you need it, without interfering with your workflow or typing.

- No admin rights required
- No background services or drivers
- No data collection â€” fully offline
- Runs in your system tray, completely out of the way

---

## Features

<table>
<tr>
<td width="50%">

### <img src="https://img.shields.io/badge/-Keep--Alive%20Engine-0078D4?style=flat-square" /> Keep-Alive Engine

Two precision input strategies keep your session active:

- **Keyboard ping** â€” sends a harmless LeftShift scan code that registers as activity without affecting your work
- **Mouse nudge** â€” moves the cursor by 1â€“5 pixels and back, invisible to the eye

</td>
<td width="50%">

### <img src="https://img.shields.io/badge/-Smart%20Intervals-107C10?style=flat-square" /> Smart Intervals

- Configurable ping interval (60â€“90 seconds recommended)
- Built-in **random jitter** (Â±15 s) to avoid robotic patterns
- Live idle-time display so you can verify it is working

</td>
</tr>
<tr>
<td>

### <img src="https://img.shields.io/badge/-System%20Tray-5C2D91?style=flat-square" /> System Tray Integration

- Minimal footprint â€” lives entirely in the tray
- Dynamic icon reflects running / stopped state at a glance
- Show or hide the window with a single click

</td>
<td>

### <img src="https://img.shields.io/badge/-Display%20Keep--Awake-D83B01?style=flat-square" /> Display & Sleep Control

- Optional system keep-awake via `SetThreadExecutionState`
- Prevents display sleep during active keep-alive sessions
- Automatically restores normal power settings on stop

</td>
</tr>
<tr>
<td>

### <img src="https://img.shields.io/badge/-Themes-FFB900?style=flat-square&logoColor=black" /> Dark & Light Themes

- Dark, Light, and **System** (auto-follow Windows theme) modes
- Live theme switching â€” no restart required
- Clean, modern UI with Fluent-inspired aesthetics

</td>
<td>

### <img src="https://img.shields.io/badge/-Windows%20Startup-0078D4?style=flat-square" /> Windows Startup Integration

- Optional launch on Windows sign-in
- Starts silently in the tray â€” no window shown on boot
- One toggle in Settings â€” no manual registry editing

</td>
</tr>
</table>

---

## Screenshots

<div align="center">

| Dashboard | Settings | System Tray |
|:---------:|:--------:|:-----------:|
| <img src="docs/assets/screenshot-dashboard.png" width="220" alt="Dashboard" /> | <img src="docs/assets/screenshot-settings.png" width="220" alt="Settings" /> | <img src="docs/assets/screenshot-tray.png" width="220" alt="System Tray" /> |
| Start / Stop with method selection | Theme, startup & license | Tray icon with live status |

</div>

---

## Getting Started

### Requirements

| Requirement | Minimum |
|-------------|---------|
| Operating System | Windows 10 (1903) or Windows 11 |
| Runtime | [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) |
| Privileges | Standard user â€” no admin required |

### Installation

**Option A â€” Installer (Recommended)**

1. Download the latest `KeepMeAlive-Setup.exe` from [**Releases**](https://github.com/GorangN/KeepMeAlive/releases/latest)
2. Run the installer and follow the setup wizard
3. KeepMeAlive launches automatically and appears in your system tray

**Option B â€” Portable**

1. Download `KeepMeAlive-Portable.zip` from [**Releases**](https://github.com/GorangN/KeepMeAlive/releases/latest)
2. Extract to any folder (e.g., `C:\Tools\KeepMeAlive`)
3. Run `KeepMeAlive.exe` â€” settings are stored in `%APPDATA%\KeepMeAlive`

---

## How It Works

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚  KeepMeAlive                                                  â”‚
â”‚                                                                 â”‚
â”‚   Dashboard â”€â”€â–º DashboardViewModel â”€â”€â–º IKeepAliveStrategy       â”‚
â”‚                       â”‚                    â”‚                    â”‚
â”‚                        â”€â”€â–º IIdleMonitor     â”œâ”€ KeyboardStrategy  â”‚
â”‚                                            â””â”€ MouseStrategy     â”‚
â”‚                                                    â”‚            â”‚
â”‚                              Win32 SendInput() â—„â”€â”€â”˜            â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

1. You configure the method and interval on the **Dashboard**
2. On **Start**, a `DispatcherTimer` fires at your chosen interval (with random jitter)
3. The selected strategy calls `SendInput()` â€” a Windows API that injects a minimal, invisible input event
4. Your system (and Teams) registers activity â†’ your status stays **Available**
5. The **Idle Monitor** reads `GetLastInputInfo()` every second and shows your real idle time, confirming it is working

---

## Notes

> **Does not work on a locked PC.** Windows and Teams will still show *Away* when the session is locked â€” KeepMeAlive requires an active, unlocked session.

> **Enterprise security policies** may block synthetic input on managed devices. Use the live idle-time display to verify your environment supports it before relying on it.

---

## Architecture

KeepMeAlive is built on **Clean Architecture** with strict **MVVM** separation and **Dependency Injection** throughout.

```
KeepMeAlive/
â”œâ”€â”€ Models/                  # Domain â€” AppSettings, LicenseInfo, SubscriptionTier
â”œâ”€â”€ Services/
â”‚   â”œâ”€â”€ Interfaces/          # Abstractions â€” IKeepAwakeService, IIdleMonitorServiceâ€¦
â”‚   â”œâ”€â”€ Keyboard/            # KeyboardStrategy_ScanCodeShift, KeyboardStrategy_VirtualKey
â”‚   â”œâ”€â”€ Mouse/               # MouseStrategy
â”‚   â”œâ”€â”€ KeepAwakeService     # SetThreadExecutionState wrapper
â”‚   â”œâ”€â”€ IdleMonitorService   # GetLastInputInfo wrapper
â”‚   â”œâ”€â”€ SettingsService      # JSON persistence â†’ %APPDATA%\KeepMeAlive
â”‚   â”œâ”€â”€ ThemeService         # System theme detection + live switching
â”‚   â”œâ”€â”€ StartupService       # HKCU Run registry key management
â”‚   â””â”€â”€ UpdateService        # GitHub Releases API version check
â”œâ”€â”€ ViewModels/              # MainViewModel, DashboardViewModel, SettingsViewModel
â”œâ”€â”€ Views/                   # DashboardView.xaml, SettingsView.xaml (zero code-behind)
â”œâ”€â”€ Messages/                # WeakReferenceMessenger message types
â”œâ”€â”€ Helpers/                 # Interop.cs â€” P/Invoke declarations
â””â”€â”€ Resources/
    â”œâ”€â”€ Icons/               # tray_active.ico, tray_inactive.ico
    â””â”€â”€ Themes/              # DarkTheme.xaml, LightTheme.xaml
```

**Key patterns:**
- **Strategy Pattern** â€” swap keep-alive methods without changing the timer logic
- **Messenger (WeakReference)** â€” decoupled ViewModel-to-ViewModel communication
- **Single-Instance Enforcement** â€” Mutex + Named Pipe IPC; secondary launches bring the window to focus

**Stack:**

| Component | Technology |
|-----------|-----------|
| UI Framework | WPF (.NET 8) |
| MVVM | CommunityToolkit.Mvvm 8.4 |
| DI Container | Microsoft.Extensions.DependencyInjection 9 |
| System Tray | Hardcodet.NotifyIcon.Wpf 1.1 |
| Settings | System.Text.Json |
| IPC | System.IO.Pipes (Named Pipes) |

---

## Building from Source

```bash
# Clone the repository
git clone https://github.com/GorangN/KeepMeAlive.git
cd KeepMeAlive

# Restore dependencies and build
dotnet restore
dotnet build -c Release

# Run
dotnet run --project KeepMeAlive/KeepMeAlive.csproj
```

> Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) and Windows.

---

## Roadmap

- [x] Keyboard & mouse keep-alive strategies
- [x] Configurable intervals with random jitter
- [x] System tray with dynamic status icon
- [x] Dark / Light / System theme support
- [x] Windows startup integration
- [x] Live idle-time display
- [x] Single-instance enforcement with IPC
- [x] GitHub auto-update check
- [ ] License & subscription activation
- [ ] Scheduled keep-alive profiles (work hours only)
- [ ] Per-application trigger (activate only when Teams is running)
- [ ] Notification suppression mode

---

## Privacy

KeepMeAlive operates **entirely on your local machine**. It does not:

- Transmit any usage data, telemetry, or analytics
- Read, capture, or log your keystrokes
- Require an internet connection (except for the optional GitHub update check)

The sole network call is an unauthenticated `GET` to the GitHub Releases API for version comparison. This can be disabled in Settings.

---

## Contributing

Contributions are welcome. Please follow the established code style defined in [CLAUDE.md](CLAUDE.md) â€” zero warnings, XML documentation on all public members, strict MVVM with no logic in code-behind.

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature`
3. Commit with a descriptive message linked to the relevant issue
4. Open a Pull Request against `main`

---

## License

Distributed under the [MIT License](LICENSE).

---

<div align="center">

Made for remote workers who just need a quick coffee break.

<br/>

[![GitHub Stars](https://img.shields.io/github/stars/GorangN/KeepMeAlive?style=social)](https://github.com/GorangN/KeepMeAlive/stargazers)
&nbsp;
[![GitHub Forks](https://img.shields.io/github/forks/GorangN/KeepMeAlive?style=social)](https://github.com/GorangN/KeepMeAlive/network/members)

</div>

