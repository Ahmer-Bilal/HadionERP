# Apps.Shell

App shell: shell bar, navigation pane, language switching — see
docs/architecture/02-business-object-model.md #2-3. Built on Vite + React + TypeScript per ADR-3.

Currently shows a System Status page (`src/pages/SystemStatusPage.tsx`) reading from Gateway.Api's
`/health`, `/api/v1/system/status`, and `/api/v1/system/greeting` endpoints — this is the Phase 0 proof
that backend, frontend, and localization work together end to end in a browser. Business-module pages
(Finance, Procurement, etc.) get added under `src/pages/` and registered in `NavigationPane.tsx` as those
modules are built, extending this same shell rather than replacing it.

## Running locally

```
npm install
npm run dev
```

Opens on http://localhost:5173 by default. Requires `Gateway.Api` running on http://localhost:5210 (see
`src/config.ts` to change) — CORS is already configured on the backend for this origin.

## Text/translation discipline

Same rule as the backend (see `docs/architecture/04-platform-services.md`'s Localization section): no component embeds a
literal display string. All display text lives in `src/i18n/content.ts` (translatable content) or
`src/i18n/languageNames.ts` (fixed language autonyms), looked up via `t(key, language)`.

Enforced automatically: `npm run check:no-hardcoded-arabic` parses every `.ts`/`.tsx` file with the
TypeScript compiler API and fails if any Arabic-script string/template/JSX-text literal exists outside
`scripts/check-no-hardcoded-arabic.mjs`'s `ALLOWED_FILES` list. Added 2026-07-13 as the frontend
equivalent of `tests/ArchitectureTests` (that one is C#/Roslyn-specific and doesn't cover this app).
