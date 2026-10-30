import { createHash } from 'node:crypto';
import { cp, mkdir, readFile, writeFile } from 'node:fs/promises';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { buildFunctionBundle } from '@neon/config-runtime/v1';

const restoredBundleHash = '5315e56f81a34d47610f0c12b2b71b6fb30f543c02975df5e52d069e1ad93003';
const repositoryMarker = 'new PostgresAccountRepository(pool)';

export function createOutageRepository() {
  return {
    async getAccount() { throw new Error('Controlled staging account outage'); },
    async updateProfile() { throw new Error('Controlled staging profile outage'); },
  };
}

export function createNativeOutageEntry(config, source, confirmed) {
  if (!confirmed) { throw new Error('Explicit --staging-outage-validation confirmation is required.'); }
  const staging = config.staging;
  if (config.function !== 'keepmealive' || config.resource !== 'https://keepmealive.app/api' ||
      staging?.projectId !== 'sweet-truth-01118709' || staging.branchId !== 'br-red-flower-b9wmbkmb' ||
      staging.issuer !== 'https://ep-red-rain-b9c8tumi.neonauth.c-7.eu-central-1.aws.neon.tech/neondb/auth' ||
      staging.apiBaseUrl !== 'https://br-red-flower-b9wmbkmb-keepmealive.compute.c-7.eu-central-1.aws.neon.tech/') {
    throw new Error('Only the pinned isolated KeepMeAlive staging root is allowed.');
  }
  if (source.split(repositoryMarker).length !== 2) {
    throw new Error('Expected exactly one account repository marker; no files were written.');
  }
  return source.replace(repositoryMarker, `(${createOutageRepository.toString()})()`);
}

export async function buildNativeOutage(backendDirectory, confirmed) {
  if (!confirmed) { throw new Error('Explicit --staging-outage-validation confirmation is required.'); }
  const root = resolve(backendDirectory);
  const config = JSON.parse(await readFile(resolve(root, 'neon.json'), 'utf8'));
  const originalSource = await readFile(resolve(root, 'src/index.ts'), 'utf8');
  const outageSource = createNativeOutageEntry(config, originalSource, confirmed);
  const restoreZip = await readFile(resolve(root, 'dist/function.zip'));
  const sha256 = (bytes) => createHash('sha256').update(bytes).digest('hex');
  if (sha256(restoreZip) !== restoredBundleHash) {
    throw new Error('The retained bundle does not match verified staging deployment 2; no files were written.');
  }
  const output = resolve(root, 'dist/native-outage');
  const restorePath = resolve(output, 'restore.zip');
  try {
    if (sha256(await readFile(restorePath)) !== restoredBundleHash) {
      throw new Error('An existing restore.zip differs; preserve it and resolve the mismatch before continuing.');
    }
  } catch (error) {
    if (error.code !== 'ENOENT') { throw error; }
  }
  await mkdir(output, { recursive: true });
  try { await writeFile(restorePath, restoreZip, { flag: 'wx' }); }
  catch (error) { if (error.code !== 'EEXIST') { throw error; } }
  if (sha256(await readFile(restorePath)) !== restoredBundleHash) {
    throw new Error('The recovery bundle changed during preparation; no outage bundle was built.');
  }
  const sourceDirectory = resolve(output, 'source');
  await cp(resolve(root, 'src'), sourceDirectory, { recursive: true });
  await writeFile(resolve(sourceDirectory, 'index.ts'), outageSource);
  const faultyZip = await buildFunctionBundle({
    slug: 'keepmealive', name: 'KeepMeAlive API', source: resolve(sourceDirectory, 'index.ts'), env: {}, runtime: 'nodejs24',
  });
  const faultyPath = resolve(output, 'faulty.zip');
  await writeFile(faultyPath, faultyZip);
  return {
    target: 'staging-only', projectId: config.staging.projectId, branchId: config.staging.branchId,
    restorePath, restoreSha256: restoredBundleHash, faultyPath, faultySha256: sha256(faultyZip),
    productionSourceChanged: false, productionBundleChanged: false, deploymentPerformed: false,
  };
}

if (process.argv[1] && fileURLToPath(import.meta.url) === resolve(process.argv[1])) {
  const backendDirectory = fileURLToPath(new URL('..', import.meta.url));
  console.log(JSON.stringify(await buildNativeOutage(backendDirectory, process.argv.includes('--staging-outage-validation'))));
}
