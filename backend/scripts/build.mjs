import { buildFunctionBundle } from '@neon/config-runtime/v1';
import { mkdir, writeFile } from 'node:fs/promises';
const zip = await buildFunctionBundle({ slug: 'keepmealive', name: 'KeepMeAlive API', source: './src/index.ts', env: {}, runtime: 'nodejs24' });
await mkdir('dist', { recursive: true });
await writeFile('dist/function.zip', zip);
console.log('Created dist/function.zip');
