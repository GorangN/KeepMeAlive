# Publishing KeepMeAlive, step by step

Updated against this repository and current provider guidance on 2026-11-02.

Progress on 2026-11-02: Resend test SMTP and required verification links are configured in isolated staging. Real test, verification and reset emails reached the owner's mailbox; verification and the API password-reset/sign-in checks passed. After resetting Computer Use, the native Account screen showed Connected, the expected staging account and No subscription linked. Refresh succeeded without reopening the browser, and Sign out removed both protected session files. Browser sign-out was confirmed by an empty login form and a null issuer session; the temporary verification file was deleted. Complete browser reset-form interaction remains unverified. Resend has no verified domain yet, so public production sending is still blocked. The steps below still apply to production; do not treat the restricted test sender as production acceptance.

Pushing `main` saves the source code on GitHub. Publishing a release makes a downloadable installer available to users. The current workflow builds validation packages; it does **not** publish a release automatically. Production accounts remain disabled until the checks below pass. The core keep-alive utility works without an account.

**SMTP means the email delivery connection.** Neon uses it to send “verify your email” and “reset your password” messages. You can use an email service instead of running your own mail server. The missing input is an application-owned sender account, not your personal Gmail password or your Neon database password.

**The missing inputs are your owned domain name and authorized DNS-provider access, plus the signing publisher's type (individual/organization), legal name, country and trusted signing access.** The SMTP host, port, username and staging inbox are already known. Once domain access exists, the setup can generate the exact DNS records and separate production sending key. Put private identity documents directly into the signing provider, and credentials directly into the provider/secret store or GitHub production environment. No key needs to be pasted into chat.

## 1. Set up an email sender

