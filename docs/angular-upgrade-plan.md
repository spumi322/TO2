# Angular upgrade plan (17 → 21)

Angular 17 is out of support and the remaining `npm audit` findings in shipped code (`@angular/core`/`common`/`compiler`/`router`: i18n XSS, XSRF token leak; `primeng`) are fixed only in newer majors. PrimeNG versions track the Angular major (PrimeNG 21 peer-requires Angular 21), so both move together.

## Impact assessment

Small component surface, moderate styling risk.

- PrimeNG components used in templates: `p-tabView`/`p-tabPanel` (tournament details), `p-button`/`pButton`, `p-menu`, `p-toast`, `p-dialog`, `p-progressSpinner`, `pTooltip`.
- `app.module.ts` imports many unused PrimeNG modules (Dropdown, InputTextarea, Messages, Steps, Listbox, Table, ...). Several no longer exist in PrimeNG 21 (`tabview`, `dropdown`, `inputtextarea`, `messages`) - unused ones are dropped, not migrated.
- Theming: PrimeNG 18+ has no theme CSS files. `angular.json` loads `primeng/resources/primeng.css` + `themes/lara-dark-green/theme.css`; replaced by `providePrimeNG({ theme: { preset } })` (Lara/Aura preset, green primary, dark mode on).
- `src/styles.css` (390 lines) overrides ~45 PrimeNG classes. Some were renamed or removed in v18+ (`.p-tabview-*`, `.p-dropdown-*`, `.p-menuitem-link`, `.p-highlight`), so look will shift until selectors are fixed.
- `TabView` → `Tabs` (`p-tabs`, `p-tablist`, `p-tab`, `p-tabpanels`, `p-tabpanel`): one template rewrite.
- Angular migrations (run by `ng update`): `standalone: false` added to NgModule components (v19 default change), `HttpClientModule` → `provideHttpClient()`. App stays NgModule-based.
- Node 22.13 meets Angular 21 (`^20.19 || ^22.12 || >=24`). TypeScript → 5.9, zone.js updated by `ng update`.
- `brackets-viewer` is loaded as a global script; not tied to Angular.

## Out of scope

- Standalone/zoneless/signals conversion, new control flow migration beyond what `ng update` does automatically.
- Fixing the 10 failing generated specs, Karma → Vitest switch.
- UI redesign; target is "looks about the same".

## Phase 0 - Baseline

- Short browser pass, screenshots: landing, tournament list, create tournament, details (overview/teams/groups/bracket tabs), ⋮ menu, confirm dialog, toast.

## Phase 1 - Angular 18 + PrimeNG 18 (the breaking step)

- `ng update @angular/core@18 @angular/cli@18`, then `primeng@18` + `@primeng/themes@18`.
- Remove theme CSS entries from `angular.json`; add `providePrimeNG` with Lara preset, green primary, dark mode.
- Drop unused PrimeNG modules from `app.module.ts`.
- `TabView` → `Tabs` in tournament details.
- Fix renamed CSS selectors in `styles.css`.
- `npm run build`, browser pass against baseline.

CONFIRM: pages look about the same, tabs/menu/dialog/toast work.

## Phase 2 - Angular 19 → 20 → 21 + PrimeNG 19 → 21

- One major at a time: `ng update @angular/core@N @angular/cli@N`, `primeng@N`; from 20: `@primeng/themes` → `@primeuix/themes`. Build after each.
- Browser pass at 21.

CONFIRM: build clean, same pages work.

## Phase 3 - Tooling and leftovers

- Karma/jasmine packages to versions Angular 21 accepts (specs stay as they are).
- `brackets-viewer` `uuid` advisory: npm `overrides` for `uuid` if brackets-manager works with it, else leave (moderate, buffer API not used).
- Final `npm audit`; check Vercel build Node version.

CONFIRM: audit residue listed and accepted.

## Stop and ask

- A page breaks functionally (not just spacing/colors) and the fix is more than selector/template edits.
- `brackets-viewer` rendering breaks.
- More than ~5 components need template rewrites.
- File writes fail repeatedly.

## Open questions

1. Preset: Lara (closest to current) or Aura (PrimeNG default)?
2. Phase 0 screenshots via Chrome ok (short, explicit)?
