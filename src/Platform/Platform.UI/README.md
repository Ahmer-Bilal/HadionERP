# Platform.UI

The in-house design system: design tokens + reusable Dynamics-365-referenced components, built so
bilingual/RTL and record-form behavior is consistent everywhere (docs/architecture/02-business-object-model.md
#4). This is NOT a grab-bag of third-party components — it is the single source of truth for the platform's
visual language.

## Current stage: Vite alias (not yet a real npm package)

Platform.UI lives here as plain `.ts/.tsx/.css` files. Apps.Shell consumes it via a `@platform/ui` import
path, resolved by:
- `tsconfig.app.json` `paths` mapping (TypeScript type-checking)
- `vite.config.ts` `resolve.alias` (Vite bundler/dev server)

**Hard rule: Platform.UI never imports anything from Apps.Shell.** It is self-contained — no dependency on
any app's i18n, routing, or state. This is what makes promoting it to a real npm workspace package later a
config change, not a rewrite: consumers switch their alias to a package dependency and nothing else changes.

### What "promoting to a real npm package" would involve later
1. Add a `package.json` here (name `@platform/ui`, with its own `build` script producing a dist bundle).
2. Set up npm workspaces at the repo root so Apps.Shell lists `@platform/ui` as a dependency.
3. Replace the Vite alias + tsconfig path with a normal package import (the `@platform/ui` import path stays
   the same — only the resolution mechanism changes).
4. Add a build step to the CI pipeline that builds Platform.UI before apps that consume it.

No consumer code changes are required — that's the point of keeping Platform.UI self-contained now.

## What's built

### Design tokens (`tokens/design-tokens.css`)
The single source of truth for color, spacing, typography, and radius — all `--pi-*` CSS custom properties.
Consumed by Platform.UI's own components AND by Apps.Shell's app-level CSS. Theme-able: light/dark today
(via `@media (prefers-color-scheme)`), per-tenant branding later by redefining the same variables.

### Components (`components/`)
- **ShellBar** — top bar: title + language switcher. Receives translated title and language autonyms as
  props (never calls a translation function itself).
- **NavigationPane** — persistent left nav: Modules -> Areas -> menu items
  (docs/architecture/02-business-object-model.md #3). Data-driven: the app passes the full nav tree as
  props; a new business module adds its entry as data, not a new component.
- **ActionPane** — the Dynamics 365 command bar (§2.1). Stateless: the app passes only the actions
  available right now (driven by the document's FSM state + the user's security role). A new BO transition
  surfaces its button everywhere automatically because no screen hardcodes its action list.
- **FastTabs** — vertically stacked, collapsible panels where several can be open at once (§2.1: "FastTabs,
  not tabs"). Real `<button>` headers with `aria-expanded`/`aria-controls` for keyboard + screen-reader
  access (WCAG 2.1 AA, per doc 02 #4).

### Bilingual & RTL
Every component is authored with CSS logical properties (`inline-start`/`inline-end`, not `left`/`right`),
so flipping `dir="rtl"` for Arabic requires zero component-level changes. Arabic is a first-class layout
direction, not a mirrored afterthought. This includes the FastTab chevron: it's a directional disclosure
arrow built with `border-inline-start` (a logical property), so it auto-mirrors — collapsed it points toward
the content's start side (right in LTR, left in RTL), and expanded it rotates to point down in both
directions. No manual RTL overrides.

## Deferred
- **Workspace component** (§2.2) — role-based landing page with KPI tiles + embedded lists. Needs business
  modules with meaningful KPIs/lists; the System Status page fills this role for now.
- **List+Details form template** (§2.1) — the merged grid+record view. Needs a real business object to
  render; built when Phase 1's first module lands.
- **Accessibility CI checks** (§4) — WCAG 2.1 AA automated enforcement (axe-core etc.) not wired into CI yet;
  components are authored to the standard but not yet verified by an automated scanner.
