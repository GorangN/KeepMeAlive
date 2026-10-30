import { randomBytes, createHash } from 'node:crypto';
import { readFile, writeFile, mkdir } from 'node:fs/promises';

if (!process.argv.includes('--live-staging') || !process.argv.includes('--emails-disabled-confirmed')) {
  throw new Error('This creates two retained synthetic staging accounts. First read back that sign-up/sign-in emails and required verification are disabled, then pass --live-staging --emails-disabled-confirmed.');
}
const config = JSON.parse(await readFile(new URL('../neon.json', import.meta.url), 'utf8'));
const staging = config.staging;
if (staging?.projectId !== 'sweet-truth-01118709' || staging.branchId !== 'br-red-flower-b9wmbkmb' || !staging.clientId) {
  throw new Error('The configured isolated staging root and public client are required.');
}
const issuer = staging.issuer;
const origin = new URL(staging.apiBaseUrl).origin;
const evidence = { target: 'staging', projectId: staging.projectId, branchId: staging.branchId, checkedAtUtc: new Date().toISOString(), protocolOnly: true, actualSmtpVerified: false, nativeBrowserVerified: false, syntheticAccountsRetained: 0, checks: [] };
const cleanupUsers = [];
const cleanupRefreshTokens = [];
function check(condition, name) {
  if (!condition) { throw new Error('Acceptance failed: ' + name); }
  evidence.checks.push({ name, passed: true });
}
const random = () => randomBytes(32).toString('base64url');

function session() {
  const cookies = new Map();
  return async (route, body, headers = {}, method) => {
    const url = new URL(issuer + route);
    const response = await fetch(url, {
      method: method ?? (body === undefined ? 'GET' : 'POST'),
      redirect: 'manual', signal: AbortSignal.timeout(30000),
      headers: { Origin: origin, ...(cookies.size ? { Cookie: [...cookies].map(([key, value]) => key + '=' + value).join('; ') } : {}), ...(body instanceof URLSearchParams ? { 'Content-Type': 'application/x-www-form-urlencoded' } : body === undefined ? {} : { 'Content-Type': 'application/json' }), ...headers },
      body: body instanceof URLSearchParams ? body : body === undefined ? undefined : JSON.stringify(body),
    });
    for (const value of response.headers.getSetCookie()) {
      const pair = value.split(';', 1)[0];
      const index = pair.indexOf('=');
      const name = pair.slice(0, index);
      if (/max-age=0(?:;|$)/i.test(value) || pair.slice(index + 1) === '') { cookies.delete(name); }
      else { cookies.set(name, pair.slice(index + 1)); }
    }
    const text = await response.text();
    let data = null;
    try { data = text ? JSON.parse(text) : null; } catch { }
    return { status: response.status, data, location: response.headers.get('Location') };
  };
}

async function api(path, accessToken, body) {
  const response = await fetch(new URL(path, staging.apiBaseUrl), {
    method: body === undefined ? 'GET' : 'PATCH', redirect: 'error', signal: AbortSignal.timeout(30000),
    headers: { Authorization: `Bearer ${accessToken}`, ...(body === undefined ? {} : { 'Content-Type': 'application/json' }) }, body: body === undefined ? undefined : JSON.stringify(body),
  });
  let data = null;
  try { data = await response.json(); } catch { }
  return { status: response.status, data };
}

