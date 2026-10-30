# KeepMeAlive account backend

The local Windows utility works without an account. This directory contains the optional authenticated API and hosted sign-in pages. It uses Neon Managed Better Auth, PostgreSQL, and a Hono Neon Function on Node.js 24.

## Provisioned environment

`neon.json` contains public project, branch, issuer, resource, and Function identifiers. The dedicated project is `morning-bread-41310427` in Frankfurt (`aws-eu-central-1`). Development and production have isolated databases and issuers. The unrelated existing Neon project was not modified.

Development is deployed at [the development API](https://br-fancy-star-b98qz19k-keepmealive.compute.c-7.eu-central-1.aws.neon.tech/readyz). Production has its schema and Managed Auth provisioned; the Function is intentionally not promoted while live account release gates remain open. Desktop production configuration has an empty client ID and displays the account feature as unavailable until setup is complete.

OAuth acceptance uses the independent Frankfurt staging root project `sweet-truth-01118709`, branch `br-red-flower-b9wmbkmb`, and [staging API](https://br-red-flower-b9wmbkmb-keepmealive.compute.c-7.eu-central-1.aws.neon.tech/readyz). It has separate Auth users, schema/RLS, restricted runtime credentials, and Function deployment 1. The existing development child did not inherit OAuth when production's root provider was enabled; its original database and API remain available. Setup/deploy scripts resolve `staging.projectId` separately from the production project.

Neon reserves `/healthz` for its runtime. `/readyz` returns this application's JSON readiness identity; it does not assert SMTP or database readiness. `/v1/account` and `/v1/profile` require a verified bearer token.

## Local checks

```powershell
npm ci
npm run check
npm test
npm audit --audit-level=high
npm run build
```

The ZIP at `dist/function.zip` is a generated deployment artifact. Keep database URLs, Neon API keys, passwords and SMTP credentials outside source control and desktop packages.

## Database and authorization

`migrations/001_accounts.sql` creates versioned profiles and entitlements. Profiles belong to the authenticated UUID. Entitlements are written only by administrative workflows; the runtime cannot grant them. All queries use parameters and run inside transactions with a local `app.user_id` setting. RLS restricts profile and entitlement rows to that subject.

The deployed API connects using the SQL-created `keepmealive_api` login. It has no superuser membership, RLS bypass, database creation or role creation privileges. It can select/update profiles and read entitlements and the three necessary auth profile columns. `KEEP_ME_ALIVE_DATABASE_URL` is a server environment variable using that role; automatic owner `DATABASE_URL` is deliberately unused.

For a new environment, create the login with `CREATE ROLE keepmealive_api LOGIN NOBYPASSRLS NOCREATEDB NOCREATEROLE`, set its password securely with your database client, then run `node scripts/migrate.mjs` with an owner `MIGRATION_DATABASE_URL`. Do not use Neon's role-creation API as a replacement: that API creates privileged `neon_superuser` members. Initial provisioning discovered this behavior; unused `keepmealive_runtime` API-created roles remain on both branches and must not be used by the application. Their removal requires an operator through the Neon Console/API because the SQL owner cannot alter them.

## Complete OAuth configuration

The connected plugin supports Auth provisioning, Function deployment, and managed OAuth Provider/public-client administration. Console fallback initially configured both root providers and public clients; current plugin read-back verifies their configuration. SMTP and email-verification policy setters remain unavailable through the plugin. Production client `IzVVcdXJJGYyOblRvHhTUqUuhTdRsGSL` and staging client `rRwBPkonGujPtDihzCtDKZJronKFaVtN` require consent and use exactly the callbacks and resource below. These are public identifiers, with no client secret.

For repeatable configuration, run `node scripts/configure-auth.mjs staging` with an authorized `NEON_API_KEY`, or use the matching project/branch in Console. The script reuses the public client, repairs its redirect/resource/consent settings, and preserves other resource identifiers. It writes only public settings. Add `--activate` to explicitly copy that target's public settings to the desktop's active `neon.json`; this does not mark the release ready. Production desktop sign-in remains disabled until SMTP and acceptance pass.

The current [provider management API](https://neon.com/docs/reference/api/auth/update-neon-auth-o-auth-provider-plugin) permits enabling OAuth Provider only on a project's root branch. The script checks fresh branch metadata and rejects a disabled child before making changes. Production and isolated staging return valid discovery metadata; the preserved development child still returns 404. Never mix a production issuer with a development/staging API.

For each supported root target:

1. Add its Function origin as an Auth trusted domain (production, development, and staging origins are already configured).
2. Enable OAuth Provider with the Function's `/login` and `/consent` pages and resource `https://keepmealive.app/api`. This URL is an audience identifier and does not need to resolve.
3. Register a **public** client named KeepMeAlive desktop with no secret and consent enabled. Register exactly `http://127.0.0.1:49152/callback/`, `http://127.0.0.1:49153/callback/`, and `http://127.0.0.1:49154/callback/`. Enable localhost access for this native loopback use case. Do not enable dynamic client registration or skip consent.
4. Configure application-owned SMTP, require verified email, and verify registration, verification and password-reset delivery. `node scripts/configure-smtp.mjs staging` accepts `SMTP_HOST`, `SMTP_PORT`, `SMTP_USERNAME`, `SMTP_PASSWORD`, `SMTP_SENDER_EMAIL`, optional `SMTP_SENDER_NAME`, and required `SMTP_TEST_RECIPIENT` through the environment. It uses a previously saved custom provider when fresh credentials are omitted, tests stored credentials without re-supplying their redaction, then requires verification links. SMTP acceptance is recorded only in command output; actual inbox delivery still needs verification. No application-owned SMTP credentials are currently available. Shared SMTP and shared Google credentials are development-only; no social sign-in is exposed by this app.
5. Use `KeepMeAlive/neon.staging.json` for a controlled staging desktop test. Only activate the matching production public client/endpoints in `KeepMeAlive/neon.json` after promotion and acceptance. `KEEP_ME_ALIVE_OAUTH_CLIENT_ID` overrides a public client ID but cannot change its issuer or API; overriding it alone is insufficient for switching environments.

The native client uses state and SHA-256 PKCE, exact issuer discovery, and one of the prebound loopback ports. It sends `resource` on authorization and token requests. The API verifies EdDSA signature, exact issuer including `/neondb/auth`, resource audience, subject, and expiry. This is necessary because Neon branches share signing keys. Refresh rotation is serialized; refresh credentials are protected locally and access tokens remain in memory. Rotated refresh credentials are saved atomically before checking API availability so an outage cannot lose the newly rotated credential. Access tokens are accepted into memory only after authenticated API validation; an authorization rejection clears the saved session.

Hosted pages preserve the signed OAuth query and use credentialed requests to Managed Auth. Registration, password reset, email verification, login and consent are implemented. Until SMTP/client settings and actual browser flows are verified, these forms are **not** a claim of a working production account release.

Current documentation says the OAuth Provider is available to Managed Better Auth projects; its management API and the `@neon/oauth` SDK are marked beta. This implementation uses the standard HTTP protocol and `jose`, without that beta SDK. See [OAuth Provider](https://neon.com/docs/auth/guides/plugins/oauth-provider) and [production checklist](https://neon.com/docs/auth/production-checklist).

## Deploy and promote

Set `NEON_API_KEY` and the branch-specific restricted `KEEP_ME_ALIVE_DATABASE_URL` in a secure deployment environment, build the ZIP, then run `node scripts/deploy.mjs development`. The script uploads through the Neon Management API without printing credentials. The connected plugin can deploy the same ZIP. Keep the previous Function ZIP/deployment ID and desktop package for rollback.

The production script refuses deployment until `accountReleaseReady` is explicitly recorded as true in `neon.json` after all checks below pass. Apply only forward-compatible migrations; do not reset a live branch to undo application code. For data recovery, use Neon retained history/snapshots and restore into a new branch, verify it, and coordinate cutover.

## Release gates and verified evidence

Verified: TypeScript strict check, 24 backend/setup/page/helper tests including two-user ownership, wrong-issuer/audience/expired token rejection, staging project routing, root-branch preflight, SMTP safeguards, signed-query preservation, account switching/error recovery, redirect validation, synthetic-fixture cleanup, and opaque staging-test tunnel controls; zero npm vulnerabilities; development/staging readiness and hosted pages; anonymous API denial; restricted runtime grants; and native OAuth unit tests. Production and staging providers/public clients and trusted Function origins are configured.

The isolated staging service passed 40 live HTTP protocol checks: discovery, exact issuer/endpoints, consent metadata, S256 authorization-code exchange, callback state, persisted profile edits, two real synthetic accounts and forged ownership rejection, refresh rotation, revocation, sign-out, and code reuse rejection. Safe evidence is in `dist/staging-oauth-acceptance.json`. These requests used reserved `.test` emails and did not request email delivery. Synthetic test users are retained; final test sessions were signed out and refresh credentials revoked. This protocol evidence does not establish browser cookie behavior, actual SMTP delivery, or native desktop session storage.

To repeat deliberately, first read back that staging sign-up/sign-in emails and required verification are disabled, then run `node scripts/test-oauth.mjs --live-staging --emails-disabled-confirmed`. It creates two additional retained synthetic staging users, pins the isolated staging root, and refuses other targets. Once email verification is enabled, use real email/browser acceptance instead. A retained snapshot of the passing protocol evidence is in `../tasks/verification/staging-oauth-2026-10-29.json`.

A real Brave browser also completed hosted sign-in, credentialed cross-origin cookies, verified client consent, the exact loopback callback, state validation, S256 exchange, and authenticated staging API access. The refresh credential was revoked and the listener exited. Safe evidence is retained in `../tasks/verification/staging-browser-2026-10-29.json`. The controlled `test-browser-oauth.mjs` fixture accepts strong synthetic reserved-domain credentials through the environment, pins staging, and requires explicit no-email confirmation flags. Neither this fixture nor the HTTP checks establish native WPF protected-session storage or actual SMTP delivery.

The consent page provides **Use another account**, calling the supported credentialed `/sign-out` endpoint before resuming the original signed login request. Brave verification confirmed an empty login form and `/get-session` returning `null`. Interactive desktop authorization uses `prompt=consent` to keep this choice reachable after a remembered grant. This also supplies the supported sign-out route while managed public clients cannot enable the [upstream RP end-session permission](https://better-auth.com/docs/plugins/oauth-provider#end-session-endpoint); an ID-token hint alone cannot override that missing permission.

Actual Windows acceptance now covers native browser sign-in, account identity, profile saving, DPAPI-protected refresh storage, explicit restoration after Quit/restart, logout deleting both session copies, account switching to an empty hosted login form, native cancellation, and signed-out utility Start/Stop. The normal profile and startup registration remained unchanged; test sessions were signed out, listeners stopped, and test tabs closed. Evidence and screenshots are in `../tasks/verification/windows-neon-2026-10-29.json`.

Native account service-outage/recovery passed on 2026-10-30. A pinned staging-only fixture made authenticated repository operations return redacted HTTP 503 responses while leaving authentication and hosted pages intact. The actual Windows app retained its cached identity and sync time, and local Start/Stop remained available. The watchdog restored the exact original bundle as staging deployment 4 after the temporary deployment 3; native Refresh returned Connected without a browser. This rehearses backend artifact rollback and service-outage recovery, not physical network disconnection or database recovery. Evidence: `../tasks/verification/windows-outage-2026-10-30.json`. The guarded builder is `scripts/build-native-outage.mjs --staging-outage-validation`; it never changes the production source or normal bundle.

Still required: custom SMTP with real verification/reset delivery; trusted release signing and remaining environment-specific Windows/manual checks; database recovery rehearsal; production email settings and controlled production smoke after promotion. Exact SMTP and signing inputs are in `../KeepMeAlive/docs/PRODUCTION_INPUTS.md`. An earlier automatic approval review rejected the process-local test-proxy launch as "blocked by policy"; no equivalent workaround launch was used. Per-user API limits are bounded in each Function isolate; public launch should additionally use a deployment-owned edge rate limit/WAF where available.

The hosted-page review was deployed to development as deployment **3** with the existing environment and runtime inherited. The local ZIP SHA-256 is `AA618FBCB62A49628706705D997730F0839D9B09A677A5D9CC1F851F61E2D25F`. Deployment **2** and its ZIP are preserved locally at `dist/deployment-before-auth-review.json` and `dist/function-before-auth-review.zip` (SHA-256 `3F127EAA3EBCB5239B9A2FBA2FBF2717A657740C59E3F920083134F7B04EC7DC`). Both deployed login and consent scripts were fetched and executed in an isolated DOM harness to verify bundling left no compiler-helper dependencies; this is a script smoke check, not a live authenticated browser flow.

Do not mark the account release ready or promote solely because `/readyz` returns 200.

The account-switch fix is deployed as development **4** and staging **2**, inheriting existing server environment/runtime. Its bundle SHA-256 is `5315E56F81A34D47610F0C12B2B71B6FB30F543C02975DF5E52D069E1AD93003`. Production remains undeployed. The earlier bundle and deployment records remain available for rollback.
