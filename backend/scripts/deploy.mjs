import { readFile } from 'node:fs/promises';
import { resolveTarget } from './targets.mjs';

const target = process.argv[2];
const config = JSON.parse(await readFile(new URL('../neon.json', import.meta.url), 'utf8'));
const { branch, projectId } = resolveTarget(config, target);
if (target === 'production' && !config.accountReleaseReady) {
  throw new Error('Production account release is gated. Complete and record the live authentication, SMTP and isolation checks first.');
}
const apiKey = process.env.NEON_API_KEY;
const databaseUrl = process.env.KEEP_ME_ALIVE_DATABASE_URL;
if (!apiKey || !databaseUrl) { throw new Error('Set NEON_API_KEY and KEEP_ME_ALIVE_DATABASE_URL in the deployment environment.'); }
if (new URL(databaseUrl).username !== 'keepmealive_api') { throw new Error('The Function requires the restricted keepmealive_api login.'); }
const root = `https://console.neon.tech/api/v2/projects/${projectId}/branches/${branch.branchId}/functions/${config.function}`;
const zip = await readFile(new URL('../dist/function.zip', import.meta.url));
const form = new FormData();
form.set('zip', new Blob([zip], { type: 'application/zip' }), 'function.zip');
form.set('runtime', 'nodejs24');
form.set('environment', JSON.stringify({ KEEP_ME_ALIVE_DATABASE_URL: databaseUrl, KEEP_ME_ALIVE_RESOURCE: config.resource, NEON_AUTH_BASE_URL: branch.issuer }));
const response = await fetch(root + '/deployments', { method: 'POST', headers: { Authorization: `Bearer ${apiKey}` }, body: form });
if (!response.ok) { throw new Error(`Deploy failed with HTTP ${response.status}.`); }
const deadline = Date.now() + 150000;
while (Date.now() < deadline) {
  await new Promise((resolve) => setTimeout(resolve, 3000));
  const statusResponse = await fetch(root, { headers: { Authorization: `Bearer ${apiKey}` } });
  if (!statusResponse.ok) { throw new Error(`Deployment status failed with HTTP ${statusResponse.status}.`); }
  const deployed = await statusResponse.json();
  const status = deployed.current_deployment?.status;
  if (status === 'failed') { throw new Error('The Function deployment failed; inspect Neon logs.'); }
  if (status === 'completed') { console.log(`Deployed ${target}: ${deployed.invocation_url}`); process.exit(0); }
}
throw new Error('Deployment is still pending. Check Neon before retrying.');
