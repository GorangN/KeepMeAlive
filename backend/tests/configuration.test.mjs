import assert from 'node:assert/strict';
import { test } from 'node:test';
import { configureAuth } from '../scripts/configure-auth.mjs';
import { configureSmtp } from '../scripts/configure-smtp.mjs';
import { resolveTarget } from '../scripts/targets.mjs';

test('staging resolves its own root project while existing branches retain the original project', () => {
  const config = { projectId: 'production-project', development: { branchId: 'br-child' }, production: { branchId: 'br-root' }, staging: { projectId: 'staging-project', branchId: 'br-staging' } };
  assert.equal(resolveTarget(config, 'staging').projectId, 'staging-project');
  assert.equal(resolveTarget(config, 'development').projectId, 'production-project');
  assert.equal(resolveTarget(config, 'production').projectId, 'production-project');
  assert.throws(() => resolveTarget(config, 'unknown'), /Choose development, staging or production/);
});

test('OAuth configuration preserves existing resources and repairs an existing public client', async () => {
  const config = { resource: 'https://keepmealive.app/api', accountReleaseReady: false, development: { branchId: 'br-test', apiBaseUrl: 'https://api.example.test/', issuer: 'https://auth.example.test/neondb/auth' } };
  const calls = [];
  const settings = await configureAuth(config, 'development', async (path, method = 'GET', body) => {
    calls.push({ path, method, body });
    if (path === '/plugins/oauth-provider' && method === 'GET') { return { enabled: true, resources: [{ identifier: 'https://other.example.test/api' }] }; }
    if (path === '/domains' && method === 'GET') { return { domains: [] }; }
    if (path === '/oauth-provider/clients' && method === 'GET') { return { clients: [{ client_id: 'existing', name: 'KeepMeAlive desktop', type: 'public', disabled: true }] }; }
    return path.endsWith('/existing') ? { client_id: 'existing' } : {};
  }, { id: 'br-test', parent_id: 'br-root' });
  assert.equal(settings.clientId, 'existing');
  assert.equal(config.accountReleaseReady, false);
  assert.ok(calls.some((call) => call.path === '/allow_localhost' && call.body.allow_localhost === true));
  const patch = calls.find((call) => call.path === '/oauth-provider/clients/existing');
  assert.equal(patch.body.skip_consent, false);
  assert.equal(patch.body.disabled, false);
  assert.deepEqual(patch.body.redirect_uris, [49152, 49153, 49154].map((port) => `http://127.0.0.1:${port}/callback/`));
  assert.deepEqual(calls.find((call) => call.path === '/plugins/oauth-provider' && call.method === 'PATCH').body.resources.map((item) => item.identifier), ['https://other.example.test/api', 'https://keepmealive.app/api']);
  assert.ok(!calls.some((call) => call.path === '/oauth-provider/clients' && call.method === 'POST'));
  assert.ok(!Object.hasOwn(calls.find((call) => call.path === '/plugins/oauth-provider' && call.method === 'PATCH').body, 'enabled'));
});

test('disabled child OAuth provider fails before any mutation', async () => {
  const config = { resource: 'https://keepmealive.app/api', development: { branchId: 'br-child', apiBaseUrl: 'https://api.example.test/', issuer: 'https://auth.example.test/neondb/auth' } };
  const calls = [];
  await assert.rejects(() => configureAuth(config, 'development', async (path, method = 'GET') => { calls.push({ path, method }); return { enabled: false }; }, { id: 'br-child', parent_id: 'br-root' }), /only permits enabling it on a project root/);
  assert.deepEqual(calls, [{ path: '/plugins/oauth-provider', method: 'GET' }]);
});

test('SMTP shared mode or missing credentials never enables verification links', async () => {
  const calls = [];
  await assert.rejects(() => configureSmtp({}, async (path) => { calls.push(path); return { type: 'shared' }; }), /No application-owned SMTP/);
  assert.deepEqual(calls, ['/email_provider']);
  await assert.rejects(() => configureSmtp({ SMTP_HOST: 'smtp.example.test', SMTP_TEST_RECIPIENT: 'owner@example.test' }, async () => { throw new Error('Must not call API'); }), /Missing SMTP settings/);
  await assert.rejects(() => configureSmtp({ SMTP_HOST: 'smtp.example.test' }, async () => { throw new Error('Must not call API'); }), /SMTP_TEST_RECIPIENT is required/);
});

test('SMTP tests the stored password without reading or resubmitting its redaction', async () => {
  const calls = [];
  const result = await configureSmtp({ SMTP_TEST_RECIPIENT: 'owner@example.test' }, async (path, method = 'GET', body) => {
    calls.push({ path, method, body });
    if (path === '/email_provider') { return { type: 'standard', password: '***redacted***' }; }
    return { success: true };
  });
  assert.deepEqual(calls.find((call) => call.path === '/email_provider/test').body, { recipient_email: 'owner@example.test' });
  assert.ok(!calls.some((call) => JSON.stringify(call.body)?.includes('***redacted***')));
  assert.equal(calls.at(-1).path, '/email_and_password');
  assert.equal(calls.at(-1).body.require_email_verification, true);
  assert.deepEqual(result, { smtpAcceptedTestMessage: true, inboxDeliveryVerified: false });
});

test('SMTP send failure leaves verification policy unchanged', async () => {
  const paths = [];
  await assert.rejects(() => configureSmtp({ SMTP_TEST_RECIPIENT: 'owner@example.test' }, async (path) => {
    paths.push(path);
    return path === '/email_provider' ? { type: 'standard' } : { success: false };
  }), /failed its send test/);
  assert.ok(!paths.includes('/email_and_password'));
});
