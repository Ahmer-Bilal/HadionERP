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
The single source of truth for color, spacing, typography, radius, elevation, and motion — all `--pi-*` CSS
custom properties. Consumed by Platform.UI's own components AND by Apps.Shell's app-level CSS. Theme-able:
light/dark today (via `@media (prefers-color-scheme)`), per-tenant branding later by redefining the same
variables.

**Color identity (2026-07-15 UI/Visual Density Pass)**: deliberately not blue. The palette before this pass
was, unintentionally, an exact copy of GitHub Primer's colors — and blue is also SAP Fiori's and Dynamics
365's default accent, so a blue HadionERP would read as "yet another enterprise app." Moved to a **deep teal
accent** (`--pi-accent`) with teal-tinted cool neutrals, keeping conventional green/red/amber for
success/danger/warning (status colors need instant recognition; the creative budget went into the identity
color, not the semantic vocabulary). See `project_visual_identity_decisions` in the project's memory for the
full reasoning and before/after screenshots context.

### Fonts (`fonts/fonts.css`)
Inter (Latin) and Noto Sans Arabic, self-hosted (the `.woff2` files live in this folder) rather than
linked from Google Fonts' CDN — this app is meant to run self-hosted/on-premises, so it shouldn't depend
on an external network call just to render text. Both are variable fonts covering the 400–600 weight range
`--pi-font-family` needs, restricted to the latin+arabic subsets since `SupportedLanguageCode` is only
`en`/`ar` (no cyrillic/greek/vietnamese ever renders). `--pi-font-family` lists both faces together, not
switched per-language, because a single screen can mix scripts (an English document number next to an
Arabic partner name); the browser picks whichever face covers each character via its `unicode-range`.

Vite's dev server needed an explicit `server.fs.allow` entry for Platform.UI in `vite.config.ts` — CSS
pulled in via a JS `import` (like `design-tokens.css`) is transformed/inlined by Vite's own pipeline and
never hits the filesystem-allow check, but a plain `url()` reference inside CSS (`fonts.css`'s
`@font-face src`) is fetched as a raw static file, which Vite refuses to serve from outside its project
root by default. Since Platform.UI lives outside Apps.Shell's root (the same reason the `@platform/ui`
alias exists at all), it had to be allow-listed explicitly — found by an actual 403 in the browser console
during live verification, not by code review.

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
- **SplitView** (added 2026-07-15) — the List+Details form template §2.1 called for, deliberately built
  differently from both reference products: the list pane stays visible while the detail pane slides in
  beside it (a CSS `@keyframes` entrance, RTL-aware via `--pi-slide-distance`), rather than replacing the
  list the way Dynamics 365 and Fiori both do on drill-down. Closer to a mail client's master-detail — a
  user triaging many records never loses their place in the list. The detail pane renders as a "crystal"
  material: translucent + `backdrop-filter: blur()` + a soft inner top highlight (`--pi-glass-highlight`),
  not the flat opaque card either reference product uses — needs `.app-shell__content`'s subtle
  accent-tinted background wash (Apps.Shell's `App.css`) behind it to actually read as glass. Reference
  usage: `PurchaseOrdersPage.tsx` and `BusinessPartnersPage.tsx`.
- **Dense list table** (`.pi-dense-table`, `.pi-link` in `components.css`) — compact rows, a key/ID column
  rendered as a real link in the product's own accent color (the "blue vendor ID" Dynamics 365 pattern,
  reworked in HadionERP's own color rather than literal blue), a selected-row indicator bar that flips sides
  correctly under RTL. Scoped by element/class, not a required markup change, so retrofitting an existing
  `bp-table`-style page is a class-name swap.

### Bilingual & RTL
Every component is authored with CSS logical properties (`inline-start`/`inline-end`, not `left`/`right`),
so flipping `dir="rtl"` for Arabic requires zero component-level changes. Arabic is a first-class layout
direction, not a mirrored afterthought. This includes the FastTab chevron: it's a directional disclosure
arrow built with `border-inline-start` (a logical property), so it auto-mirrors — collapsed it points toward
the content's start side (right in LTR, left in RTL), and expanded it rotates to point down in both
directions. No manual RTL overrides.

## Deferred
- **Workspace component** (§2.2) — role-based landing page with KPI tiles + embedded lists. `HomePage.tsx`
  fills this role today with hand-rolled tiles, not yet a shared Platform.UI component.
- **Retrofitting every remaining page to SplitView** — only `PurchaseOrdersPage.tsx` and
  `BusinessPartnersPage.tsx` are converted so far (the UI/Visual Density Pass's proof-of-concept slice,
  2026-07-15); every other module's list/details pages (GL Accounts, Items, Cost Centers, Tax Codes, Journal
  Entries, AP Invoices, Vendor Prequalification, Purchase Requisitions, RFQs, GRNs) still use the older
  three-state `list | create | details` full-page-swap pattern and inherit the new color tokens automatically
  but not the split-view/dense-table/glass-panel treatment yet.
- **Real Edit action** — no BO in this app has an "Edit" Action Pane button yet (Procurement documents have
  no update endpoint at all beyond lifecycle transitions; MasterData entities have an `Update` API but no
  frontend Edit button wired to it). Flagged as a real gap during the UI pass; not fixed in this slice.
- **DocumentChain as a real shared component** — `PurchaseOrdersPage.tsx` has a one-off `.pi-doc-chain`
  styled paragraph for "Source RFQ: ..."; promoting it to a real Platform.UI component (with multiple links,
  not just one) is deferred until a second page needs a multi-hop chain (GRN → PO → RFQ → PR).
- **Accessibility CI checks** (§4) — WCAG 2.1 AA automated enforcement (axe-core etc.) not wired into CI yet;
  components are authored to the standard but not yet verified by an automated scanner.
