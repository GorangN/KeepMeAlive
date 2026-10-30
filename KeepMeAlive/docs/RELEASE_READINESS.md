# Release readiness

This file records prerequisites that cannot be replaced by a successful compile or an unsigned validation package. Do not mark a production account release ready until all gates have evidence.

## Account service

- [ ] Application-owned SMTP is configured in Neon Auth.
- [ ] Real verification and password-reset messages are received through that SMTP provider.
- [x] Production and isolated staging OAuth Providers/public desktop clients are configured with exact loopback redirects, consent enabled, and S256 PKCE advertised.
- [x] Hosted login and consent pages complete sign-in in a supported system browser, with the exact loopback callback and S256 exchange verified against staging.
- [x] Staging protocol acceptance verifies callback state, token refresh rotation, logout, two-user isolation, and invalid/expired/wrong-issuer/wrong-audience rejection.
- [x] Actual Windows app completes staging browser sign-in, profile saving, DPAPI-protected restart restoration, logout, browser account switching, and native cancellation. The local utility starts/stops while signed out.
- [x] Actual Windows app preserves cached account details during an account-service outage and recovers after the service returns. A physical network disconnection remains a separate unverified scenario.
- [x] Current Neon OAuth Provider configuration and staging browser/protocol behavior are verified; this app uses the standard HTTP protocol rather than the beta OAuth SDK.
- [ ] Development and production endpoints, branch identities, and runtime roles are separately verified.

## Distribution

- [ ] A publicly trusted code-signing certificate and its private key are available to the release workflow.
- [ ] Application executable and installer Authenticode signatures validate with the intended publisher identity.
- [ ] Fresh-user installation, launch, tray use, exit, and uninstall are verified on Windows.
- [x] Native light/dark navigation covers all pages, dropdowns, the calendar, and the 860 x 580 minimum window size.
- [x] Native checks verify invalid schedule rejection, Cancel/Discard dialogs, Tab/Space interaction, minimizing, secondary-instance restoration, and close-to-tray behavior.
- [x] Native invalid numeric input keeps Start disabled, accessible control labels are present, and Quit exits an active session with keep-awake enabled.
- [x] Native date entry remains unchanged after attempted typing, Alt+Down opens the calendar, Return closes it, and Discard leaves the action disarmed.
- [x] Maximizing respects the monitor work area without clipping the footer against the taskbar; restore preserves the window layout.
- [x] Actual Windows high contrast, including live entry/exit, dropdown options and selection, calendar dates/header and button names, is verified after correcting vendor control resources.
- [x] Actual Windows 100/150% scaling is verified; additional testing at the display's maximum offered 175% keeps Dashboard/sidebar actions reachable.
- [ ] Actual Windows 200% scaling is verified on a display/environment that offers it; this host's scaling menu ends at 175%.
- [ ] Screen-reader output, accessible names, focus order, and keyboard-only use are checked comprehensively.
- [ ] Native tray-menu Start/Stop/Show/Exit interactions are verified.
- [x] Backend artifact rollback is rehearsed in isolated staging, restoring the exact saved ZIP and recovering the native account.
- [x] Desktop artifact rollback and return to the current build preserve portable settings and pass native Start/Stop/Quit checks.
- [ ] Database recovery procedures have been rehearsed in development.

The existing local `EasyBluetoothAudio` self-signed certificate is not suitable for this application's public release and must not be reused.

## Account setup verification (2026-10-29)

The connected Neon plugin was used to add the production Function origin to the production Auth redirect allowlist and set the application name to KeepMeAlive on both branches. The matching Google Console session then enabled production OAuth Provider and registered a public desktop client with exactly three callbacks, consent enabled, and the account API audience. Its public ID is recorded in `backend/neon.json`; the active desktop client remains empty until acceptance and promotion pass.

Neon's root-branch restriction prevents enabling the provider on the existing development child, which did not inherit the root's new configuration. An isolated Frankfurt staging root project, `sweet-truth-01118709`, branch `br-red-flower-b9wmbkmb`, now has its own Auth issuer, restricted runtime role, schema/RLS, deployed API, and public client. Both existing branches were preserved. `KeepMeAlive/neon.staging.json` contains only public staging settings for controlled acceptance.

