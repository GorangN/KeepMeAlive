import assert from 'node:assert/strict';
import { test } from 'node:test';
import { Script } from 'node:vm';
import { authContinuationUrl, authPage, readOAuthQuery, safeAuthDestination } from '../src/pages.js';

const origin = 'https://api.example.test';
const issuer = 'https://auth.example.test/neondb/auth';
const query = 'client_id=desktop&redirect_uri=http%3A%2F%2F127.0.0.1%3A49152%2Fcallback%2F&scope=openid%20offline_access&state=a%2fb%2Bc&signature=x+Y';

test('OAuth continuation preserves the signed query byte for byte through email callback parameters', () => {
  assert.equal(readOAuthQuery('?' + query), query);
  const reset = new URL(authContinuationUrl('/reset-password', origin, query));
  reset.searchParams.set('token', 'private-reset-token');
  assert.equal(readOAuthQuery(reset.search), query);
  const login = authContinuationUrl('/login', origin, readOAuthQuery(reset.search));
  assert.equal(readOAuthQuery(new URL(login).search), query);
  assert.ok(!login.includes('private-reset-token'));
  assert.equal(readOAuthQuery('?token=private-reset-token'), '');
  assert.throws(() => readOAuthQuery('?keepmealive_oauth=a&keepmealive_oauth=b'), /ambiguous/);
});

test('navigation permits the exact active desktop callback and known hosted pages only', () => {
  assert.equal(safeAuthDestination('http://127.0.0.1:49152/callback/?code=code&state=value', origin, issuer, query), 'http://127.0.0.1:49152/callback/?code=code&state=value');
  assert.equal(safeAuthDestination('/consent?request=signed', origin, issuer, query), origin + '/consent?request=signed');
  for (const destination of ['https://evil.example/login', 'javascript:alert(1)', 'https://api.example.test@evil.example/login', 'http://127.0.0.1:49153/callback/?code=code', 'http://127.0.0.1:49152/other', 'http://user@127.0.0.1:49152/callback/', 'http://127.0.0.1:49152/callback/#fragment', 'https://api.example.test/unregistered']) {
    assert.throws(() => safeAuthDestination(destination, origin, issuer, query));
  }
});

type TestElement = {
  value: string; href: string; textContent: string; disabled: boolean; hidden: boolean; required: boolean; autocomplete: string;
  onsubmit?: (event: { preventDefault(): void }) => Promise<void>;
  onclick?: () => Promise<void>;
};

async function render(path: string, search: string, response: (route: string) => object = () => ({})) {
  const html = await authPage(path, issuer).text();
  const code = html.match(/<script nonce="[^"]+">([\s\S]*)<\/script>/)?.[1];
  assert.ok(code);
  const elements: Record<string, TestElement> = {};
  for (const id of ['status', 'auth', 'submit', 'email', 'password', 'name', 'email-label', 'password-label', 'details', 'accept', 'deny', 'switch-account', 'login', 'register', 'reset', 'verify']) {
    elements[id] = { value: id === 'password' ? 'not-a-real-password' : id === 'email' ? 'user@example.test' : 'User', href: '/', textContent: '', disabled: id === 'accept', hidden: false, required: true, autocomplete: '' };
  }
  const links = ['login', 'register', 'reset', 'verify'].map((id) => elements[id]!);
  for (const [index, route] of ['/login', '/register', '/reset-password', '/verify-email'].entries()) { links[index]!.href = route; }
  const requests: { route: string; body: Record<string, unknown> | null }[] = [];
  const destinations: string[] = [];
  new Script(code).runInNewContext({
    URL, URLSearchParams,
    location: { origin, search, assign: (value: string) => destinations.push(value) },
    document: { getElementById: (id: string) => elements[id], querySelectorAll: () => links },
    fetch: async (url: string, init: { body?: string }) => {
      const route = url.slice(issuer.length);
      requests.push({ route, body: init.body ? JSON.parse(init.body) as Record<string, unknown> : null });
      const result = response(route);
      return { ok: !(result instanceof Error), json: async () => result instanceof Error ? { message: result.message } : result };
    },
  });
  await new Promise((resolve) => setImmediate(resolve));
  return { elements, requests, destinations, links };
}

