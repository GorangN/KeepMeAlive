import { randomBytes } from 'node:crypto';
import { readFile } from 'node:fs/promises';
import { pathToFileURL } from 'node:url';

const stagingIssuer = 'https://ep-red-rain-b9c8tumi.neonauth.c-7.eu-central-1.aws.neon.tech/neondb/auth';
const stagingOrigin = 'https://br-red-flower-b9wmbkmb-keepmealive.compute.c-7.eu-central-1.aws.neon.tech';

export async function createNativeFixture(config, password, request = fetch) {
  const staging = config.staging;
  if (staging?.projectId !== 'sweet-truth-01118709' || staging.branchId !== 'br-red-flower-b9wmbkmb' || staging.issuer !== stagingIssuer || new URL(staging.apiBaseUrl).origin !== stagingOrigin) {
    throw new Error('Only the isolated KeepMeAlive staging root is allowed.');
  }
  if (typeof password !== 'string' || password.length < 40 || password.length > 128 || new Set(password).size < 16) {
    throw new Error('Supply a randomly generated synthetic password of 40 to 128 characters through the environment.');
  }
  const email = 'keepmealive-native-' + randomBytes(12).toString('hex') + '@example.test';
  const name = 'KeepMeAlive native validation';
  const signup = await request(stagingIssuer + '/sign-up/email', {
    method: 'POST', redirect: 'error', headers: { Origin: stagingOrigin, 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password, name, callbackURL: stagingOrigin + '/login' }), signal: AbortSignal.timeout(30000),
  });
  if (!signup.ok) { throw new Error('Synthetic native fixture creation failed (HTTP ' + signup.status + ').'); }
  const created = await signup.json();
  if (created.user?.email !== email || typeof created.user?.id !== 'string') { throw new Error('The synthetic native fixture response was incomplete.'); }
  const cookie = signup.headers.getSetCookie().map((value) => value.split(';', 1)[0]).join('; ');
  if (!cookie) { throw new Error('The signup session could not be identified for cleanup; do not retry account creation automatically.'); }
  const headers = { Origin: stagingOrigin, Cookie: cookie, 'Content-Type': 'application/json' };
  const signedOut = await request(stagingIssuer + '/sign-out', { method: 'POST', redirect: 'error', headers, body: '{}', signal: AbortSignal.timeout(30000) });
  if (!signedOut.ok) { throw new Error('The fixture was created but its bootstrap session could not be revoked; do not retry automatically.'); }
  const checked = await request(stagingIssuer + '/get-session', { redirect: 'error', headers, signal: AbortSignal.timeout(30000) });
  if (!checked.ok) { throw new Error('The fixture was created but bootstrap session cleanup could not be verified.'); }
  const remaining = await checked.json();
  if (remaining?.session || remaining?.user) { throw new Error('The fixture was created but its bootstrap session remains active.'); }
  return { email, name, userId: created.user.id, target: 'staging', bootstrapSessionRevoked: true, actualSmtpVerified: false };
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  try {
    if (!process.argv.includes('--live-staging') || !process.argv.includes('--emails-disabled-confirmed')) {
      throw new Error('Explicit staging and current no-email-policy confirmation flags are required.');
    }
    const password = process.env.NATIVE_TEST_PASSWORD;
    delete process.env.NATIVE_TEST_PASSWORD;
    const config = JSON.parse(await readFile(new URL('../neon.json', import.meta.url), 'utf8'));
    console.log(JSON.stringify(await createNativeFixture(config, password)));
  } catch (error) {
    console.error(error instanceof Error ? error.message : 'Synthetic native fixture preparation failed.');
    process.exitCode = 1;
  }
}
