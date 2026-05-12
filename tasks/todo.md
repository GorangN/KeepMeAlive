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