test('registration and verification callbacks retain authorization context', async () => {
  for (const path of ['/register', '/verify-email']) {
    const page = await render(path, '?' + query);
    await page.elements.auth!.onsubmit!({ preventDefault() {} });
    const callback = page.requests.at(-1)!.body!.callbackURL as string;
    assert.equal(new URL(callback).pathname, '/login');
    assert.equal(readOAuthQuery(new URL(callback).search), query);
  }
});

test('verification requested by sign-in returns to the exact pending authorization', async () => {
  const page = await render('/login', '?' + query, (route) => route === '/sign-in/email' ? new Error('Email not verified') : {});
  await page.elements.auth!.onsubmit!({ preventDefault() {} });
  const signIn = page.requests.find((entry) => entry.route === '/sign-in/email')!;
  assert.equal(signIn.body!.oauth_query, query);
  const callback = new URL(signIn.body!.callbackURL as string);
  assert.equal(callback.origin, origin);
  assert.equal(callback.pathname, '/login');
  assert.equal(readOAuthQuery(callback.search), query);
  assert.equal(page.destinations.length, 0);
  assert.equal(page.elements.status!.textContent, 'Email not verified');

  const verified = await render('/login', callback.search, (route) => route === '/get-session' ? { session: { userId: 'verified-user' } } : { url: '/consent?' + query });
  assert.equal(verified.requests.find((entry) => entry.route === '/oauth2/continue')!.body!.oauth_query, query);
  assert.equal(verified.destinations.length, 1);
});

test('invalid verification callbacks show safe recovery guidance without resuming a browser session', async () => {
  for (const path of ['/login', '/verify-email']) {
    for (const error of ['invalid_token', 'TOKEN_EXPIRED', '<script>private-query-value</script>']) {
      const callback = new URL(authContinuationUrl(path, origin, query));
      callback.searchParams.set('error', error);
      const page = await render(path, callback.search, () => ({ session: { userId: 'existing-user' } }));
      assert.match(page.elements.status!.textContent, /invalid|expired/i);
      assert.match(page.elements.status!.textContent, /Verify email/);
      assert.ok(!page.elements.status!.textContent.includes(error));
      assert.equal(page.requests.length, 0);
      assert.equal(page.destinations.length, 0);
      assert.ok(page.links.every((link) => !new URL(link.href).searchParams.has('error')));
      assert.ok(page.links.every((link) => readOAuthQuery(new URL(link.href).search) === query));
      const resend = await render('/verify-email', new URL(page.elements.verify!.href).search);
      await resend.elements.auth!.onsubmit!({ preventDefault() {} });
      const verification = resend.requests.at(-1)!;
      assert.equal(verification.route, '/send-verification-email');
      assert.equal(readOAuthQuery(new URL(verification.body!.callbackURL as string).search), query);
    }
  }
});

test('invalid reset callbacks request a new link without submitting an accompanying token or echoing errors', async () => {
  for (const error of ['INVALID_TOKEN', 'TOKEN_EXPIRED', '<script>private-query-value</script>']) {
    const callback = new URL(authContinuationUrl('/reset-password', origin, query));
    callback.searchParams.set('error', error);
    callback.searchParams.set('token', 'untrusted-reset-token');
    const page = await render('/reset-password', callback.search);
    assert.match(page.elements.status!.textContent, /invalid|expired/i);
    assert.match(page.elements.status!.textContent, /request a new link/i);
    assert.ok(!page.elements.status!.textContent.includes(error));
    assert.equal(page.elements['email-label']!.hidden, false);
    assert.equal(page.elements.email!.required, true);
    assert.equal(page.elements['password-label']!.hidden, true);
    assert.equal(page.elements.password!.required, false);
    assert.ok(page.links.every((link) => !new URL(link.href).searchParams.has('token') && !new URL(link.href).searchParams.has('error')));
    await page.elements.auth!.onsubmit!({ preventDefault() {} });
    assert.equal(page.requests.length, 1);
    assert.equal(page.requests[0]!.route, '/request-password-reset');
    assert.equal(page.requests[0]!.body!.email, 'user@example.test');
    assert.ok(!('token' in page.requests[0]!.body!));
    assert.equal(readOAuthQuery(new URL(page.requests[0]!.body!.redirectTo as string).search), query);
  }
});

