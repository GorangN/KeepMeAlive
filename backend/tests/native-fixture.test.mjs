import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createNativeFixture } from '../scripts/create-native-fixture.mjs';

const staging = {
  projectId: 'sweet-truth-01118709', branchId: 'br-red-flower-b9wmbkmb',
  issuer: 'https://ep-red-rain-b9c8tumi.neonauth.c-7.eu-central-1.aws.neon.tech/neondb/auth',
  apiBaseUrl: 'https://br-red-flower-b9wmbkmb-keepmealive.compute.c-7.eu-central-1.aws.neon.tech/',
};
const password = 'Synthetic-Only-0123456789-AbCdEfGhIjKlMnOpQrStUvWxYz';

test('native fixture rejects other roots and weak passwords before making requests', async () => {
  const noRequest = async () => { assert.fail('No network request expected'); };
  for (const change of [{ projectId: 'production' }, { branchId: 'production' }, { issuer: 'https://production.example.test/auth' }, { apiBaseUrl: 'https://production.example.test/' }]) {
    await assert.rejects(() => createNativeFixture({ staging: { ...staging, ...change } }, password, noRequest), /Only the isolated/);
  }
  await assert.rejects(() => createNativeFixture({ staging }, 'short', noRequest), /randomly generated/);
  await assert.rejects(() => createNativeFixture({ staging }, 'a'.repeat(48), noRequest), /randomly generated/);
});

test('native fixture returns only public identity after revoking and checking its signup session', async () => {
  const calls = [];
  let email;
  const fixture = await createNativeFixture({ staging }, password, async (url, init) => {
    calls.push({ url, init });
    if (url.endsWith('/sign-up/email')) {
      const body = JSON.parse(init.body);
      email = body.email;
      assert.equal(body.password, password);
      assert.match(email, /^keepmealive-native-[a-f0-9]{24}@example\.test$/);
      return Response.json({ user: { email, id: 'fixture-user' }, token: 'private-bootstrap-token' }, { headers: { 'Set-Cookie': 'staging-session=private-cookie; Secure; HttpOnly; Path=/' } });
    }
    assert.equal(init.headers.Cookie, 'staging-session=private-cookie');
    return Response.json(url.endsWith('/sign-out') ? { success: true } : null);
  });
  assert.deepEqual(calls.map((call) => new URL(call.url).pathname.split('/').at(-1)), ['email', 'sign-out', 'get-session']);
  assert.deepEqual(fixture, { email, name: 'KeepMeAlive native validation', userId: 'fixture-user', target: 'staging', bootstrapSessionRevoked: true, actualSmtpVerified: false });
  for (const secret of [password, 'private-bootstrap-token', 'private-cookie']) { assert.ok(!JSON.stringify(fixture).includes(secret)); }
});

test('native fixture does not claim cleanup when the revoked bootstrap cookie still has a session', async () => {
  let email;
  await assert.rejects(() => createNativeFixture({ staging }, password, async (url, init) => {
    if (url.endsWith('/sign-up/email')) {
      email = JSON.parse(init.body).email;
      return Response.json({ user: { email, id: 'fixture-user' } }, { headers: { 'Set-Cookie': 'session=private; HttpOnly; Path=/' } });
    }
    return Response.json(url.endsWith('/sign-out') ? { success: true } : { session: { id: 'still-active' }, user: { id: 'fixture-user' } });
  }), /bootstrap session remains active/);
});
