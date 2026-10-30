# Privacy and data flow

## Local preferences

Settings contain input method and interval, appearance, startup/notification preferences, network opt-ins, storage location, and one-time scheduling preferences. They do not contain passwords or refresh tokens.

- Portable settings: `<AppFolder>\Data\settings.json`.
- Profile settings: `%APPDATA%\KeepMeAlive\settings.json`.
- Settings writes use a temporary file, a complete flush, and atomic replacement. The preceding file is retained as `.bak`.
- Invalid primary files are preserved when defaults or a valid backup are used.
- Portable storage selection is stored in `<AppFolder>\KeepMeAlive.bootstrap.json`.
- Installed/profile storage selection is stored in `%APPDATA%\KeepMeAlive\runtime.json`. Existing sidecar bootstrap files remain readable for compatibility.

## Protected data

Refresh-session material is stored at `<DataDirectory>\Secrets\session.bin`, protected with DPAPI `CurrentUser`. Access tokens remain in memory. DPAPI data is bound to a Windows user and machine; portable data copied elsewhere may require sign-in again.

Legacy license keys use Windows Credential Manager (`KeepMeAlive/LicenseKey`) in profile mode and DPAPI-protected `Secrets\license.bin` in portable mode. A saved key is not proof of an active entitlement.

Storage migration copies and verifies settings and secrets before committing the new location. Protected source copies and encrypted destination recovery snapshots can remain for recovery. Sign-out deletes the active session and its backup, plus matching copies of the same sign-in session in the other storage mode. Recovery snapshots lose only that session's material; legacy license recovery data and unrelated sessions remain. Each interactive sign-in has a distinct session identity that survives refresh rotation.

## Network behavior

- Manual browser sign-in talks to the configured Neon Auth endpoint and hosted authentication pages.
- Account requests send an access token to the configured API. The API stores a user profile and server-managed entitlements in Neon.
- Utility settings, machine names, input events, and usage history are not uploaded.
- Startup account synchronization is disabled by default. Enabling it permits session refresh and account requests during startup.
- Automatic update checks are disabled by default. Manual checks and enabled automatic checks use the GitHub Releases API.
- Opening a release page launches a validated GitHub HTTPS URL in the default browser.
- There is no telemetry, analytics, or third-party crash-reporting upload.

Neon production email delivery and the public desktop OAuth client must be configured before production account launch. The shared provider is suitable for development only.

## Windows integration

Startup registration uses the current user's `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` value and a quoted executable path. It is written only when enabled. Disabling startup or uninstalling removes only a registration that targets the current executable, preserving a different installation's registration.

The app reads the Windows appearance preference, observes lock/suspend state, and injects only the selected keyboard/mouse strategy during an active unlocked session. It does not read keystrokes or upload input information.

Diagnostics stay local, redact credential material, and use bounded retention. Clear local sign-in with Sign out; uninstall retains user preferences and protected data unless removed separately.