test('password reset consumes its token locally without spreading it into sign-in links or oauth_query', async () => {
  const reset = new URL(authContinuationUrl('/reset-password', origin, query));
  const request = await render('/reset-password', reset.search);
  await request.elements.auth!.onsubmit!({ preventDefault() {} });
  const redirectTo = request.requests.at(-1)!.body!.redirectTo as string;
  assert.equal(readOAuthQuery(new URL(redirectTo).search), query);
  reset.searchParams.set('token', 'private-reset-token');
  const complete = await render('/reset-password', reset.search);
  assert.ok(complete.links.every((link) => !link.href.includes('private-reset-token')));
  await complete.elements.auth!.onsubmit!({ preventDefault() {} });
  assert.equal(complete.requests.at(-1)!.route, '/reset-password');
  assert.equal(complete.requests.at(-1)!.body!.token, 'private-reset-token');
  assert.equal(complete.elements.password!.value, '');
  const login = await render('/login', new URL(complete.links[0]!.href).search);
  await login.elements.auth!.onsubmit!({ preventDefault() {} });
  assert.equal(login.requests.find((entry) => entry.route === '/sign-in/email')!.body!.oauth_query, query);
});

test('consent stays disabled unless requesting client metadata matches', async () => {
  const rejected = await render('/consent', '?' + query, () => ({ client_id: 'other-client', client_name: 'Misleading name' }));
  assert.equal(rejected.elements.accept!.disabled, true);
  await rejected.elements.deny!.onclick!();
  assert.equal(rejected.requests.at(-1)!.route, '/oauth2/consent');
  assert.equal(rejected.requests.at(-1)!.body!.accept, false);
  const valid = await render('/consent', '?' + query, () => ({ client_id: 'desktop', client_name: 'KeepMeAlive' }));
  assert.equal(valid.elements.accept!.disabled, false);
  assert.match(valid.elements.details!.textContent, /Callback: http:\/\/127.0.0.1:49152\/callback\//);
});

test('account switching signs out before resuming the exact signed login request', async () => {
  const page = await render('/consent', '?' + query, () => ({ client_id: 'desktop', client_name: 'KeepMeAlive' }));
  await page.elements['switch-account']!.onclick!();
  assert.equal(page.requests.at(-1)!.route, '/sign-out');
  assert.deepEqual(page.requests.at(-1)!.body, {});
  assert.equal(page.destinations.length, 1);
  const destination = new URL(page.destinations[0]!);
  assert.equal(destination.origin, origin);
  assert.equal(destination.pathname, '/login');
  assert.equal(readOAuthQuery(destination.search), query);
});

test('failed browser sign-out keeps the consent page and restores safe button state', async () => {
  const page = await render('/consent', '?' + query, (route) => route === '/sign-out' ? new Error('Sign-out unavailable') : { client_id: 'desktop', client_name: 'KeepMeAlive' });
  await page.elements['switch-account']!.onclick!();
  assert.equal(page.destinations.length, 0);
  assert.equal(page.elements.status!.textContent, 'Sign-out unavailable');
  assert.equal(page.elements.accept!.disabled, false);
  assert.equal(page.elements.deny!.disabled, false);
  assert.equal(page.elements['switch-account']!.disabled, false);
});
