# Production credentials and signing inputs

Checked 2026-10-30 against the setup scripts, release workflow and current vendor documentation. These inputs are still unavailable. Supply secrets through the provider Console, an approved secret manager, protected process environment or GitHub Actions secrets; do not paste them into chat, commit them, or include them in desktop packages.

## SMTP

The application needs an application-owned transactional SMTP account. Neon's shared `auth@mail.myneon.app` sender remains suitable only for development; verification links require custom SMTP. [Neon production checklist](https://neon.com/docs/auth/production-checklist)

| Required input | Script environment name | Value to provide |
| --- | --- | --- |
| SMTP server | `SMTP_HOST` | Provider's exact SMTP hostname; not the mail API URL. |
| SMTP port and transport | `SMTP_PORT` | Provider's supported encrypted submission port, normally 465 with implicit TLS or 587 with STARTTLS. Confirm the transport with the provider. |
| SMTP username | `SMTP_USERNAME` | Provider-issued SMTP login; it may differ from the account email. |
| SMTP password | `SMTP_PASSWORD` | Provider-issued SMTP password or supported app password; not an unrelated REST API token or Neon password. |
| Verified From address | `SMTP_SENDER_EMAIL` | Address/domain accepted and verified by that SMTP provider for this application. |
| Sender display name | `SMTP_SENDER_NAME` | `KeepMeAlive`, or the approved sender name. The script defaults to KeepMeAlive. |
| Acceptance mailbox | `SMTP_TEST_RECIPIENT` | A real mailbox available to check the test email, verification link and password-reset link. It must not be a reserved `.test` address. |

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
| Signing tool | Windows SDK `signtool.exe`, on PATH or under the SDK's x64 bin directory. |
| Timestamp/network access | The current script uses RFC 3161 `http://timestamp.digicert.com`, `/fd SHA256` and `/td SHA256`; allow the TSA and certificate-chain/revocation endpoints required by the issuer. |
| Acceptance | Sign both application executable and installer; validate the intended publisher, trusted Authenticode signature and timestamp on the final artifacts. |

These store-selection and timestamp switches follow [Microsoft SignTool documentation](https://learn.microsoft.com/en-us/windows/win32/seccrypto/signtool).

The current GitHub workflow supports a PFX import through repository secrets `WINDOWS_SIGNING_CERT_BASE64` (base64 PFX containing the key) and `WINDOWS_SIGNING_CERT_PASSWORD`. It imports into the runner's CurrentUser My store and sets the thumbprint. **This route is usable only if the certificate's private-key policy permits that PFX.** Modern token/HSM/cloud signing keys commonly cannot be exported; do not try to extract them. DigiCert explicitly states that token, HSM and KeyLocker certificates cannot be exported to PFX. [DigiCert PFX restrictions](https://knowledge.digicert.com/general-information/export-a-code-signing-certificate-as-a-pfx-file)

For a nonexportable key, provide the selected signing provider and runner integration instead of a PFX: provider/account or certificate-profile identifiers, intended publisher, authorized signing identity and scoped signing access, and any required KSP/client or self-hosted runner. The workflow must be adapted and verified for that provider. Microsoft Artifact Signing, for example, keeps its private keys in managed HSMs and does not export them; its service integration cannot be substituted with the current PFX secrets. [Microsoft Artifact Signing key management](https://learn.microsoft.com/en-us/azure/artifact-signing/concept-certificate-management)

## Release activation

Credentials alone do not mark the release ready. After real SMTP delivery, native/account acceptance, production backend smoke testing and signing pass, record the acceptance variables `KEEP_ME_ALIVE_SMTP_VERIFIED=true` and `KEEP_ME_ALIVE_OAUTH_VERIFIED=true`, the tested `KEEP_ME_ALIVE_DEV_API_URL`, and the reviewed `accountReleaseReady`/matching active production settings. Run `scripts/Build-Packages.ps1 -RequireProduction`. The remaining operational checks and their evidence are tracked in `RELEASE_READINESS.md`.
