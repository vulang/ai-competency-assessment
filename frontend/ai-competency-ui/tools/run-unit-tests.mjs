import { build } from 'esbuild';
import { mkdir, rm } from 'node:fs/promises';
import { spawnSync } from 'node:child_process';

const output = '.cache/unit-tests/tests.mjs';
await mkdir('.cache/unit-tests', { recursive: true });
try {
  await build({
    entryPoints: ['tests/phase-0.test.ts'],
    outfile: output,
    bundle: true,
    platform: 'node',
    format: 'esm',
    packages: 'external',
    sourcemap: 'inline',
    tsconfig: 'tsconfig.json'
  });
  const result = spawnSync(process.execPath, ['--test', output], { stdio: 'inherit' });
  process.exitCode = result.status ?? 1;
} finally {
  await rm('.cache/unit-tests', { recursive: true, force: true });
}
