# Production credentials and signing inputs

Updated 2026-11-02. Resend test SMTP is configured in isolated staging, and actual SMTP-test, verification and reset messages reached the account owner's connected mailbox. The verification link worked; the reset link, password change and new-password sign-in passed through the staging API. After resetting Computer Use, the native Account screen showed Connected, the expected staging account and No subscription linked. Explicit Refresh advanced the sync time without reopening the browser or decrypting credentials for inspection. Desktop Sign out showed Signed out and removed the primary and backup session files. Browser sign-out reached an empty login form and the issuer's session endpoint returned null; the temporary verification file was deleted. Complete browser reset-form interaction remains unverified.

Production still needs a verified owned sender domain, a production-scoped sending key, final acceptance and trusted signing. Supply secrets through the provider Console, an approved secret manager, protected process environment or GitHub Actions secrets; do not paste them into chat, commit them, or include them in desktop packages.

## What is still needed from you

The Resend server (`smtp.resend.com`), port (`465`), username (`resend`) and existing staging acceptance inbox are already known. You do not need to invent those values, provide your personal email password, or paste a sending key into chat.

| Missing input | What to provide and what it unlocks |
| --- | --- |
| Owned domain and DNS access | The exact domain you own, plus an authorized DNS-provider session or appropriately scoped DNS access through a secret store. An owned domain has not been identified, and authorized DNS access is unavailable. Once available, the setup can generate the exact Resend DNS records and a separate production sending key. |
| Publisher identity and signing access | Whether the publisher is an individual or organization, its exact legal name and country, plus an issued publicly trusted signing certificate with authorized key access or an enrolled signing service with scoped runner access. Supply identity documents directly to the signing provider. Put permitted PFX material/passwords directly into the GitHub `production` environment secrets. |

The remaining browser reset-form, Windows and database-recovery checks are acceptance work, not additional SMTP credentials. Neither the API audience `https://keepmealive.app/api` nor Resend's test sender proves ownership of a domain.

## SMTP

