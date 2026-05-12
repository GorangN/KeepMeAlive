# KeepMeAlive Privacy and Data Flow

## Stored data

- `settings.json`
  - keep-alive method
  - interval and advanced input values
  - theme
  - startup and notification preferences
  - storage mode
  - privacy toggles for update checks and startup sync
  - scheduled action settings
- License key
  - never stored in plain text inside `settings.json`
  - stored in Windows Credential Manager when `StorageMode = ProfileAppData`
  - stored in a DPAPI-protected local file when `StorageMode = PortableLocal`
- Runtime bootstrap
  - stored beside the executable as `KeepMeAlive.bootstrap.json`
  - records the selected storage mode before the main settings file is loaded

## Storage locations

### PortableLocal

- Settings: `<AppFolder>\Data\settings.json`
- Protected license data: `<AppFolder>\Data\Secrets\license.bin`
- Runtime marker: `<AppFolder>\KeepMeAlive.portable`
- Bootstrap: `<AppFolder>\KeepMeAlive.bootstrap.json`

### ProfileAppData

- Settings: `%APPDATA%\KeepMeAlive\settings.json`
- Protected license data: Windows Credential Manager entry `KeepMeAlive/LicenseKey`
- Bootstrap: `<AppFolder>\KeepMeAlive.bootstrap.json`

## Network calls

- Automatic update checks
  - disabled by default
  - optional unauthenticated `GET` to `https://api.github.com/repos/GorangN/KeepMeAlive/releases/latest`
- Startup account/license sync
  - disabled by default
  - current implementation is a local no-op stub and performs no network traffic
- Manual release page open
  - opens the GitHub releases page in the default browser only when the user clicks the UI action

## Registry writes

- `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
  - written only when the user explicitly enables Windows startup
  - removed when the user disables Windows startup
- Theme detection reads:
  - `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize`

## Explicit non-goals

- No attempt to hide from antivirus, EDR, enterprise monitoring, or endpoint controls
- No code packing, obfuscation, or stealth startup behavior
- No telemetry, analytics, or usage reporting in the current implementation
