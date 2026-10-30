import { readFile, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import { resolve } from 'node:path';
import { resolveTarget } from './targets.mjs';

export async function configureAuth(config, target, request, branchMetadata) {
  const branch = config[target];
  if (!branch?.branchId || !branch.apiBaseUrl || !branch.issuer) { throw new Error('The target branch configuration is incomplete.'); }
  if (branchMetadata?.id !== branch.branchId) { throw new Error('Fresh metadata for the intended branch is required before configuring OAuth.'); }
  const origin = new URL(branch.apiBaseUrl).origin;
  const current = await request('/plugins/oauth-provider');
  if (branchMetadata.parent_id && current.enabled !== true) {
    throw new Error('OAuth Provider is disabled on this child branch. Neon only permits enabling it on a project root branch. Configure the root first and verify an inherited branch configuration; no settings were changed.');
  }
  const resources = [...new Set([...(current.resources ?? []).map((item) => item.identifier), config.resource])].map((identifier) => ({ identifier }));
  await request('/plugins/oauth-provider', 'PATCH', {
    ...(!branchMetadata.parent_id ? { enabled: true } : {}), login_page: new URL('login', branch.apiBaseUrl).href,
    consent_page: new URL('consent', branch.apiBaseUrl).href, resources,
  });
  const domains = await request('/domains');
  if (!Array.isArray(domains.domains)) { throw new Error('The trusted-domain API returned an unexpected response. No origin was added.'); }
  if (!domains.domains.some((item) => item.domain === origin)) {
    await request('/domains', 'POST', { domain: origin, auth_provider: 'better_auth' });
  }
  await request('/allow_localhost', 'PATCH', { allow_localhost: true });
  await request('/config', 'PATCH', { name: 'KeepMeAlive' });
  const existing = await request('/oauth-provider/clients');
  const clients = Array.isArray(existing) ? existing : existing.clients ?? [];
  const matching = clients.filter((entry) => branch.clientId ? entry.client_id === branch.clientId : entry.name === 'KeepMeAlive desktop' && entry.type === 'public');
  if (matching.length > 1) { throw new Error('Multiple matching clients exist. Record the intended public client ID before configuring.'); }
  let client = matching[0];
  const policy = {
    name: 'KeepMeAlive desktop', skip_consent: false,
    redirect_uris: [49152, 49153, 49154].map((port) => `http://127.0.0.1:${port}/callback/`),
    resources: [config.resource],
  };
  if (client) {
    if (client.type !== 'public') { throw new Error('The configured desktop client must be public and have no secret.'); }
    client = await request('/oauth-provider/clients/' + encodeURIComponent(client.client_id), 'PATCH', { ...policy, disabled: false });
  } else {
    client = await request('/oauth-provider/clients', 'POST', { ...policy, type: 'public' });
  }
  if (!client.client_id) { throw new Error('The response did not include a public client ID. Inspect the Neon Console.'); }
  branch.clientId = client.client_id;
  return { apiBaseUrl: branch.apiBaseUrl, authIssuer: branch.issuer, resource: config.resource, clientId: client.client_id };
}

async function main() {
  const target = process.argv[2];
  const apiKey = process.env.NEON_API_KEY;
  if (!apiKey) { throw new Error('NEON_API_KEY is required. Do not save it in source control.'); }
  const config = JSON.parse(await readFile(new URL('../neon.json', import.meta.url), 'utf8'));
  const { branch, projectId } = resolveTarget(config, target);
  const branchRoot = `https://console.neon.tech/api/v2/projects/${projectId}/branches/${branch.branchId}`;
  const root = branchRoot + '/auth';
  async function request(path, method = 'GET', body, base = root) {
    const response = await fetch(base + path, { method, headers: { Authorization: `Bearer ${apiKey}`, 'Content-Type': 'application/json' }, body: body ? JSON.stringify(body) : undefined, signal: AbortSignal.timeout(30000), redirect: 'error' });
    if (!response.ok) {
      const rootHint = path === '/plugins/oauth-provider' && method === 'PATCH' ? ' OAuth Provider can only be enabled on a project root branch; inspect the provider configuration before retrying.' : '';
      throw new Error(`Neon configuration ${method} ${path} failed with HTTP ${response.status}.${rootHint}`);
    }
    const text = await response.text();
    return text ? JSON.parse(text) : {};
  }
  const branchDetails = await request('', 'GET', undefined, branchRoot);
  const settings = await configureAuth(config, target, request, branchDetails.branch);
  await writeFile(new URL('../neon.json', import.meta.url), JSON.stringify(config, null, 2) + '\n');
  await writeFile(new URL(`../../KeepMeAlive/neon.${target}.json`, import.meta.url), JSON.stringify(settings, null, 2) + '\n');
  if (process.argv.includes('--activate')) {
    await writeFile(new URL('../../KeepMeAlive/neon.json', import.meta.url), JSON.stringify(settings, null, 2) + '\n');
  }
  console.log(`Configured ${target} public OAuth client and exact callback origins. SMTP and browser verification remain required. No readiness gate was changed.`);
}

if (process.argv[1] && fileURLToPath(import.meta.url) === resolve(process.argv[1])) { await main(); }
