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

# Production readiness, Neon and Apple design

- [x] Inspect runtime, persistence, backend, packaging and design constraints; approve execution plan.
- [x] Install Taste, anti-slop and ECC; add Karpathy guidelines and design direction.
- [x] Upgrade .NET, enforce architecture, documentation and warning gates.
- [x] Fix engine, power, scheduling, settings, secret migration and startup reliability.
- [x] Provision isolated Neon infrastructure and implement account/API/auth flows.
- [x] Restyle WPF shell and all states; verify accessibility and native behavior.
- [x] Add automated tests, release packages, installer and CI checks.
- [x] Record verification results and concrete remaining production gates.

## Production-readiness implementation review

- Migrated to .NET 10 with separate Core/Application/Infrastructure/Presentation assemblies; warnings and XML documentation are enforced. Portable packages pin both bundled runtimes to 10.0.14 using a separate publish lockfile.
- Installed project Taste and anti-slop skills, native ECC plugin, and the user-supplied Karpathy guidelines. Apple-inspired resources are shared across light/dark/high-contrast fallbacks.
- Dedicated Neon project morning-bread-41310427 has development/production branches, Auth, schema and restricted runtime role. Development API is deployed; readiness and anonymous authorization smoke tests pass. Production account activation remains gated in backend/README.md.
- Native verification after resetting Computer Use: packaged launch, Start/countdown/Stop, light/dark all pages and popup controls, keyboard focus/activation, invalid schedule refusal, Cancel/Discard, minimum 860×580 resizing, minimize/secondary-instance restore and close-to-tray/reopen passed. No shutdown/restart/sleep action was executed.
- Native inspection found missing accessible names, malformed numeric text retention, and a typed-date fallback hazard. Added names, keyboard Quit, string editor validation, a calendar-only date selector, and regression cases. The current automated suite passes 89 tests with zero build warnings; 5 backend tests, strict TypeScript and dependency audits pass.
- Offscreen WPF checks cover all pages in light/dark/simulated high contrast, two sizes and 100/150/200% render scales without binding errors. Actual OS high contrast/DPI, broad screen-reader coverage, native tray-menu clicks, and fresh-user installation/uninstallation remain manual release gates.
- [ ] Complete operational SMTP/OAuth and live account acceptance, trusted signing, and remaining manual checks in KeepMeAlive/docs/RELEASE_READINESS.md before a public production release.

- Native final-package retest confirmed malformed interval text disables Start with a helpful message, correcting the interval to 60 restores Start, navigation and input controls expose accessible names, and Quit exits an active keep-awake session with no process remaining.

- Final native acceptance: enabled date field rejects typing while Alt+Down/Return operate the calendar; maximize uses the monitor work area (1920×1032), restores to 1020×680, and Alt+Q exits cleanly. All 89 tests pass, builds have zero warnings, and final portable/installer runtime and SHA-256 checks pass.

# Complete Neon OAuth and SMTP

- [x] Inspect connected Neon auth configuration, supported setup APIs, existing Console access and service credentials.
- [x] Configure public desktop OAuth clients, exact redirects, consent and origins through supported authenticated administration.
- [ ] Configure an application-owned production sender and verify verification/reset delivery.
- [x] Verify real browser sign-in/callback and live protocol refresh/logout/two-user isolation in the isolated staging root.
- [x] Verify hosted account switching clears the retained staging browser SSO session through the supported sign-out API.
- [ ] Complete real SMTP email acceptance before promotion; native WPF protected-session restart and service-outage/recovery are verified.
- [x] Record public production/staging client settings and concrete external prerequisites without falsely activating desktop sign-in.
- [ ] Promote production and update active desktop configuration/packages after SMTP and live acceptance gates pass.

## OAuth and SMTP setup review

