import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createLocalJWKSet, exportJWK, generateKeyPair, SignJWT } from 'jose';
import { createApp, type Account, type AccountRepository } from '../src/app.js';
import { createTokenVerifier } from '../src/auth.js';
import { authPage } from '../src/pages.js';

const issuer = 'https://auth.example.test/neondb/auth';
const audience = 'https://api.example.test/';
const alice = '11111111-1111-4111-8111-111111111111';
const bob = '22222222-2222-4222-8222-222222222222';
const { privateKey, publicKey } = await generateKeyPair('EdDSA');
const jwk = await exportJWK(publicKey);
const verify = createTokenVerifier(issuer, audience, createLocalJWKSet({ keys: [{ ...jwk, kid: 'test' }] }));
const token = (sub: string, iss = issuer, aud = audience, exp = '1h') => new SignJWT().setProtectedHeader({ alg: 'EdDSA', kid: 'test' }).setSubject(sub).setIssuer(iss).setAudience(aud).setIssuedAt().setExpirationTime(exp).sign(privateKey);
function setup() {
  const accounts = new Map<string, Account>([alice, bob].map((id) => [id, { userId: id, displayName: id === alice ? 'Alice' : 'Bob', email: `${id}@example.test`, entitlements: [] }]));
  const repository: AccountRepository = {
    async getAccount(id) { return accounts.get(id)!; },
    async updateProfile(id, name) { const account = accounts.get(id)!; account.displayName = name; return account; },
  };
  return createApp(repository, verify);
}
test('two users read and update only their authenticated profiles', async () => {
  const app = setup();
  const headers = { Authorization: `Bearer ${await token(alice)}`, 'Content-Type': 'application/json' };
  const response = await app.request('/v1/profile', { method: 'PATCH', headers, body: JSON.stringify({ displayName: 'Alice Updated' }) });
  assert.equal(response.status, 200);
  assert.equal((await response.json()).userId, alice);
  const other = await app.request('/v1/account', { headers: { Authorization: `Bearer ${await token(bob)}` } });
  assert.equal((await other.json()).displayName, 'Bob');
  const attack = await app.request('/v1/profile', { method: 'PATCH', headers, body: JSON.stringify({ userId: bob, displayName: 'Stolen' }) });
  assert.equal(attack.status, 400);
});
test('invalid, expired, wrong branch, and wrong audience tokens are rejected', async () => {
  const app = setup();
  for (const invalid of ['invalid', await token(alice, issuer + '/other'), await token(alice, issuer, audience + 'other'), await token(alice, issuer, audience, '-1s')]) {
    const response = await app.request('/v1/account', { headers: { Authorization: `Bearer ${invalid}` } });
    assert.equal(response.status, 401);
  }
  assert.equal((await app.request('/v1/account')).status, 401);
});
test('profile validation rejects invalid names, oversized payloads and invalid JSON', async () => {
  const app = setup();
  const headers = { Authorization: `Bearer ${await token(alice)}`, 'Content-Type': 'application/json' };
  for (const body of ['{', JSON.stringify({ displayName: '' }), JSON.stringify({ displayName: '\n' }), JSON.stringify({ displayName: 'x'.repeat(101) })]) {
    assert.equal((await app.request('/v1/profile', { method: 'PATCH', headers, body })).status, 400);
  }
  assert.equal((await app.request('/v1/profile', { method: 'PATCH', headers, body: 'x'.repeat(5000) })).status, 413);
});
test('health endpoint is public and account requests are rate limited', async () => {
  const app = setup();
  assert.equal((await app.request('/healthz')).status, 200);
  const headers = { Authorization: `Bearer ${await token(alice)}` };
  for (let i = 0; i < 60; i++) { assert.equal((await app.request('/v1/account', { headers })).status, 200); }
  const limited = await app.request('/v1/account', { headers });
  assert.equal(limited.status, 429);
  assert.ok(limited.headers.get('Retry-After'));
});
test('hosted auth forms are protected by nonce CSP and never cache credentials', async () => {
  const response = authPage('/login', issuer);
  assert.equal(response.headers.get('Cache-Control'), 'no-store');
  assert.match(response.headers.get('Content-Security-Policy')!, /script-src 'nonce-/);
  const html = await response.text();
  assert.match(html, /oauth_query/);
  assert.doesNotMatch(html, /localStorage|innerHTML/);
});
