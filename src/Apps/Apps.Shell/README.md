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

Same rule as the backend (see `src/Platform/Platform.Localization/README.md`): no component embeds a
literal display string. All display text lives in `src/i18n/content.ts` (translatable content) or
`src/i18n/languageNames.ts` (fixed language autonyms), looked up via `t(key, language)`. Not yet covered
by an automated test the way the backend is (`tests/ArchitectureTests`) — that guardrail is C#/Roslyn-
specific; extending an equivalent check to the frontend is a known follow-up, not yet done.
