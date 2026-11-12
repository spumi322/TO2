# TO2 UI

Angular 22 (standalone, signals, zoneless) + PrimeNG 22. Talks to the TO2 API at `http://localhost:5161/api` in development.

## Commands

- `npm start`: dev server at `http://localhost:4200`
- `npm run build`: production build into `dist/`
- `npm test`: unit tests (Vitest)

`start`, `build` and `test` first write `src/environments/primeng-license.ts` from the env variable `PRIMENG_LICENSE` (empty = PrimeNG shows a license notice).

Adding packages: `npx npm@11 install ...` (npm 10 fails on the Vitest peer dependencies; `npm ci` works).
