import { Hono } from 'hono';
import { bodyLimit } from 'hono/body-limit';
import { secureHeaders } from 'hono/secure-headers';
import type { JWTPayload } from 'jose';

export interface Account {
  userId: string;
  displayName: string;
  email: string;
  entitlements: { feature: string; expiresAt: string | null }[];
}

export interface AccountRepository {
  getAccount(userId: string): Promise<Account>;
  updateProfile(userId: string, displayName: string): Promise<Account>;
}

export function createApp(repository: AccountRepository, verify: (token: string) => Promise<JWTPayload>) {
  const app = new Hono<{ Variables: { userId: string } }>();
  const requests = new Map<string, { count: number; resetAt: number }>();
  app.use('*', secureHeaders());
  app.use('*', async (c, next) => {
    c.header('Cache-Control', 'no-store');
    c.header('Referrer-Policy', 'no-referrer');
    await next();
  });
  app.get('/healthz', (c) => c.json({ status: 'ok', service: 'keepmealive', version: '1.0.0' }));
  app.get('/readyz', (c) => c.json({ status: 'ok', service: 'keepmealive', version: '1.0.0' }));
  app.use('/v1/*', async (c, next) => {
    const authorization = c.req.header('Authorization');
    if (!authorization?.startsWith('Bearer ') || authorization.length > 16384) {
      return c.json({ error: 'unauthorized' }, 401);
    }
    let payload: JWTPayload;
    try {
      payload = await verify(authorization.slice(7));
      if (!payload.sub || !/^[0-9a-f-]{36}$/i.test(payload.sub)) {
        return c.json({ error: 'unauthorized' }, 401);
      }
    } catch {
      return c.json({ error: 'unauthorized' }, 401);
    }
    const now = Date.now();
    if (requests.size > 10000) {
      for (const [key, value] of requests) {
        if (value.resetAt <= now) { requests.delete(key); }
      }
      if (requests.size > 10000) { return c.json({ error: 'busy' }, 503); }
    }
    const previous = requests.get(payload.sub);
    const current = previous && previous.resetAt > now ? previous : { count: 0, resetAt: now + 60000 };
    current.count++;
    requests.set(payload.sub, current);
    if (current.count > 60) {
      c.header('Retry-After', String(Math.ceil((current.resetAt - now) / 1000)));
      return c.json({ error: 'rate_limited' }, 429);
    }
    c.set('userId', payload.sub);
    await next();
  });
  app.use('/v1/*', bodyLimit({ maxSize: 4096, onError: (c) => c.json({ error: 'request_too_large' }, 413) }));
  app.get('/v1/account', async (c) => c.json(await repository.getAccount(c.get('userId'))));
  app.patch('/v1/profile', async (c) => {
    if (!c.req.header('Content-Type')?.startsWith('application/json')) {
      return c.json({ error: 'content_type_required' }, 415);
    }
    let body: unknown;
    try { body = await c.req.json(); } catch { return c.json({ error: 'invalid_json' }, 400); }
    if (!body || typeof body !== 'object' || Array.isArray(body) ||
      Object.keys(body).some((key) => key !== 'displayName') ||
      !('displayName' in body) || typeof body.displayName !== 'string') {
      return c.json({ error: 'invalid_profile' }, 400);
    }
    const displayName = body.displayName.trim();
    if (!displayName || displayName.length > 100 || /[\x00-\x1f\x7f]/.test(displayName)) {
      return c.json({ error: 'invalid_display_name' }, 400);
    }
    return c.json(await repository.updateProfile(c.get('userId'), displayName));
  });
  app.notFound((c) => c.json({ error: 'not_found' }, 404));
  app.onError((_error, c) => {
    console.error(JSON.stringify({ event: 'request_failed', requestId: crypto.randomUUID() }));
    return c.json({ error: 'service_unavailable' }, 503);
  });
  return app;
}