| Live check | Development child | Isolated staging root | Production root |
| --- | --- | --- | --- |
| Application `/readyz` | 200, KeepMeAlive JSON | 200, KeepMeAlive JSON | 404, Function not deployed |
| OAuth discovery at the configured issuer | 404 | 200 | 200 |
| Application-owned SMTP | Missing | Missing | Missing |
| Registered desktop public client | Not configured | Public, three exact callbacks | Public, three exact callbacks |

The initial plugin surface lacked OAuth Provider/public-client administration, so Console fallback completed that configuration without extracting connector/session credentials or creating a Management API key. The available GitHub identity belonged to a different Neon organization; the matching Google identity reached the intended organization and project. A fresh plugin inventory on 2026-10-29 now exposes OAuth server and public-client administration, and read-back confirms both configured public clients, all three exact callbacks, the API audience, and required consent. SMTP-provider and email-verification settings are still absent from the exposed tools; `update_auth_config` accepts the application name only.

The latest production and staging Auth read-back still reports shared development email delivery from `auth@mail.myneon.app`; sign-up/sign-in verification and required email verification are disabled. Exact SMTP and common email-provider environment names are absent at process, user, and machine scope, and the checked root/backend `.env` and `.neon` locations do not exist. Neither `neon` nor `neonctl` is installed. This bounded audit did not search personal mail, browser credentials, or unrelated projects. Safe current evidence is in `tasks/verification/smtp-status-2026-10-29.json`.

Completion requires application-owned SMTP host/port/username/password, a provider-verified sender address, and a recipient mailbox where verification and reset messages can actually be checked. Neon's [production checklist](https://neon.com/docs/auth/production-checklist) requires a custom provider for production; shared SMTP does not support this application's verification links. With authorized credentials available, configure through Console or the existing `backend/scripts/configure-smtp.mjs` using an authorized `NEON_API_KEY`, accept the SMTP send test, require email verification, and verify both received links before production promotion. The exposed plugin cannot supply these missing credentials or perform those email-setting changes. No email was sent and no cloud configuration changed during this recheck. Never place credentials in a chat, committed file, desktop configuration, or package. `accountReleaseReady` remains false and the active production desktop client remains empty.

The setup scripts now resolve each target's project explicitly, support the isolated staging project, and refuse to enable a disabled child provider before making changes. No production Auth issuer was combined with a staging/development API.

The staging authorization URL redirected Chrome to the hosted login form, with the signed OAuth query preserved in registration navigation and no console errors. A subsequent real Brave browser acceptance completed hosted sign-in, credentialed cross-origin cookies, verified client consent, the exact `127.0.0.1:49152/callback/` callback, state validation, S256 code exchange, and authenticated account API access. The refresh credential was revoked after verification and the test listener exited. Safe evidence is retained in `tasks/verification/staging-browser-2026-10-29.json`, checked at 2026-10-29 17:12:25 UTC. This browser fixture does not establish native WPF protected-session storage or actual SMTP delivery.

Managed RP-initiated logout rejected this public client because its required end-session permission is not exposed by the current managed client configuration. The consent page now provides **Use another account**, which calls the supported credentialed sign-out API and returns to login with the original signed authorization query. Real Brave verification returned to an empty login form and the issuer's session endpoint displayed `null`, confirming the staging browser cookie was cleared. Interactive desktop authorization now requests `prompt=consent` so this account choice remains reachable when a previous consent grant is remembered. The regression assertion failed before that parameter was added, and all 89 desktop tests pass with the fix.

The isolated staging service passed 40 live HTTP protocol checks, recorded in `tasks/verification/staging-oauth-2026-10-29.json` at 2026-10-29 17:07:42 UTC. They covered exact issuer/endpoints, S256 PKCE, required consent, callback state, code exchange, persisted profile updates, two real synthetic users and ownership rejection, refresh rotation, revocation, sign-out/session restoration, and code reuse rejection. The final run revoked refresh credentials and signed out both sessions. Reserved `.test` accounts are retained; no verification/reset emails were requested. This is protocol evidence, separate from browser, protected desktop-session, and actual SMTP acceptance.