Production needs an application-owned transactional sender. Resend currently has no verified domain. Staging uses a sending-only key stored in Neon's managed SMTP settings with `smtp.resend.com`, port `465`, username `resend`, and the test From address `onboarding@resend.dev`. That address can send only to the exact Resend account owner's email; it is not a public production sender. No production email configuration was changed. [Resend test-domain restriction](https://resend.com/docs/knowledge-base/403-error-resend-dev-domain)

Use a dedicated transactional subdomain such as `notify.<owned-domain>` once the owned domain is known. The inspected empty Add domain form offered Ireland (`eu-west-1`) and return-path label `send`; no domain was created. Add only the exact DKIM, SPF and return-path DNS records Resend generates for that domain, with applicable DMARC alignment. Preserve existing root-domain mailbox MX records. Disable both open and click tracking for authentication email and verify the saved settings after creation. Preserve the working staging key until its replacement has passed delivery tests. [Resend subdomains](https://resend.com/docs/dashboard/domains/introduction), [domain management](https://resend.com/docs/dashboard/domains/manage-domains), [tracking controls](https://resend.com/docs/dashboard/domains/tracking)

Neon's shared `auth@mail.myneon.app` sender remains suitable only for development; verification links require custom SMTP. [Neon production checklist](https://neon.com/docs/auth/production-checklist)

| Configuration field | Script environment name | Known value or next setup action |
| --- | --- | --- |
| SMTP server | `SMTP_HOST` | Already known for Resend: `smtp.resend.com`. |
| SMTP port and transport | `SMTP_PORT` | Already known for Resend: `465`, implicit TLS. |
| SMTP username | `SMTP_USERNAME` | Already known for Resend: `resend`. |
| SMTP password | `SMTP_PASSWORD` | Generate a separate production Sending access key scoped to the verified domain. Resend uses that key as its SMTP password; it is not a personal mailbox or Neon password. |
| Verified From address | `SMTP_SENDER_EMAIL` | Select an address on the verified sending subdomain after domain setup. |
| Sender display name | `SMTP_SENDER_NAME` | `KeepMeAlive`, or the approved sender name. The script defaults to KeepMeAlive. |
| Acceptance mailbox | `SMTP_TEST_RECIPIENT` | The existing real staging acceptance inbox is available; no new mailbox password is needed. Check production verification/reset delivery separately. |

The current script/API body has no separate TLS/SSL switch or `SMTP_TLS` variable. The provider's port/transport must work with Neon's managed SMTP service; the send test must pass. Do not invent an extra environment setting. Configure the sender domain using the chosen provider's actual domain-verification, SPF and DKIM records, plus its applicable DMARC/alignment requirements. The exact DNS names and values come from that provider; none can be supplied before a provider/domain is chosen and authorized.

For Console setup, use the intended project's Auth/Better Auth email-provider settings with existing authorized access. No new Neon Management API key is necessary for that route. The currently connected plugin does not expose SMTP or verification-policy setters.

For the existing script route, also supply `NEON_API_KEY` with permission to configure Auth on the intended project/branch, then run `node backend/scripts/configure-smtp.mjs staging` first. The script uses Neon's branch-scoped `PATCH /projects/{project_id}/branches/{branch_id}/auth/email_provider` and stored-provider test endpoint. After the SMTP server accepts the test, it enables required link verification. Repeat against `production` only after staging email acceptance. [Neon email-provider API](https://neon.com/docs/reference/api/auth/update-neon-auth-email-provider)

The production target is project `morning-bread-41310427`, branch `br-fragrant-voice-b9m46w7y`; the separate staging target is project `sweet-truth-01118709`, branch `br-red-flower-b9wmbkmb`. `backend/neon.json` resolves them. A successful SMTP send response does not prove inbox receipt: both actual verification and password-reset messages and their links must be checked before recording `KEEP_ME_ALIVE_SMTP_VERIFIED=true`.

## Windows signing

Provide the intended publisher's publicly trusted Windows code-signing certificate and authorized access to its private key on the signing runner. The publisher identity must be appropriate for KeepMeAlive. The certificate must be active, unexpired and unrevoked, with a trusted chain, code-signing EKU `1.3.6.1.5.5.7.3.3`, and digital-signature key usage. A public certificate file alone cannot sign. The unrelated local self-signed EasyBluetoothAudio certificate must not be reused. [Microsoft certificate criteria](https://learn.microsoft.com/en-us/windows/win32/appxpkg/how-to-sign-a-package-using-signtool)

The current local packaging script expects:

| Input or capability | Exact current integration |
| --- | --- |
| Certificate/key available to the signing Windows user | Certificate in `Cert:\CurrentUser\My` with an accessible private key; hardware/KSP-backed keys need the vendor driver/provider and authorized key access. |
| Selected certificate | Environment variable `WINDOWS_SIGNING_CERT_THUMBPRINT`. |
| Expected publisher | Environment variable `WINDOWS_SIGNING_PUBLISHER`, copied exactly from the intended certificate's full `Subject`. Matching is case-sensitive and includes its complete formatting. |
| Signing tool | Windows SDK `signtool.exe`, on PATH or under the SDK's x64 bin directory. |
| Timestamp/network access | The current script uses RFC 3161 `http://timestamp.digicert.com`, `/fd SHA256` and `/td SHA256`; allow the TSA and certificate-chain/revocation endpoints required by the issuer. |
| Acceptance | Validate trusted embedded Authenticode, the exact expected publisher and timestamp for the installer and the application executable extracted from the final portable ZIP. |

These store-selection and timestamp switches follow [Microsoft SignTool documentation](https://learn.microsoft.com/en-us/windows/win32/seccrypto/signtool).

The GitHub `production` environment exists and permits only `main`. In **Settings → Environments → production**, supply environment secrets `WINDOWS_SIGNING_CERT_BASE64` (base64 PFX containing the authorized key) and `WINDOWS_SIGNING_CERT_PASSWORD`, plus environment variable `WINDOWS_SIGNING_PUBLISHER` with the exact certificate `Subject`. These signing inputs are still absent. Only the manually requested production job accesses them; ordinary validation jobs have no signing secrets. The production job imports into CurrentUser My, sets the thumbprint and removes imported material afterward.

**This PFX route is usable only if the certificate's private-key policy permits it.** Modern token/HSM/cloud signing keys commonly cannot be exported; do not try to extract them. DigiCert explicitly states that token, HSM and KeyLocker certificates cannot be exported to PFX. [DigiCert PFX restrictions](https://knowledge.digicert.com/general-information/export-a-code-signing-certificate-as-a-pfx-file)

For a nonexportable key, provide the selected signing provider and runner integration instead of a PFX: provider/account or certificate-profile identifiers, intended publisher, authorized signing identity and scoped signing access, and any required KSP/client or self-hosted runner. The workflow must be adapted and verified for that provider. Microsoft Artifact Signing, for example, keeps its private keys in managed HSMs and does not export them; its service integration cannot be substituted with the current PFX secrets. [Microsoft Artifact Signing key management](https://learn.microsoft.com/en-us/azure/artifact-signing/concept-certificate-management)

Provider eligibility depends on the publisher's legal status and location. Microsoft Artifact Signing Public Trust supports eligible EU organizations, but individual developers must currently be in the USA or Canada. Do not infer eligibility from the computer's timezone or substitute Private Trust for public distribution. No managed signing provider has been enrolled or integrated. [Microsoft eligibility requirements](https://learn.microsoft.com/en-us/azure/artifact-signing/quickstart)

## Release activation

The repository variable `KEEP_ME_ALIVE_DEV_API_URL` is set after a successful live development smoke test. Credentials and this smoke check do not mark production ready. Keep the production SMTP/OAuth acceptance flags unset until verified-domain production email, native/account acceptance, production backend checks and the other gates pass. Then record `KEEP_ME_ALIVE_SMTP_VERIFIED=true` and `KEEP_ME_ALIVE_OAUTH_VERIFIED=true` in the `production` environment, and review `accountReleaseReady` plus the matching active production configuration.

Use **Actions → Verify and package → Run workflow**, select `main` and enable `production`. The separate production job requires the environment inputs, runs after backend/desktop validation, and produces `KeepMeAlive-production`; ordinary builds produce `KeepMeAlive-validation`. Neither publishes a release. Local production packaging uses `scripts/Build-Packages.ps1 -RequireProduction`. Remaining acceptance and evidence are tracked in `RELEASE_READINESS.md`.
