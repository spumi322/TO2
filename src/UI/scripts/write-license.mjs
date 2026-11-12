// Writes src/environments/primeng-license.ts (gitignored) before start/build.
// Source: the PRIMENG_LICENSE env var (CI/Vercel). Without it an existing local file is kept;
// with neither, an empty key is written and PrimeNG shows its license notice.
import { existsSync, writeFileSync } from 'node:fs';

const file = new URL('../src/environments/primeng-license.ts', import.meta.url);
const key = process.env.PRIMENG_LICENSE;

if (key) {
  writeFileSync(file, `export const PRIMENG_LICENSE = ${JSON.stringify(key.trim())};\n`);
} else if (!existsSync(file)) {
  writeFileSync(file, `export const PRIMENG_LICENSE = '';\n`);
}