SMTP send-test acceptance is separate from inbox delivery and real verification/reset links. Both remain pending. Production remains undeployed and desktop production sign-in remains disabled. Trusted Authenticode signing and the remaining Windows acceptance checks above are also unresolved.

## Recorded automated validation (2026-10-29)

- The Release solution builds without warnings; all 107 desktop tests pass, including session-family isolation, startup registration ownership, cached-account outage/recovery and actual vendor-control contrast assertions.
- Offscreen WPF rendering covers light, dark, and simulated high-contrast palettes at 860/1020 DIP widths and 100/150/200% scale, with no binding errors. This does not replace the native keyboard, screen-reader, Windows high-contrast, and fresh-user checks above.
- The real DatePicker template's inner text field is verified read-only after Loaded and repeated disabled/enabled transitions, with dispatcher work drained. Its UI Automation value provider is read-only, the calendar button remains enabled, and SelectedDate still updates the ViewModel through its binding.
- Eight geometry regression cases verify monitor-work-area calculations used by the maximized window; native maximize/restore acceptance remains separate.
- Portable publishing includes .NET and Windows Desktop runtime 10.0.14. The installer compiles, and archive contents, runtime versions, and SHA-256 checksums are validated.
- Live development API smoke checks verify platform health, service readiness, and rejection of unauthenticated account requests.
- Twenty-nine backend/setup/page/helper tests and TypeScript checks pass, npm audit reports zero vulnerabilities, and the production packaging regression passes eight gate scenarios.
- Validation packages were refreshed after the interactive consent correction; runtime/content/checksum checks pass. Previous installer/ZIP/checksum/inventory artifacts are retained under `artifacts/previous-before-oauth-consent`.
- Production packaging rejects missing SMTP/OAuth acceptance and trusted signing prerequisites.

## Recorded native validation (2026-10-29)

An isolated self-contained portable staging copy completed the real system-browser-to-WPF OAuth flow after resetting Computer Use and starting a fresh signed authorization request. The native Account screen showed Connected, the expected synthetic staging identity, a real sync timestamp, and No subscription linked. Saving a display name succeeded. After Quit and relaunch, startup synchronization remained off; explicit Refresh restored the same user and persisted name without a browser. DPAPI session-file metadata was verified without decrypting credentials, and refresh atomically replaced the encrypted file. Native Sign out removed the active session and backup; Refresh stayed signed out. A new interactive sign-in exposed Use another account, which returned to an empty hosted login form. Cancel sign-in returned the app to Signed out. Start/Stop worked while signed out, and Quit exited the validation process. The normal profile's session/recovery files and startup registration remained unchanged. Evidence and screenshots: `tasks/verification/windows-neon-2026-10-29.json`.

The earlier proxy-based attempt was unverified: automatic approval review rejected a PowerShell app launch configured with a process-local test proxy as "blocked by policy". No equivalent workaround launch was attempted. On 2026-10-30, a safer staging-service outage completed native acceptance: authenticated account routes returned HTTP 503, the app retained the cached account and original sync time, and local Start/Stop worked. An automatic restoration deployed the exact saved Function ZIP as staging deployment 4 after the temporary deployment 3. Native Refresh returned Connected without another browser and advanced the sync time. A physical network disconnection was not simulated. Evidence: `tasks/verification/windows-outage-2026-10-30.json`.

## Windows follow-up verification (2026-10-30)

Actual Windows Night sky high contrast exposed vendor control defects absent from the earlier app-only palette checks: dropdown items and calendar dates became white on white, and selected text inherited a conflicting implicit TextBlock style. A high-contrast-only vendor resource dictionary now uses system foreground, background and selection colors, including the actual rendered text. It is removed when Windows returns to normal appearance. Native retesting verifies selected/unselected dropdown text, the chevron, calendar dates and month header. Calendar opener/previous/next buttons now expose semantic names instead of geometry strings. Keyboard Tab/Space activation, access-key Start/Stop/Quit and calendar Alt+Down/Return were exercised. Spoken screen-reader output remains unverified because the observation tools do not capture speech/audio.

