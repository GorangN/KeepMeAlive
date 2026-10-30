CREATE SCHEMA IF NOT EXISTS keepmealive;
CREATE TABLE IF NOT EXISTS keepmealive.schema_migrations(version integer PRIMARY KEY, applied_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS keepmealive.profiles(
  user_id uuid PRIMARY KEY REFERENCES neon_auth."user"(id) ON DELETE CASCADE,
  display_name text NOT NULL CHECK (length(display_name) BETWEEN 1 AND 100),
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS keepmealive.entitlements(
  user_id uuid NOT NULL REFERENCES neon_auth."user"(id) ON DELETE CASCADE,
  feature text NOT NULL,
  granted boolean NOT NULL DEFAULT false,
  expires_at timestamptz,
  PRIMARY KEY(user_id, feature)
);
ALTER TABLE keepmealive.profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE keepmealive.entitlements ENABLE ROW LEVEL SECURITY;
CREATE POLICY profiles_owner ON keepmealive.profiles TO keepmealive_api USING (user_id::text = current_setting('app.user_id', true)) WITH CHECK (user_id::text = current_setting('app.user_id', true));
CREATE POLICY entitlements_owner ON keepmealive.entitlements FOR SELECT TO keepmealive_api USING (user_id::text = current_setting('app.user_id', true));
GRANT USAGE ON SCHEMA keepmealive, neon_auth TO keepmealive_api;
GRANT SELECT, INSERT, UPDATE ON keepmealive.profiles TO keepmealive_api;
GRANT SELECT ON keepmealive.entitlements TO keepmealive_api;
GRANT SELECT(id, name, email) ON neon_auth."user" TO keepmealive_api;
INSERT INTO keepmealive.schema_migrations(version) VALUES (1) ON CONFLICT DO NOTHING;