One supported example is [Resend](https://resend.com/docs/send-with-smtp). Another provider is fine if it supports Neon's SMTP requirements.

The connected Resend account currently has no domains. Once you supply an owned domain and authorized DNS access, add a transactional subdomain such as `notify.<owned-domain>` in **Domains**. Neither the API audience `keepmealive.app` nor `resend.dev` proves domain ownership. Copy Resend's generated DKIM, SPF and return-path records into the DNS provider, preserve existing root-mail MX records, and follow applicable DMARC instructions. Disable open and click tracking for authentication email, then confirm the saved settings and successful domain verification. [Resend domain instructions](https://resend.com/docs/dashboard/domains/manage-domains), [tracking settings](https://resend.com/docs/dashboard/domains/tracking)

Create a **Sending access** key restricted to that verified domain and choose a From address on it. Use separate staging and production keys; preserve the current staging key until a replacement passes delivery tests. With Resend, the already known SMTP values are:

| Neon field | Value |
| --- | --- |
| Host | `smtp.resend.com` |
| Port | `465` (encrypted SMTP) |
| Username | `resend` |
| Password | Your Resend API key; Resend explicitly supports this as its SMTP password. |
| Sender email | Your actual verified address, for example `noreply@your-domain.tld`. |
| Sender name | `KeepMeAlive` |

Keep the key in the provider Console or an approved secret manager. Do not paste it into chat, source files or desktop packages. You also need a real inbox where you can open the acceptance emails.

## 2. Connect the sender to Neon staging

In [Neon Console](https://console.neon.tech), select staging project **`sweet-truth-01118709`**, branch **`br-red-flower-b9wmbkmb`**. Open **Settings → Better Auth → Custom SMTP provider**, enter the fields above, and **Save**. Use a send test if offered. [Neon's email configuration](https://neon.com/docs/auth/production-checklist)

Enable required email verification using links, including sending verification on registration. The repository's `configure-smtp.mjs` script can apply that policy after a successful send test if the Console does not expose every setting; the technical inputs are in [PRODUCTION_INPUTS.md](PRODUCTION_INPUTS.md). Existing authorized Console access does not require creating a new Neon API key.

Use a controlled staging Windows build to register with a real mailbox. Open the verification email, finish sign-in in the browser, and confirm the Windows app shows Connected. Then sign out, request a password reset, open its email, change the password, and sign in again. A successful SMTP test alone does not prove either email reached the inbox or its link worked.

Preserve the application's three exact registered callbacks on ports **49152, 49153 and 49154**, each using `http://127.0.0.1:<port>/callback/`. This desktop flow relies on loopback callbacks. Changes to Neon's generic localhost settings need a fresh native sign-in test; do not replace callbacks with wildcards.

## 3. Configure production email

Repeat the SMTP configuration and required verification policy for production project **`morning-bread-41310427`**, branch **`br-fragrant-voice-b9m46w7y`**. Keep staging and production configuration separate. Complete real production verification/reset acceptance in a controlled validation build during the promotion in step 6. Do not enable public account distribution just because SMTP settings saved successfully.

## 4. Arrange Windows signing

Signing lets Windows identify the application's publisher. Provide a publicly trusted code-signing service or certificate for the intended KeepMeAlive publisher, and authorized access to its signing key on the build runner. A certificate file without access to its private key cannot sign.

The current production job imports a permitted PFX from the GitHub **production environment** secrets **`WINDOWS_SIGNING_CERT_BASE64`** and **`WINDOWS_SIGNING_CERT_PASSWORD`**. It also requires environment variable **`WINDOWS_SIGNING_PUBLISHER`**, containing the intended certificate's exact full Subject. It verifies the final installer's and portable executable's trusted signatures, publisher and timestamps.

Many modern signing keys stay in hardware or a cloud service and cannot be exported. For that route, provide the selected service, enrolled legal publisher and scoped runner access; the workflow needs a verified provider integration before release. Check eligibility for your actual publisher type/country before enrolling. The detailed inputs and Microsoft eligibility limits are in [PRODUCTION_INPUTS.md](PRODUCTION_INPUTS.md).

Do not reuse the unrelated local self-signed EasyBluetoothAudio certificate. Exact supported inputs and acceptance checks are in [PRODUCTION_INPUTS.md](PRODUCTION_INPUTS.md).

## 5. Finish the remaining acceptance checks

Record the results in [RELEASE_READINESS.md](RELEASE_READINESS.md): actual Windows 200% scaling; installation and uninstall under a fresh standard user; spoken screen-reader output; native tray-menu clicks; and a database recovery rehearsal. The current machine offered only up to 175% scaling. Native account outage/recovery and application/backend artifact rollback have already been exercised; database recovery is a separate remaining check.

## 6. Promote the backend and activate accounts

This is deployment work for the maintainer or Codex after the inputs above exist:

1. Recheck staging authentication, email delivery, account isolation and rollback evidence. Keep `accountReleaseReady` in `backend/neon.json` false and the shipped desktop client inactive during controlled production acceptance.
2. Deploy the verified backend artifact to the existing production target using the connected Neon service for that controlled acceptance. The script route rejects production while readiness is false; it is for later ready-state deployments and requires protected deployment credentials plus the restricted `keepmealive_api` database login. These credentials belong on the server/deployment environment.
3. Verify production health and real account access, verification/reset mail, refresh, logout, isolation and native outage recovery in a controlled Windows copy. Record successful acceptance before setting `accountReleaseReady` to true.
4. Activate the registered production public client in the shipped `KeepMeAlive/neon.json` only after acceptance. The existing `configure-auth.mjs production --activate` route performs activation; it does not mark the service ready or prove acceptance. Commit the reviewed public configuration. Never ship a staging configuration or database credentials.

The technical deployment and recovery procedure is in [RELEASE.md](RELEASE.md). Readiness flags record completed checks; changing them does not perform the checks.

## 7. Prepare a new version and GitHub settings

The latest public release checked on 2026-11-02 is [v1.0.1](https://github.com/GorangN/KeepMeAlive/releases/tag/v1.0.1). The project is prepared for `1.1.0`: **Version**, **AssemblyVersion**, **FileVersion** and **InformationalVersion** are `1.1.0`, `1.1.0.0`, `1.1.0.0` and `1.1.0`. No `v1.1.0` tag or release has been created. Recheck that the tag is unused before publishing. For a later version, update all four properties together before building.

On GitHub, **Settings → Environments → production** is already restricted to `main`. Add the two signing **Environment secrets** and the publisher **Environment variable** from step 4 there. Normal validation jobs cannot access these signing secrets. Record these acceptance variables in that environment only after their checks pass:

| Variable | Value |
| --- | --- |
| `KEEP_ME_ALIVE_SMTP_VERIFIED` | `true` after verified-domain production verification/reset delivery and link tests; the restricted staging test sender does not qualify. |
| `KEEP_ME_ALIVE_OAUTH_VERIFIED` | `true` after account/native acceptance. |

The repository variable **`KEEP_ME_ALIVE_DEV_API_URL`** is already set under **Settings → Secrets and variables → Actions → Variables** to the development URL from `backend/neon.json`, following a successful live smoke test. That public URL is separate from the production environment's private signing credentials.

## 8. Build the production packages

Open the repository's **Actions → Verify and package → Run workflow**. Choose `main`, enable **production**, and run it. Wait for the backend and desktop validation jobs and then the separate production job to succeed. Production signing runs only for this manual request on `main`, using the `production` environment. A failed gate must be resolved before publishing.

Download and extract **KeepMeAlive-production** from the successful run. It contains the signed installer, portable ZIP, checksums and dependency inventory. **KeepMeAlive-validation** is the ordinary validation artifact, which is also produced by the earlier desktop job. Confirm the final signed production packages work on the clean test machine and correspond to the commit shown by the successful run. Keep the previous release artifacts for rollback. The workflow still does not publish a GitHub release.

## 9. Publish the downloads

Open [KeepMeAlive Releases](https://github.com/GorangN/KeepMeAlive/releases), then **Draft a new release**. Choose the new version tag, for example `v1.1.0`, pointing to the exact commit tested in step 8. If `main` advanced meanwhile, tag the tested commit explicitly instead of releasing the newer untested code.

Add release notes describing the shipped behavior. Attach these four files from the same successful run: **KeepMeAlive-Setup.exe**, **KeepMeAlive-Portable.zip**, **SHA256SUMS.txt**, and **dependencies.json**. GitHub's automatically generated source archives are source code, not the runnable application. Save the draft, review its files/version, then **Publish release**. [GitHub's release instructions](https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository)

A clearly labeled unsigned **pre-release** of the offline utility can be published earlier with accounts disabled and the remaining Windows checks disclosed. Use ordinary validation packages for that path, leave production acceptance flags unset, and do not distribute the staging sign-in ZIP as a public production package.
