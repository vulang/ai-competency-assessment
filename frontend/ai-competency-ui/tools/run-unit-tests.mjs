import { build } from 'esbuild';
import { mkdir, readdir, rm } from 'node:fs/promises';
import { spawnSync } from 'node:child_process';

const outdir = '.cache/unit-tests';
await mkdir(outdir, { recursive: true });
try {
  const entryPoints = (await readdir('tests')).filter(name => name.endsWith('.test.ts')).map(name => `tests/${name}`);
  await build({
    entryPoints,
    outdir,
    outExtension: { '.js': '.mjs' },
    bundle: true,
    platform: 'node',
    format: 'esm',
    packages: 'external',
    sourcemap: 'inline',
    tsconfig: 'tsconfig.json'
  });
  const outputs = entryPoints.map(entry => `${outdir}/${entry.slice('tests/'.length).replace(/\.ts$/, '.mjs')}`);
  const result = spawnSync(process.execPath, ['--test', ...outputs], { stdio: 'inherit' });
  process.exitCode = result.status ?? 1;
} finally {
  await rm('.cache/unit-tests', { recursive: true, force: true });
}
