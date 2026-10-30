import { readFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { resolve } from 'node:path';
import { resolveTarget } from './targets.mjs';

export async function configureSmtp(environment, request) {
  const fields = ['SMTP_HOST', 'SMTP_PORT', 'SMTP_USERNAME', 'SMTP_PASSWORD', 'SMTP_SENDER_EMAIL'];
  const supplied = fields.some((name) => environment[name]);
  if (supplied) {
    if (!environment.SMTP_TEST_RECIPIENT) { throw new Error('SMTP_TEST_RECIPIENT is required before saving new SMTP settings. No configuration was changed.'); }
    const missing = fields.filter((name) => !environment[name]);
    if (missing.length) { throw new Error('Missing SMTP settings: ' + missing.join(', ')); }
    const port = Number(environment.SMTP_PORT);
    if (!Number.isInteger(port) || port < 1 || port > 65535) { throw new Error('SMTP_PORT must be a valid TCP port.'); }
    if (environment.SMTP_PASSWORD === '***redacted***') { throw new Error('A redacted read-back value cannot be used as an SMTP password.'); }
    await request('/email_provider', 'PATCH', {
      type: 'standard', host: environment.SMTP_HOST, port,
      username: environment.SMTP_USERNAME, password: environment.SMTP_PASSWORD,
      sender_email: environment.SMTP_SENDER_EMAIL, sender_name: environment.SMTP_SENDER_NAME || 'KeepMeAlive',
    });
  } else {
    const existing = await request('/email_provider');
    if (existing.type !== 'standard') { throw new Error('No application-owned SMTP provider is configured. Supply the SMTP settings through the environment.'); }
  }
  if (!environment.SMTP_TEST_RECIPIENT) { throw new Error('SMTP_TEST_RECIPIENT is required to test the saved provider. No test email was sent.'); }
  const test = await request('/email_provider/test', 'POST', { recipient_email: environment.SMTP_TEST_RECIPIENT });
  if (test.success !== true) { throw new Error('The saved SMTP provider failed its send test. Email verification settings were not changed.'); }
  await request('/email_and_password', 'PATCH', {
    enabled: true, email_verification_method: 'link', require_email_verification: true,
    send_verification_email_on_sign_up: true, send_verification_email_on_sign_in: true,
    auto_sign_in_after_verification: true, disable_sign_up: false,
  });
  return { smtpAcceptedTestMessage: true, inboxDeliveryVerified: false };
}

async function main() {
  const target = process.argv[2];
  const apiKey = process.env.NEON_API_KEY;
  if (!apiKey) { throw new Error('NEON_API_KEY is required. Do not save it in source control.'); }
  const config = JSON.parse(await readFile(new URL('../neon.json', import.meta.url), 'utf8'));
  const { branch, projectId } = resolveTarget(config, target);
  const root = `https://console.neon.tech/api/v2/projects/${projectId}/branches/${branch.branchId}/auth`;
  async function request(path, method = 'GET', body) {
    const response = await fetch(root + path, { method, headers: { Authorization: `Bearer ${apiKey}`, 'Content-Type': 'application/json' }, body: body ? JSON.stringify(body) : undefined, signal: AbortSignal.timeout(30000), redirect: 'error' });
    if (!response.ok) { throw new Error(`Neon SMTP configuration ${method} ${path} failed with HTTP ${response.status}.`); }
    const text = await response.text();
    return text ? JSON.parse(text) : {};
  }
  await configureSmtp(process.env, request);
  console.log(`Configured ${target} SMTP and required email verification. The SMTP send test was accepted; confirm inbox delivery separately. No readiness gate was changed.`);
}

if (process.argv[1] && fileURLToPath(import.meta.url) === resolve(process.argv[1])) { await main(); }
