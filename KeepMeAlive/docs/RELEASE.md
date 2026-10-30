# Release procedures

## Validation build

1. Restore locked dependencies and run the Release build, all desktop tests, backend type checks/tests, and dependency audits.
2. Run `scripts/Build-Packages.ps1` on Windows. It publishes with runtime restore enabled and the portable profile's separate `KeepMeAlive/packages.publish.lock.json`, avoiding the old RID-less restore/`--no-restore` failure. Normal development uses `packages.lock.json`; publishing pins the current bundled runtime patch separately.
3. Inspect `artifacts/packages`: complete self-contained portable ZIP, per-user installer, SHA-256 checksums, and dependency inventory.
4. Run `scripts/Test-Package.ps1` and complete the native UI/fresh-user checks in `RELEASE_READINESS.md`.

Native checks have already exercised all pages in light/dark appearance, calendar/dropdown controls, invalid scheduling and numeric input, accessible labels, Cancel/Discard, Tab/Space, minimum-size resizing, minimizing, secondary-instance restoration, close-to-tray behavior, and Quit during a running keep-awake session. Repeat these against the exact release binaries. Fresh-user installer/uninstaller testing, actual OS high-contrast and DPI switching, screen-reader coverage, and native tray-menu actions still require acceptance.

Calendar-only selection is also verified natively after enabling the schedule: attempted date typing leaves the value unchanged, Alt+Down opens the calendar, Return closes it, and Discard leaves the action disarmed. Verify maximize/restore against the monitor work area and taskbar before release; the focused window-bounds correction includes eight geometry regression cases, with native acceptance still required.

The portable ZIP sorts file names and normalizes entry timestamps for repeatable output from identical publish files. .NET builds use deterministic compilation and pinned dependency lockfiles. Authenticode timestamping necessarily changes signed bytes. Retain the exact verified artifacts and their checksums instead of rebuilding after acceptance.

When intentionally updating dependencies or the bundled runtime patch, regenerate both dependency graphs with `dotnet restore KeepMeAlive.sln --force-evaluate` and `dotnet restore KeepMeAlive/KeepMeAlive.csproj -p:PublishProfile=Portable --force-evaluate`, then review both lockfiles. Regular CI and packaging restores remain locked.

`Build-Packages.ps1 -SkipBuild` repackages an existing `artifacts/publish-portable` directory. It still verifies the self-contained layout, compiles the installer, and checks package contents. Use it only after the build/tests for those exact binaries have passed.

## Production validation and signing

Complete every release-readiness gate. Import the intended certificate into the release runner's current-user certificate store and set `WINDOWS_SIGNING_CERT_THUMBPRINT`. The packaging script signs the main executable before creating the archive, signs the installer, and checks trusted Authenticode status. It rejects self-issued, expired, or private-key-less certificates.

Set `KEEP_ME_ALIVE_SMTP_VERIFIED=true` and `KEEP_ME_ALIVE_OAUTH_VERIFIED=true` only after recording real staging acceptance. Then run:

```powershell
./scripts/Build-Packages.ps1 -RequireProduction
```

The script also requires `accountReleaseReady: true` in `backend/neon.json` and checks that the active desktop configuration matches that file's production public client, API, issuer, and audience. Flags alone cannot release a package with disabled sign-in or development endpoints. `scripts/Test-ProductionGates.ps1` exercises these rejection paths without producing packages or changing real account configuration.

CI imports configured PFX secrets into an ephemeral runner and removes imported signing material after the job. Never commit PFX files, passwords, management credentials, database URLs, or session tokens. The current workflow produces reviewable artifacts and does not create a GitHub release automatically.

## Backend promotion

Use the dedicated development Neon branch for migrations and authentication tests. Run `scripts/Test-Backend.ps1 -BaseUrl <development-api-url>` for the live health/unauthenticated smoke check, then complete authenticated two-user and email-delivery acceptance described in the backend setup guide.

Before applying a production migration, record the production branch ID, current deployed Function version, schema version, and available recovery window/snapshot. Apply versioned additive migrations and deploy the exact validated backend bundle. Configure production Auth issuer/audience/client and confirm health and authorization behavior before enabling desktop sign-in for that endpoint.

## Staging account validation

`artifacts/staging-validation/KeepMeAlive-Staging-Validation.zip` is a separate unsigned account-validation build. Its binaries match the tested self-contained output; only `neon.json` selects the isolated staging root's public client, issuer and API. Extract it to a separate folder and quit other KeepMeAlive instances before launch. Use staging test identities, then Sign out before discarding the validation folder. Do not distribute this as a production account release.

To reproduce, package a fresh clean portable publish directory, replacing only `neon.json` with `KeepMeAlive/neon.staging.json`. Include the portable marker, pinned runtimes and Tabler license; exclude Data, Secrets, bootstrap files, settings, logs and environment files. Keep the normal production package and repository's active configuration unchanged. `tasks/verification/native-packages-2026-10-30.json` records the validation archive's current checksums and checks.

Actual Windows staging acceptance and screenshots are recorded in `tasks/verification/windows-neon-2026-10-29.json` and `tasks/verification/windows-checks-2026-10-30.json`. Browser callback, profile save, DPAPI session storage, explicit restart restoration, sign-out, account switching, cancellation and native service-outage/recovery passed. High-contrast control fixes and desktop/backend artifact rollback were verified natively. Custom SMTP receipt, trusted signing, database recovery and the remaining environment-specific Windows gates still require acceptance. Exact credential and signing-runner inputs are in `PRODUCTION_INPUTS.md`.

## Rollback and recovery

- **Desktop**: retain the last verified installer and portable ZIP. Stop the app, restore the previous binaries, and retain the user's settings/protected data. Test compatibility with the current settings format before rollout.
- **Backend**: retain the previous deployment bundle and configuration. Redeploy that known-good bundle to the same branch if compatible with the current schema. Do not roll back schema by dropping data.
- **Database**: use Neon's retained history or a recorded snapshot to restore into a separate recovery branch. Verify schema, row counts, authorization, and application behavior there before changing traffic. Overwriting/resetting production requires an explicit operator decision; never use an automatic destructive reset as rollback.
- **Local settings**: `.bak` contains the preceding complete preferences. Stop the app before manual restoration. Damaged originals and migration source copies are retained for recovery. Session recovery files contain encrypted credentials and must remain private; sign-out clears the active session and matching migrated copies while preserving unrelated sessions and license recovery records.

## Installer behavior

Installation is per-user and needs no administrator prompt. The installed app excludes the portable marker, so settings default to the Windows profile. Uninstall removes installed files and only the matching current-user startup registration. Profile preferences and protected sign-in are retained; sign out before uninstall to clear local session material.
