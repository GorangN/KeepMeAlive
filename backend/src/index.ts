import { Pool } from 'pg';
import { attachDatabasePool } from '@neon/functions';
import { createRemoteJWKSet } from 'jose';
import { createTokenVerifier } from './auth.js';
import { createApp } from './app.js';
import { PostgresAccountRepository } from './repository.js';
import { authPage } from './pages.js';

const issuer = process.env.NEON_AUTH_BASE_URL;
const audience = process.env.KEEP_ME_ALIVE_RESOURCE;
const databaseUrl = process.env.KEEP_ME_ALIVE_DATABASE_URL;
if (!issuer || !audience || !databaseUrl) { throw new Error('Account service configuration is incomplete'); }
const pool = new Pool({ connectionString: databaseUrl, max: 5, connectionTimeoutMillis: 5000, statement_timeout: 10000 });
attachDatabasePool(pool);
const jwks = createRemoteJWKSet(new URL(`${issuer}/.well-known/jwks.json`));
const app = createApp(new PostgresAccountRepository(pool), createTokenVerifier(issuer, audience, jwks));
for (const path of ['/login', '/register', '/reset-password', '/verify-email', '/consent']) {
  app.get(path, () => authPage(path, issuer));
}
export default app;
