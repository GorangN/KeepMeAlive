import assert from 'node:assert/strict';
import { access, mkdir, mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { resolve, sep } from 'node:path';
import { test } from 'node:test';
import { createApp } from '../src/app.ts';
import { buildNativeOutage, createNativeOutageEntry, createOutageRepository } from '../scripts/build-native-outage.mjs';

const config = {
  function: 'keepmealive', resource: 'https://keepmealive.app/api',
  staging: {
    projectId: 'sweet-truth-01118709', branchId: 'br-red-flower-b9wmbkmb',
    issuer: 'https://ep-red-rain-b9c8tumi.neonauth.c-7.eu-central-1.aws.neon.tech/neondb/auth',
    apiBaseUrl: 'https://br-red-flower-b9wmbkmb-keepmealive.compute.c-7.eu-central-1.aws.neon.tech/',
  },
};
const originalEntry = 'const app = createApp(new PostgresAccountRepository(pool), verifier); export default app;';

async function fixture(t, target = config, source = originalEntry) {
  const directory = await mkdtemp(resolve(tmpdir(), 'keepmealive-native-outage-'));
  assert.ok(directory.startsWith(resolve(tmpdir()) + sep + 'keepmealive-native-outage-'));
  t.after(() => rm(directory, { recursive: true, force: true }));
  await mkdir(resolve(directory, 'src'));
  await writeFile(resolve(directory, 'neon.json'), JSON.stringify(target));
  await writeFile(resolve(directory, 'src/index.ts'), source);
  return directory;
}

async function unchanged(directory, source) {
  assert.equal(await readFile(resolve(directory, 'src/index.ts'), 'utf8'), source);
  await assert.rejects(access(resolve(directory, 'dist/native-outage')), { code: 'ENOENT' });
}

test('native outage requires explicit confirmation before writing files', async (t) => {
  const directory = await fixture(t);
  await assert.rejects(() => buildNativeOutage(directory, false), /Explicit --staging-outage-validation/);
  await unchanged(directory, originalEntry);
});

test('native outage rejects every staging identity mismatch before writing files', async (t) => {
  for (const change of [{ projectId: 'production' }, { branchId: 'production' }, { issuer: 'https://production.example.test/auth' }, { apiBaseUrl: 'https://production.example.test/' }]) {
    const directory = await fixture(t, { ...config, staging: { ...config.staging, ...change } });
    await assert.rejects(() => buildNativeOutage(directory, true), /Only the pinned isolated/);
    await unchanged(directory, originalEntry);
  }
});

test('native outage rejects missing or duplicated repository markers before writing files', async (t) => {
  for (const source of ['export default {};', originalEntry + '\n' + originalEntry]) {
    const directory = await fixture(t, config, source);
    await assert.rejects(() => buildNativeOutage(directory, true), /exactly one account repository marker/);
    await unchanged(directory, source);
  }
});

test('native outage refuses an unverified recovery bundle before writing files', async (t) => {
  const directory = await fixture(t);
  await mkdir(resolve(directory, 'dist'));
  await writeFile(resolve(directory, 'dist/function.zip'), 'not-the-verified-deployment');
  await assert.rejects(() => buildNativeOutage(directory, true), /does not match verified staging deployment 2/);
  await unchanged(directory, originalEntry);
  assert.equal(await readFile(resolve(directory, 'dist/function.zip'), 'utf8'), 'not-the-verified-deployment');
});

test('generated outage repository retains authentication, readiness and redacted 503 responses', async (t) => {
  const generated = createNativeOutageEntry(config, originalEntry, true);
  assert.ok(generated.includes(`(${createOutageRepository.toString()})()`));
  assert.ok(!generated.includes('new PostgresAccountRepository(pool)'));
  const logged = [];
  t.mock.method(console, 'error', (value) => logged.push(value));
  const app = createApp(createOutageRepository(), async (token) => {
    if (token !== 'synthetic-valid-token') { throw new Error('Invalid synthetic token'); }
    return { sub: '11111111-1111-4111-8111-111111111111' };
  });
  for (const path of ['/v1/account', '/v1/profile']) {
    const response = await app.request(path, { method: path.endsWith('/profile') ? 'PATCH' : 'GET' });
    assert.equal(response.status, 401);
    assert.deepEqual(await response.json(), { error: 'unauthorized' });
  }
  assert.equal((await app.request('/readyz')).status, 200);
  assert.equal((await app.request('/v1/account', { headers: { Authorization: 'Bearer invalid-token' } })).status, 401);
  for (const path of ['/v1/account', '/v1/profile']) {
    const response = await app.request(path, {
      method: path.endsWith('/profile') ? 'PATCH' : 'GET',
      headers: { Authorization: 'Bearer synthetic-valid-token', 'Content-Type': 'application/json' },
      ...(path.endsWith('/profile') ? { body: JSON.stringify({ displayName: 'Test' }) } : {}),
    });
    assert.equal(response.status, 503);
    assert.equal(response.headers.get('Cache-Control'), 'no-store');
    assert.deepEqual(await response.json(), { error: 'service_unavailable' });
  }
  assert.equal(logged.length, 2);
  for (const value of logged) {
    const record = JSON.parse(value);
    assert.deepEqual(Object.keys(record).sort(), ['event', 'requestId']);
    assert.equal(record.event, 'request_failed');
    assert.match(record.requestId, /^[0-9a-f-]{36}$/);
    assert.ok(!value.includes('Controlled staging'));
  }
});