async function main() {
  const anonymous = session();
  const discovery = await anonymous('/.well-known/oauth-authorization-server');
  check(discovery.status === 200 && discovery.data?.issuer === issuer, 'discovery pins the exact staging issuer');
  const metadata = discovery.data;
  for (const key of ['authorization_endpoint', 'token_endpoint']) {
    const endpoint = new URL(metadata[key]);
    check(endpoint.origin === new URL(issuer).origin && endpoint.pathname.startsWith(new URL(issuer).pathname + '/'), key + ' stays under the configured issuer');
  }

  async function createUser(label) {
    const request = session();
    const email = 'keepmealive-' + randomBytes(12).toString('hex') + '@example.test';
    const password = random();
    const signup = await request('/sign-up/email', { name: 'KeepMeAlive staging ' + label, email, password, callbackURL: new URL('login', staging.apiBaseUrl).href });
    check(signup.status >= 200 && signup.status < 300 && Boolean(signup.data?.user?.id), label + ' synthetic reserved-domain account created without requesting email');
    evidence.syntheticAccountsRetained++;
    const user = { request, userId: signup.data.user.id, email, password };
    cleanupUsers.push(user);
    const login = await request('/sign-in/email', { email, password });
    check(login.status === 200, label + ' email-password session established');
    const current = await request('/get-session');
    const userId = current.data?.user?.id ?? current.data?.session?.user?.id;
    check(current.status === 200 && Boolean(userId), label + ' browser-cookie session restored');
    user.userId = userId;
    return user;
  }

  async function authorize(user, options = {}) {
    const verifier = random();
    const state = random();
    const redirect = 'http://127.0.0.1:49152/callback/';
    const parameters = new URLSearchParams({ response_type: 'code', client_id: staging.clientId, redirect_uri: redirect, scope: 'openid offline_access', resource: config.resource, state, code_challenge: createHash('sha256').update(verifier).digest('base64url'), code_challenge_method: 'S256' });
    if (options.redirect) { parameters.set('redirect_uri', options.redirect); }
    if (options.missingPkce) { parameters.delete('code_challenge'); parameters.delete('code_challenge_method'); }
    const response = await user.request('/oauth2/authorize?' + parameters);
    return { response, verifier, state, redirect };
  }

  async function accept(user, authorization) {
    let target = authorization.response.location;
    if (!target && authorization.response.data?.url) { target = authorization.response.data.url; }
    check(Boolean(target), 'authorization returns a hosted continuation');
    let destination = new URL(target);
    check(destination.origin === origin && ['/login', '/consent'].includes(destination.pathname), 'authorization continuation uses the hosted staging origin');
    if (destination.pathname === '/login') {
      const response = await user.request('/sign-in/email', { email: user.email, password: user.password, oauth_query: destination.search.slice(1) });
      check(response.status === 200 && Boolean(response.data?.url), 'signed OAuth request resumes after login');
      destination = new URL(response.data.url);
    }
    check(destination.origin === origin && destination.pathname === '/consent', 'public desktop client requires consent');
    const client = await user.request('/oauth2/public-client?client_id=' + encodeURIComponent(staging.clientId));
    check(client.status === 200 && client.data?.client_id === staging.clientId && typeof client.data.client_name === 'string', 'consent metadata identifies the registered public client');
    const consent = await user.request('/oauth2/consent', { accept: true, oauth_query: destination.search.slice(1) });
    check(consent.status === 200 && Boolean(consent.data?.url), 'signed consent request accepted');
    const callback = new URL(consent.data.url);
    check(callback.origin === new URL(authorization.redirect).origin && callback.pathname === '/callback/' && callback.searchParams.get('state') === authorization.state && Boolean(callback.searchParams.get('code')), 'callback pins redirect state and authorization code');
    return callback.searchParams.get('code');
  }

  async function exchange(user, code, authorization) {
    return user.request('/oauth2/token', new URLSearchParams({ grant_type: 'authorization_code', client_id: staging.clientId, code, code_verifier: authorization.verifier, redirect_uri: authorization.redirect, resource: config.resource }));
  }

  const alice = await createUser('A');
  function authorizationRejected(result) {
    if (result.status >= 400 || result.data?.error) { return true; }
    const raw = result.location ?? result.data?.url;
    if (!raw) { return false; }
    const destination = new URL(raw);
    const expectedError = destination.origin === new URL(issuer).origin && destination.pathname === new URL(issuer).pathname + '/error';
    const expectedCallback = destination.origin === 'http://127.0.0.1:49152' && destination.pathname === '/callback/';
    return (expectedError || expectedCallback) && destination.searchParams.has('error') && !destination.searchParams.has('code');
  }
  const invalidRedirect = await authorize(alice, { redirect: 'https://invalid.example.test/callback' });
  check(authorizationRejected(invalidRedirect.response), 'unregistered redirect is rejected');
  const missingPkce = await authorize(alice, { missingPkce: true });
  check(authorizationRejected(missingPkce.response), 'public-client authorization without PKCE is rejected');
  const first = await authorize(alice);
  const code = await accept(alice, first);
  const tokenA = await exchange(alice, code, first);
  check(tokenA.status === 200 && tokenA.data?.token_type?.toLowerCase() === 'bearer' && Boolean(tokenA.data.access_token) && Boolean(tokenA.data.refresh_token), 'S256 authorization code exchanges for access and refresh tokens');
  cleanupRefreshTokens.push({ request: alice.request, token: tokenA.data.refresh_token });
  const profileA = await api('v1/account', tokenA.data.access_token);
  check(profileA.status === 200 && profileA.data?.userId === alice.userId, 'API accepts staging token for its authenticated account');
  const changed = await api('v1/profile', tokenA.data.access_token, { displayName: 'Protocol acceptance A' });
  check(changed.status === 200 && changed.data?.displayName === 'Protocol acceptance A', 'authenticated profile update persists');

  const bob = await createUser('B');
  const second = await authorize(bob);
  const tokenB = await exchange(bob, await accept(bob, second), second);
  check(tokenB.status === 200 && Boolean(tokenB.data?.access_token), 'second independent account obtains its own access token');
  cleanupRefreshTokens.push({ request: bob.request, token: tokenB.data.refresh_token });
  const profileB = await api('v1/account', tokenB.data.access_token);
  check(profileB.status === 200 && profileB.data?.userId === bob.userId && bob.userId !== alice.userId && profileB.data?.displayName !== 'Protocol acceptance A', 'two real accounts remain isolated');
  const forged = await api('v1/profile', tokenA.data.access_token, { userId: bob.userId, displayName: 'forged' });
  check(forged.status === 400, 'caller-supplied alternate account ownership is rejected');
  const unchanged = await api('v1/account', tokenB.data.access_token);
  check(unchanged.data?.displayName === profileB.data.displayName, 'alternate account remains unchanged after forged update');

  const refresh = await alice.request('/oauth2/token', new URLSearchParams({ grant_type: 'refresh_token', client_id: staging.clientId, refresh_token: tokenA.data.refresh_token, resource: config.resource }));
  check(refresh.status === 200 && Boolean(refresh.data?.access_token) && Boolean(refresh.data?.refresh_token) && refresh.data.refresh_token !== tokenA.data.refresh_token, 'refresh credential rotates and returns a new access token');
  cleanupRefreshTokens.push({ request: alice.request, token: refresh.data.refresh_token });
  check((await api('v1/account', refresh.data.access_token)).status === 200, 'rotated access token remains authorized');
  const revocationEndpoint = metadata.revocation_endpoint ?? issuer + '/oauth2/revoke';
  check(revocationEndpoint.startsWith(issuer + '/'), 'revocation endpoint remains pinned to staging issuer');
  const revoked = await alice.request(revocationEndpoint.slice(issuer.length), new URLSearchParams({ token: refresh.data.refresh_token, token_type_hint: 'refresh_token', client_id: staging.clientId }));
  check(revoked.status >= 200 && revoked.status < 300, 'rotated refresh credential revoked');
  const afterRevoke = await alice.request('/oauth2/token', new URLSearchParams({ grant_type: 'refresh_token', client_id: staging.clientId, refresh_token: refresh.data.refresh_token, resource: config.resource }));
  check(afterRevoke.status >= 400, 'revoked refresh credential cannot obtain another token');
  const reused = await exchange(alice, code, first);
  check(reused.status >= 400, 'authorization code reuse is rejected');
  await bob.request(revocationEndpoint.slice(issuer.length), new URLSearchParams({ token: tokenB.data.refresh_token, token_type_hint: 'refresh_token', client_id: staging.clientId }));
  for (const user of [alice, bob]) {
    const out = await user.request('/sign-out', {});
    check(out.status >= 200 && out.status < 300, 'browser-cookie session signed out');
    const current = await user.request('/get-session');
    check(!current.data?.session, 'signed-out browser session is no longer restored');
  }
}

try {
  await main();
  evidence.passed = true;
} catch (error) {
  evidence.passed = false;
  evidence.failure = error instanceof Error ? error.message : 'Protocol acceptance failed';
  process.exitCode = 1;
} finally {
  for (const item of cleanupRefreshTokens) {
    try { await item.request('/oauth2/revoke', new URLSearchParams({ token: item.token, token_type_hint: 'refresh_token', client_id: staging.clientId })); } catch { }
  }
  for (const user of cleanupUsers) {
    try { await user.request('/sign-out', {}); } catch { }
  }
  await mkdir(new URL('../dist', import.meta.url), { recursive: true });
  await writeFile(new URL('../dist/staging-oauth-acceptance.json', import.meta.url), JSON.stringify(evidence, null, 2) + '\n');
  console.log(JSON.stringify(evidence, null, 2));
}