The main display was tested at actual 100%, 150% and its maximum offered 175% scale, including Dashboard/sidebar reachability and maximize/restore. Its 1920 x 1080 scaling menu does not offer 200%; that requested check remains pending on a suitable environment. The original 100% scale, no-contrast setting and app/system light/dark preferences were restored. Native notification-area controls were not exposed as targetable windows, so tray-menu clicks remain unverified. A separate standard-user Windows account or VM is still required for fresh-user install/uninstall acceptance; changing `/DIR` would share the shipped AppId and profile.

The retained staging desktop package and current package were each launched in one isolated portable test directory, exercised with native keyboard Start/Stop/Quit, and both retained interval 67 and the local safety/network preferences. The older validated writer normalized JSON and omitted defaults without changing those preferences. This rehearses desktop artifact rollback and return, not a fresh-user installer test. The original profile's protected files and startup registration remained unchanged; staging browser/desktop sessions were signed out, session files removed, and test processes/listeners stopped.

Full evidence and screenshots: `tasks/verification/windows-checks-2026-10-30.json`. Current unsigned packages and retained previous artifacts: `tasks/verification/native-packages-2026-10-30.json`. Exact remaining SMTP and signing inputs, including the hardware/cloud-key limitation of the current PFX workflow: `KeepMeAlive/docs/PRODUCTION_INPUTS.md`. Production remains gated.

The running application was inspected in light and dark appearance across all pages. Native calendar and dropdown interaction, invalid schedule rejection without executing a system action, and Cancel/Discard navigation dialogs were exercised. Tab and Space operated controls, resizing reached 860 x 580, and minimizing behaved correctly.

A real secondary process exited after requesting that the existing instance reopen. Closing the main window hid it while its process remained running, confirming close-to-tray behavior. This does not establish the separate tray-menu action checks.

Invalid numeric text was verified to disable Start, and controls expose accessible labels. Quit was exercised while the keep-alive session was running with keep-awake enabled; the process exited. These targeted checks do not replace comprehensive screen-reader or keyboard-focus acceptance.

Native testing exposed a date-control lifecycle defect: WPF's [DatePickerTextBox enabled-state handler](https://github.com/dotnet/wpf/blob/main/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/Primitives/DatePickerTextBox.cs) resets IsReadOnly when enabled, overriding the original implicit style. The earlier disabled-only rendering assertion missed this. A presentation behavior now reconnects to the real template part on Loaded/template changes and reapplies local read-only state after the framework's enabled-state handler. The regression first failed against the style-only implementation and passes against this lifecycle fix.

The rebuilt package passed native date verification: the date child no longer exposed a settable value, Ctrl+A followed by typing left the date unchanged, Alt+Down opened the calendar, Return closed it, and Discard left the action disarmed. The Alt+Q Quit shortcut also exited the process.

Native maximizing revealed footer clipping where the maximized window extended beneath the taskbar. The monitor-work-area correction passed eight geometry regression cases and fresh native verification: maximize used 1920 x 1032 with the footer fully visible, restore returned to 1020 x 680, and Alt+Q exited the final process cleanly.

Fresh-user installation/uninstall, actual OS high contrast and DPI switching, comprehensive screen-reader/focus testing, and the tray-menu interactions remain pending. Offscreen rendering and targeted keyboard checks are recorded above with their narrower scope.

## CI enforcement

Push and pull-request workflows build, test, audit dependencies, and produce validation packages. They do not publish a public release.

A manual production validation run additionally requires:

- `KEEP_ME_ALIVE_DEV_API_URL` repository variable pointing to the tested development API.
- `KEEP_ME_ALIVE_SMTP_VERIFIED=true` and `KEEP_ME_ALIVE_OAUTH_VERIFIED=true`, set only after the acceptance evidence above is recorded.
- `WINDOWS_SIGNING_CERT_BASE64` and `WINDOWS_SIGNING_CERT_PASSWORD` repository secrets for the intended publicly trusted certificate.

Missing production gates fail the production packaging run. The SMTP/OAuth variables record completed operational acceptance; they do not perform email delivery or substitute for the staging tests.

Production packaging additionally requires recorded backend acceptance and a nonempty shipped public client ID that exactly matches the production client, issuer, API, and audience. `scripts/Test-ProductionGates.ps1` checks that absent acceptance, an empty client, and each environment mismatch fail before packaging; CI runs these checks.
