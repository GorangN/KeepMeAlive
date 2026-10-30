import { createServer } from 'node:http';
import { randomBytes, createHash } from 'node:crypto';
import { readFile, writeFile, mkdir } from 'node:fs/promises';

if (!process.argv.includes('--live-staging') || !process.argv.includes('--emails-disabled-confirmed')) { throw new Error('Explicit staging and no-email confirmation flags are required.'); }
const config = JSON.parse(await readFile(new URL('../neon.json', import.meta.url), 'utf8'));
const staging = config.staging;
if (staging?.projectId !== 'sweet-truth-01118709' || staging.branchId !== 'br-red-flower-b9wmbkmb' || !staging.clientId) { throw new Error('The isolated staging root is required.'); }
const email = process.env.BROWSER_TEST_EMAIL;
const password = process.env.BROWSER_TEST_PASSWORD;
if (!email?.endsWith('@example.test') || !password || password.length < 32) { throw new Error('Supply a strong synthetic reserved-domain fixture through the environment.'); }
const issuer = staging.issuer;
const origin = new URL(staging.apiBaseUrl).origin;
const signup = await fetch(issuer + '/sign-up/email', { method: 'POST', redirect: 'error', headers: { Origin: origin, 'Content-Type': 'application/json' }, body: JSON.stringify({ email, password, name: 'KeepMeAlive browser acceptance', callbackURL: origin + '/login' }), signal: AbortSignal.timeout(30000) });
if (!signup.ok) { throw new Error('Synthetic browser fixture could not be created (HTTP ' + signup.status + ').'); }
const created = await signup.json();
if (!created.user?.id) { throw new Error('Synthetic fixture response was incomplete.'); }
const state = randomBytes(32).toString('base64url');
const verifier = randomBytes(32).toString('base64url');
const redirect = 'http://127.0.0.1:49152/callback/';
const parameters = new URLSearchParams({ response_type: 'code', client_id: staging.clientId, redirect_uri: redirect, scope: 'openid offline_access', resource: config.resource, state, code_challenge: createHash('sha256').update(verifier).digest('base64url'), code_challenge_method: 'S256', prompt: 'consent' });
const evidence = { target: 'staging', projectId: staging.projectId, checkedAtUtc: new Date().toISOString(), passed: false, browserCallbackReceived: false, callbackStateMatched: false, pkceExchangePassed: false, authenticatedApiPassed: false, refreshRevoked: false, browserCookieSignedOut: false, nativeWpfVerified: false, actualSmtpVerified: false };
let finishing = false;
const server = createServer(async (request, response) => {
  if (finishing) { response.writeHead(409).end(); return; }
  const callback = new URL(request.url, redirect);
  if (request.method !== 'GET' || callback.pathname !== '/callback/' || request.headers.host !== '127.0.0.1:49152') { response.writeHead(404).end(); return; }
  finishing = true;
  let refreshToken;
  try {
    evidence.browserCallbackReceived = true;
    if (callback.searchParams.get('state') !== state || !callback.searchParams.get('code') || callback.searchParams.has('error')) { throw new Error('Browser callback state or authorization was invalid.'); }
    evidence.callbackStateMatched = true;
    const exchanged = await fetch(issuer + '/oauth2/token', { method: 'POST', redirect: 'error', headers: { 'Content-Type': 'application/x-www-form-urlencoded' }, body: new URLSearchParams({ grant_type: 'authorization_code', client_id: staging.clientId, code: callback.searchParams.get('code'), code_verifier: verifier, redirect_uri: redirect, resource: config.resource }), signal: AbortSignal.timeout(30000) });
    if (!exchanged.ok) { throw new Error('Browser PKCE exchange failed with HTTP ' + exchanged.status + '.'); }
    const token = await exchanged.json();
    if (!token.access_token || !token.refresh_token) { throw new Error('Browser exchange returned an incomplete token response.'); }
    refreshToken = token.refresh_token;
    evidence.pkceExchangePassed = true;
    const profile = await fetch(new URL('v1/account', staging.apiBaseUrl), { redirect: 'error', headers: { Authorization: 'Bearer ' + token.access_token }, signal: AbortSignal.timeout(30000) });
    const account = await profile.json();
    if (!profile.ok || account.userId !== created.user.id) { throw new Error('The browser token did not retrieve its own account.'); }
    evidence.authenticatedApiPassed = true;
    const revoke = await fetch(issuer + '/oauth2/revoke', { method: 'POST', redirect: 'error', headers: { 'Content-Type': 'application/x-www-form-urlencoded' }, body: new URLSearchParams({ token: refreshToken, token_type_hint: 'refresh_token', client_id: staging.clientId }), signal: AbortSignal.timeout(30000) });
    evidence.refreshRevoked = revoke.ok;
    evidence.passed = revoke.ok;
    response.writeHead(200, { 'Content-Type': 'text/plain; charset=utf-8', 'Cache-Control': 'no-store' }).end('Browser sign-in verified. The loopback state, PKCE exchange, and staging account API passed. You can close this tab.');
  } catch (error) {
    evidence.failure = error instanceof Error ? error.message : 'Browser acceptance failed';
    response.writeHead(400, { 'Content-Type': 'text/plain; charset=utf-8', 'Cache-Control': 'no-store' }).end('Browser sign-in verification failed. Return to the test report.');
  } finally {
    clearTimeout(timeout);
    if (refreshToken && !evidence.refreshRevoked) {
      try {
        const revoked = await fetch(issuer + '/oauth2/revoke', { method: 'POST', redirect: 'error', headers: { 'Content-Type': 'application/x-www-form-urlencoded' }, body: new URLSearchParams({ token: refreshToken, token_type_hint: 'refresh_token', client_id: staging.clientId }), signal: AbortSignal.timeout(30000) });
        evidence.refreshRevoked = revoked.ok;
      } catch { }
    }
    await mkdir(new URL('../dist', import.meta.url), { recursive: true });
    await writeFile(new URL('../dist/staging-browser-acceptance.json', import.meta.url), JSON.stringify(evidence, null, 2) + '\n');
    console.log(JSON.stringify(evidence));
    server.close();
    if (!evidence.passed) { process.exitCode = 1; }
  }
});
const timeout = setTimeout(() => { console.error('Browser acceptance timed out without a completed callback.'); server.close(); process.exitCode = 1; }, 300000);
server.listen(49152, '127.0.0.1', () => console.log(JSON.stringify({ ready: true, authorizationUrl: issuer + '/oauth2/authorize?' + parameters })));