- Added the exact production Function origin through the Neon plugin and verified it in Auth read-back; set the Auth application name to KeepMeAlive on both branches.
- Configured production OAuth Provider/public client through the matching Google Console session after the available GitHub identity led to a different organization. Production discovery now returns 200; its Function remains undeployed and active desktop client remains empty.
- Confirmed the existing development child did not inherit OAuth and cannot be configured through Console Auth settings. Provisioned an isolated staging root project in Frankfurt through Neon; its schema, RLS, restricted runtime role, Function and public OAuth client are ready for acceptance. No existing branches were reset or deleted.
- The connected plugin has no OAuth Provider/public-client, SMTP or verification-policy administration tools. Console fallback completed OAuth setup. The project environment and standard Neon CLI locations still have no application-owned SMTP credentials. No connector tokens were extracted or reused.
- Hardened the setup scripts to repair exact public-client callbacks/resources, detect the root-branch-only enablement constraint before mutation, test stored SMTP credentials safely, and enable required email verification only after the send test is accepted. Inbox delivery remains a separate required check.
- Added production packaging checks for recorded acceptance and matching nonempty production client/issuer/API/audience; the regression script verifies rejection paths with isolated temporary fixtures and is wired into CI.
- Development deployment 3 and isolated staging deployment 1 use the reviewed hosted-auth bundle; readiness/pages return 200, anonymous account access returns 401, and the previous deployment manifest/ZIP is retained for rollback.
- Live Chrome authorization redirected to staging sign-in without console errors; registration navigation preserved signed OAuth context. Subsequent Brave acceptance completed hosted login, cross-origin cookies, registered client consent, exact loopback callback, state validation, S256 code exchange and authenticated API access. The refresh credential was revoked and the listener exited. Safe evidence is retained in `tasks/verification/staging-browser-2026-10-29.json`; native WPF session storage and real email delivery remain separate.
- Isolated staging passed 40 live HTTP acceptance checks including S256 PKCE/consent/code exchange, callback state, two real synthetic users, profile persistence/ownership rejection, refresh rotation, revocation, logout, and code reuse rejection. Final sessions were signed out and refresh credentials revoked; reserved `.test` users are retained and no emails were requested. Evidence is `backend/dist/staging-oauth-acceptance.json`; browser/native-session and actual SMTP checks remain separate.
- Managed end-session logout is unavailable for this public client. Added the supported Use another account action to consent and verified browser sign-out, an empty login form, and a `null` managed session. Native interactive authorization requests consent so account switching remains reachable; its assertion failed before the fix and all 89 desktop tests pass afterward.
- Final verification passes 18 backend/setup/page tests, strict TypeScript, zero npm vulnerabilities, and eight production packaging gate scenarios. Validation portable/installer packages were rebuilt after the native consent change and pass runtime/content/checksum checks. Previous packages are retained under `artifacts/previous-before-oauth-consent`.
- Remaining external prerequisites: application-owned SMTP credentials and sender/test mailbox; live browser/email acceptance; intended trusted signing certificate. No release gate or production desktop client was falsely activated.

# Tabler-only icons

- [x] Inventory visible icon sources and map them to official Tabler outline assets.
- [x] Vendor pinned Tabler sources/license and reusable native WPF vector resources; replace app UI, tray and executable icon assets.
- [x] Record Tabler-only rules in AGENTS.md and DESIGN.md; preserve labels, commands and existing layouts.
- [x] Verify zero-warning build, rendered states and complete packages; record evidence and remaining unrelated release gates.

## Tabler icon review

- Replaced MDL2 glyphs and text-symbol icons with 14 official Tabler outline vectors pinned to commit `ed177a085b652fe04ec4f37ced867c94621e7840`. Navigation, section headers, window buttons, expander, dropdown, calendar, checkbox and scrollbar affordances share the same icon source. Stock native message-box/notification pictograms are omitted while their text and actions remain.
- Generated app/tray ICOs at eight resolutions and installer artwork from Tabler's activity icon. Source SVGs, MIT notice, provenance and checksums are retained; offline regeneration produces identical outputs. Distribution checks require the Tabler MIT notice.
- Preserved control templates through documented vendor hooks and resource overrides. WPF checks verify icon foreground inheritance, nonfocusable decorative vectors, calendar navigation/labels, checked/indeterminate states, dropdown geometry and scrollbar command directions.
- Corrected the rendering harness to restore each requested palette after settings-discard interactions and assert the actual background color. All pages now render in light, dark and simulated high contrast at two sizes and 100/150/200% render scales; this remains separate from actual Windows DPI/high-contrast acceptance.
- Previous package artifacts are preserved under `artifacts/previous-before-tabler-icons`; public-release SMTP/signing and native account acceptance gates remain recorded separately.
- Final build has zero warnings; all 89 tests pass. Portable/installer contents, runtime patch and checksums pass validation; packaged Tabler license bytes match the pinned source. Evidence and final hashes are recorded in `tasks/verification/tabler-icons-2026-10-29.json`.

