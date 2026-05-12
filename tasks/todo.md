# KeepMeAlive Privacy-First Portable Hardening

- [x] Add task tracking and lessons files for the repo workflow.
- [x] Introduce runtime storage detection, bootstrap persistence, and storage migration support.
- [x] Remove plain-text license storage from settings and add secure storage providers.
- [x] Add privacy toggles and storage controls to the settings/account flows.
- [x] Gate startup network behavior behind explicit settings and add a startup sync stub.
- [x] Add portable publish profile, CI artifact packaging, and signing hooks.
- [x] Update privacy and release documentation to match the new behavior.
- [x] Verify `Release` build and portable publish output with zero warnings.

## Review

- `dotnet build KeepMeAlive.sln -c Release` succeeded with `0` warnings and `0` errors.
- `dotnet publish KeepMeAlive\KeepMeAlive.csproj -c Release -p:PublishProfile=Portable` succeeded.
- Portable publish output contains the expected `KeepMeAlive.portable` marker beside `KeepMeAlive.exe`.

# MahApps Dropdown Refresh

- [x] Add MahApps.Metro and wire its dropdown resources into the WPF application.
- [x] Rebase the app dropdown styling on MahApps for ComboBoxes and tray menus while preserving the existing palette and typography.
- [x] Verify the `Release` build still completes with zero warnings and zero errors.

## Review

- `dotnet build KeepMeAlive.sln -c Release` succeeded with `0` warnings and `0` errors after the MahApps dropdown refresh.

# README Repair

- [x] Confirm whether `README.md` is actually corrupted or only mis-decoded locally, including a check against the GitHub-rendered page.
- [x] Identify every rendering-sensitive section that is currently broken (special characters, ASCII diagrams, relative doc/image links).
- [x] Apply the minimal README fix needed so GitHub renders the document correctly again.
- [x] Verify the repaired README locally, capture review notes here, and keep the rest of the repo untouched.

## Review

- Confirmed the README had two real problems: mojibake characters in the committed Markdown and dead image references under `docs/assets/*`.
- Replaced the broken asset references with stable text, normalized the README to ASCII-safe content, and swapped fragile Unicode art for GitHub-safe Mermaid / ASCII blocks.
- Verified `README.md` contains no non-ASCII characters and that all remaining repo-local links in the document resolve successfully.
