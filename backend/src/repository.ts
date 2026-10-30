import type { Pool, PoolClient } from 'pg';
import type { Account, AccountRepository } from './app.js';

export class PostgresAccountRepository implements AccountRepository {
  constructor(private readonly pool: Pool) {}

  private async read(client: PoolClient, userId: string): Promise<Account> {
    const result = await client.query<{ display_name: string; email: string }>(
      'SELECT COALESCE(p.display_name, u.name) AS display_name, u.email FROM neon_auth."user" u LEFT JOIN keepmealive.profiles p ON p.user_id = u.id WHERE u.id = $1', [userId]);
    const user = result.rows[0];
    if (!user) { throw new Error('Account missing'); }
    const grants = await client.query<{ feature: string; expires_at: Date | null }>(
      "SELECT feature, expires_at FROM keepmealive.entitlements WHERE user_id = $1 AND granted = TRUE AND (expires_at IS NULL OR expires_at > now()) ORDER BY feature", [userId]);
    return { userId, displayName: user.display_name, email: user.email,
      entitlements: grants.rows.map((row) => ({ feature: row.feature, expiresAt: row.expires_at?.toISOString() ?? null })) };
  }

  private async scoped<T>(userId: string, action: (client: PoolClient) => Promise<T>): Promise<T> {
    const client = await this.pool.connect();
    try {
      await client.query('BEGIN');
      await client.query("SELECT set_config('app.user_id', $1, true)", [userId]);
      const result = await action(client);
      await client.query('COMMIT');
      return result;
    } catch (error) {
      await client.query('ROLLBACK');
      throw error;
    } finally { client.release(); }
  }

  getAccount(userId: string) { return this.scoped(userId, (client) => this.read(client, userId)); }

  updateProfile(userId: string, displayName: string) {
    return this.scoped(userId, async (client) => {
      await client.query('INSERT INTO keepmealive.profiles(user_id, display_name) VALUES ($1,$2) ON CONFLICT (user_id) DO UPDATE SET display_name = EXCLUDED.display_name, updated_at = now()', [userId, displayName]);
      return this.read(client, userId);
    });
  }
}