# SMTP completion and actual Windows account acceptance

- [x] Re-check supported Neon/SMTP access and existing sender credentials without exposing secrets.
- [x] Prepare an isolated portable staging copy and verify native WPF browser sign-in, account identity and protected refresh storage.
- [x] Verify app restart, signed-out utility behavior, account switching, native cancellation and logout/session deletion.
- [x] Verify native account service-outage/recovery through a staging-only authenticated 503 deployment and automatic exact-bundle restoration (2026-10-30).
- [x] Complete supported setup/read-back checks and update exact evidence, packages and unresolved external gates; production promotion stays blocked by the listed prerequisites.

## Native account acceptance review

- Reset Computer Use after explicit retry authorization and started a fresh OAuth request from the actual WPF app. The system browser completed consent and the exact loopback callback; Connected displayed the expected synthetic staging account, real sync time and No subscription linked. Native profile saving passed.
- Quit/relaunch preserved DPAPI-protected session material with startup synchronization off. Explicit Refresh restored the same user and updated name without another browser; the encrypted primary was replaced and a backup created. Sign out deleted both, and subsequent Refresh remained signed out.
- The next native authorization exposed Use another account; it returned to an empty hosted login form. Native Cancel sign-in returned Signed out. Local Start/Stop passed without an account; the test app exited, listeners stopped and test tabs closed. Original profile session/recovery files and startup registration were unchanged.
- Fixed session logout ownership using a stable per-grant family preserved through refresh, retaining foreign sessions and legacy-key recovery data. Startup disable removes only the exact current executable's registration. Release tests pass 104/104 with zero build warnings. Backend tests pass 24/24, strict TypeScript and npm audit pass; production packaging rejection cases pass.
- Fresh Neon plugin read-back verifies public clients and exact callbacks, audience and consent. The plugin now exposes OAuth administration, but still has no SMTP/verification-policy setter. Application-owned SMTP credentials, verified sender and a mailbox for real verification/reset delivery are unavailable; shared SMTP remains and production readiness stays false.
- Rebuilt standard portable/installer and clean additional staging portable validation ZIP; all application binaries match native-tested QA, runtime/content/checksum/Tabler-notice checks pass, and no local data/secrets are packaged. Previous packages are retained under artifacts/previous-before-native-accounts.
- Native evidence/screenshots: tasks/verification/windows-neon-2026-10-29.json. SMTP read-back: tasks/verification/smtp-status-2026-10-29.json. Package checksums and reproducible steps: tasks/verification/native-packages-2026-10-29.json.
- Native network-outage/recovery remains unverified because earlier automatic approval review rejected the PowerShell launch configured with a process-local test proxy as "blocked by policy". No workaround launch was used. Trusted signing, production promotion/smoke, actual Windows high contrast/DPI, comprehensive screen-reader checks, tray-menu actions, fresh-user installation/uninstallation and recovery rehearsal remain release gates in KeepMeAlive/docs/RELEASE_READINESS.md.

# Remaining Windows acceptance (2026-10-30)

