import { readFile } from 'node:fs/promises';
import pg from 'pg';

const connectionString = process.env.MIGRATION_DATABASE_URL;
if (!connectionString) { throw new Error('MIGRATION_DATABASE_URL is required for an owner connection to the chosen branch.'); }
const client = new pg.Client({ connectionString });
await client.connect();
try {
  await client.query('BEGIN');
  await client.query("SELECT pg_advisory_xact_lock(hashtext('keepmealive-migrations'))");
  const installed = await client.query("SELECT to_regclass('keepmealive.schema_migrations') IS NOT NULL AS installed");
  let version = 0;
  if (installed.rows[0].installed) {
    const result = await client.query('SELECT COALESCE(max(version),0) AS version FROM keepmealive.schema_migrations');
    version = result.rows[0].version;
  }
  if (version < 1) {
    const role = await client.query("SELECT rolname FROM pg_roles WHERE rolname = 'keepmealive_api' AND NOT rolbypassrls AND NOT rolcreaterole AND NOT rolcreatedb");
    if (!role.rowCount) { throw new Error('Create the restricted keepmealive_api login first.'); }
    await client.query(await readFile(new URL('../migrations/001_accounts.sql', import.meta.url), 'utf8'));
  }
  await client.query('COMMIT');
  console.log('Account schema is current.');
} catch (error) {
  await client.query('ROLLBACK');
  console.error('Migration failed; the transaction was rolled back. Inspect database permissions and migration version.');
  process.exitCode = 1;
} finally { await client.end(); }