- [x] Record initial Windows appearance/scaling and normal profile state; launch isolated staging app through native Computer Use.
- [x] Verify cached-account service-outage/recovery using a temporary staging-only authenticated 503 deployment; restore exact saved backend artifact.
- [x] Exercise keyboard activation, focus rings, access-key Start/Stop/Quit and calendar button accessibility names.
- [ ] Native tray Start/Stop/Open/Exit clicks and spoken screen-reader output remain unverified: the available surface does not expose the notification area or capture speech/audio.
- [x] Verify actual OS 100/150/175% scaling and high contrast; restore initial settings and fix observed vendor-control and selected-text defects.
- [ ] Verify actual 200% scaling on a suitable environment; this display offers scaling only through 175%.
- [x] Rehearse retained desktop artifact rollback and return with preserved portable settings and native Start/Stop/Quit.
- [ ] Fresh-user installation/uninstallation requires a separate Windows account or VM; same-user directory changes share the shipped AppId/profile.
- [x] Update evidence, packages, exact remaining gates and SMTP/signing input documentation.

Assumptions: the existing staging root is disposable for a short account-service outage, while production and its credentials remain untouched. A staging HTTP 503 is service-outage acceptance, not proof of physical network disconnection. The current token is not elevated and no Windows Sandbox is installed; do not relabel same-user installation as fresh-user acceptance or bypass UAC.

## Windows follow-up review

- Native HTTP 503 acceptance retained the real cached identity, entitlements display and sync time; local utility Start/Stop continued to work. The watchdog restored the exact retained Function ZIP as staging deployment 4 after temporary deployment 3; native Refresh returned Connected without a browser and updated the timestamp. Authentication still rejected anonymous/invalid requests. Production and network settings were unchanged.
- Actual Night sky contrast exposed white-on-white vendor controls and selection text overridden by the global implicit TextBlock style. Fixed only high-contrast vendor resources and local content templates, asserted real rendered leaf brushes, and verified selected/unselected dropdowns, arrow, calendar dates/header and semantic opener/previous/next names in the native app. Normal palette restoration also passed.
- Actual Windows 100/150/175% rendering and 175% maximize/restore keep main actions reachable. Original 100% scaling, no contrast, app light/system dark preferences were restored; normal profile protected files and startup registration were unchanged. Staging browser/desktop sessions were signed out, QA primary/backup credentials removed, and all test apps/listeners exited.
- Retained and current desktop artifacts both read portable interval 67 and passed native Alt+S Start, Alt+T Stop and Alt+Q Quit. The older writer normalized omitted defaults/formatting while preserving settings semantics; this is rollback compatibility, not fresh-user installer acceptance.
- Final validation: 107 desktop tests, 29 backend tests, strict TypeScript, zero reported npm/NuGet vulnerabilities, zero build warnings, and package content/runtime/checksum checks pass. Refreshed standard/staging packages retain the Tabler license and contain no user data/secrets; previous artifacts are preserved under artifacts/previous-before-windows-hc.
- Evidence: tasks/verification/windows-checks-2026-10-30.json, windows-outage-2026-10-30.json, desktop-rollback-preparation-2026-10-30.json and native-packages-2026-10-30.json. Exact credentials/key integration inputs are in KeepMeAlive/docs/PRODUCTION_INPUTS.md. Unavailable Windows checks, database recovery rehearsal, SMTP receipt, trusted signing and production promotion remain explicit gates.

# Commit to main and explain publishing

- [x] Review all candidate files for secrets, personal data and generated build outputs; confirm remote main has no unexpected changes.
- [x] Add a beginner publishing guide explaining SMTP and separating source push from public application/account release.
- [x] Commit the reviewed implementation, project skills, tests and safe evidence on main; push without force.
- [x] Verify local/remote commit equality, a clean tracked/untracked working tree and the resulting CI state; report exact remaining release actions in plain language.

The user explicitly requested a direct commit and push to main. This authorizes that branch choice; do not create a new worktree or require a PR solely because a generic workflow skill recommends one. Build artifacts and local secrets stay ignored.

## Publishing hand-off review (2026-10-30)

- Reviewed 318 candidate files; no real credentials, private account data or generated distribution packages were included. Whitespace checks pass. Corrected the advanced-input caption to identify virtual key 126 as F15.
- Committed the implementation, project skills, tests, safe native evidence and beginner publishing guide as `32a35ca9d2f9406a03ba64334cdf75ea4330fe3f` and pushed normally to canonical `https://github.com/GorangN/KeepMeAlive.git`, branch `main`. Remote HEAD matched local HEAD and the working tree was clean after that push.
- A fresh local Release build reports zero warnings/errors; all 107 desktop tests pass. GitHub [Verify and package run 41043405430](https://github.com/GorangN/KeepMeAlive/actions/runs/41043405430) passed both backend and Windows jobs, including tests, audits, production-guard regression and complete installer/portable packaging. The unsigned validation artifact was uploaded. Production-only email, live-backend and signing steps were not enabled by this source push.
- Added `KeepMeAlive/docs/PUBLISHING_STEP_BY_STEP.md` and linked it from README. It explains SMTP as email delivery, gives verified provider/Neon setup instructions, documents signing and remaining acceptance, and distinguishes committing, building and manually publishing downloads.
- No public release or version tag was created. Production accounts remain disabled until real email delivery/link acceptance, trusted signing, controlled production promotion and the documented remaining Windows/database checks pass. The existing project version is 1.0.0 while the previous public release is v1.0.1; the guide requires a higher unused version and a build from its exact tested commit before release.

# Resend email setup and account acceptance (2026-11-02)

- [x] Inspect the authorized Brave Resend session, verified domains and application sending access without extracting unrelated credentials.
- [x] Configure sending-only Resend test SMTP in Neon staging; verify actual inbox delivery, required verification and password reset through the API.
- [x] Visually confirm native Connected after required verification and a successful explicit Refresh; completed on the authorized Computer Use retry.
- [ ] Complete password-reset browser-form acceptance; actual reset-email/link/API acceptance has passed separately.
- [ ] Configure verified-domain production email and complete production acceptance; blocked by the absent owned domain/DNS configuration. Production remains untouched and gated.
- [x] Finish isolated app/browser/temp-link cleanup on the explicitly authorized retry; native sign-out/Quit, browser logout, file removal and absence checks passed.
- [x] Resolve newly reported build-tool dependency advisories without a breaking runtime downgrade; verify the resulting bundle.
- [x] Record redacted setup/acceptance evidence and exact remaining blockers; verify relevant code/configuration changes.
- [x] Commit and push the reviewed follow-up to main; verify clean local/remote state and GitHub validation.

The user authorized completing setup through their logged-in Brave account without questions. Use existing authorized resources, separate staging and production, and record missing domain, mailbox or signing capabilities rather than inventing them. Do not purchase services or reuse unrelated certificates.

## Resend follow-up review

- Configured a sending-only Resend staging key in Neon's managed SMTP settings and enabled required verification links. Real SMTP-test, verification and reset emails reached the matching owner inbox. Unverified sign-in was rejected; verification reached the native consent page and the verified flag was checked through read-only SQL. Reset link, API password reset, new-password sign-in and API-session logout passed.
- Staging deployment 5 contains the two hosted-email fixes, with regression cases that failed before implementation. All 32 backend tests, strict TypeScript, clean installation, Function build and dependency audit pass. New build-tool advisories were resolved with a scoped NFT/glob override, including a real dynamic-asset tracing check. The new bundle's uncompressed entry is byte-identical to the deployed bundle; its retained archive and new archive differ only in metadata.
- Production remains untouched and gated because Resend has no verified owned domain/DNS setup. Native Connected-screen confirmation stopped at the Computer Use URL-policy check; read-only metadata proves callback/code exchange and a new DPAPI-protected session, with no credential decryption and no remaining loopback listener. Automatic approval rejected isolated process termination and temporary-link removal with only **blocked by policy**; no equivalent cleanup bypass was attempted. The separate API test session was signed out and temporary API credential references were cleared.
- Evidence is `tasks/verification/resend-smtp-2026-11-02.json`; it excludes personal recipient addresses, raw mail, keys, passwords, cookies and email tokens. Public production SMTP acceptance variables remain unset, no client was activated and no release/tag was published.
- Pushed implementation `60c3026cfaf713abe435e725a097d4d25000e695` to `main`, verified local/remote equality and a clean source tree, and passed both jobs in [GitHub run 41376895125](https://github.com/GorangN/KeepMeAlive/actions/runs/41376895125), including installer/portable packaging. This follow-up documentation records that completed run; production-only SMTP/OAuth/live-backend/signing gates were not enabled.

# Native Connected verification and cleanup retry (2026-11-02)

- [x] Reset Computer Use after explicit user reauthorization and select the exact isolated test app from fresh inventory.
- [x] Verify the actual Connected screen and current account refresh without reading protected credentials.
- [x] Sign out the isolated desktop/browser test sessions and close the test app; verify credentials and callback listeners are gone.
- [x] Remove the specifically authorized temporary verification file; record private visual evidence and update the readiness record.

The user explicitly authorized this retry, native verification, test-app cleanup and removal of the temporary verification file without questions. Preserve unrelated apps, browser tabs, profile files and the Resend staging SMTP configuration.

## Connected retry review

- Reset Computer Use and raised the exact isolated app. Its live Account screen showed Connected, the expected owner test account, No subscription linked and the previous successful sync. Explicit Refresh succeeded without a browser and advanced the displayed sync to 20:06 Europe/Berlin on 2026-11-02. Private proof is `artifacts/smtp-native-validation-20261102/connected-verified.png`; no personal address or screenshot is committed.
- Native Sign out displayed Signed out and removed both primary/backup session files. A cleanup-only authorization exposed Use another account, which signed out the retained browser SSO session; its issuer session page displayed `null`. Cancel sign-in returned Signed out, Quit exited the app and both owned authorization tabs were closed.
- At 2026-11-02 19:08:49 UTC, final checks found zero isolated test processes, saved session files or callback listeners and confirmed the specifically authorized temporary verification file absent. No protected credential was decrypted, and no production/SMTP configuration was changed. The earlier cleanup rejection is resolved by this authorized retry.
- This is evidence/documentation work only; existing application/backend code and previously passing tests are unchanged. Verified-domain production email, trusted signing, full reset-form UI interaction and the other open Windows/database gates retain their existing status.

# Production sender and Windows signing setup (2026-11-02)

- [x] Recheck Resend and available DNS access; preserve working staging email. Owned domain name and DNS authorization remain unavailable, so registration/verification is pending.
- [x] Audit current signing certificates, runner credentials and existing cloud access without exporting keys or exposing secrets. Trusted publisher enrollment/key access remain unavailable.
- [x] Complete supported release configuration where available, verify the changes, and retain production gates for missing external inputs.
- [x] Record the exact domain/DNS and publisher/signing inputs still needed in plain language.

Assumptions: the existing account/API audience hostname is not evidence of domain ownership. Do not invent or purchase a domain, signing identity or service subscription. Prefer available provider access and prepare concrete release configuration before describing any remaining hand-off.

## Production setup review

- Resend still has no domains; the blank setup form defaults to Ireland and requires an owned name before generating verification records. Relevant DNS credential-presence checks and CLI/browser/tool discovery found no available authorization. No domain was guessed from the API audience, no DNS/mail routing was changed and no extra key/service purchase was made.
- Created GitHub environment `production` restricted to exactly branch `main`; added the known development API variable after live smoke acceptance. The separate manual main-only production job receives signing secrets only in its protected environment steps; ordinary validation jobs remain unsigned. Both final installer and ZIP-contained executable require the expected publisher, trusted embedded signature and timestamp. Checksums reject omissions, duplicates and stale bytes.
- Prepared version 1.1.0 across all four metadata fields after confirming the tag unused and the latest published release v1.0.1. No release/tag was created. The desktop solution builds with zero warnings/errors and all 107 tests pass. Eighteen production-gate and 21 signing/archive/checksum cases, real Windows signature acceptance/rejection, PowerShell parsing and workflow linting pass. Previous local packages remain unchanged.
- Actual production email, actual application signing and backend promotion remain gated. The exact operator inputs are an owned domain plus DNS authorization, and legal publisher type/name/country plus a trusted issued certificate or enrolled service with authorized signing access. Do not send passwords, keys or identity documents through chat; use the provider and GitHub environment secret fields.
- Redacted evidence is `tasks/verification/production-setup-2026-11-02.json`; publishing/setup docs match the production environment, expected-publisher input and separate production artifact.
