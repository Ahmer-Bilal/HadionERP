# Project Instructions

This repository is **HadionERP**, by **hAdisHere**, created by **aHmAr** — an enterprise ERP platform for
construction and finance companies in Saudi Arabia, architecturally inspired by SAP S/4HANA
(financial/project accounting architecture) and Microsoft Dynamics 365 F&O (UI, navigation, usability).

**This is a multi-session, multi-agent project** — different AI tools (Claude, Codex, others) and humans
work on it over time with no shared memory except what's written to disk. Before doing anything else:

1. Read `PROGRESS.md` — the append-only log of what's been done, by whom, and what's next. Read its Phase
   Status Summary and most recent entries before starting work.
2. After finishing a unit of work, add an entry to `PROGRESS.md` following its template — this is not
   optional, it's how the next session (yours or another tool's) knows the real state of the project.

See `AGENTS.md` for the same rules in tool-agnostic form (that's the file other AI tools will read),
including the standing "the application must always run" rule added 2026-07-13 — every phase must leave
`src/Gateway/Gateway.Api` (backend) and `src/Apps/Apps.Shell` (frontend) compiling and startable, verified
before reporting a phase done, not just covered by the test suite.

**Before writing any code in this repo, read `ARCHITECTURE.md` and the linked docs in
`docs/architecture/`.** They define the layered architecture, module boundaries, the Business Object model
and Object Page standard, localization/security/workflow/audit as platform services, database and API
conventions, naming conventions, and the extension model. Do not:

- Add business logic to the Domain layer of one module that reaches into another module's Domain or
  Infrastructure layer directly — cross-module calls go through published `Contracts` packages or events
  (see `docs/architecture/01-architecture-foundation.md` §3).
- Hard-code business rules (approval thresholds, tax rates, posting rules) that should be configuration
  (see `docs/architecture/04-data-and-api.md` §3).
- Hard-delete any record that has left Draft status — use the reversal pattern
  (see `docs/architecture/02-business-object-model.md` §1.1).
- Build a one-off screen instead of using the List Report / Object Page templates
  (see `docs/architecture/02-business-object-model.md` §2) without flagging it as an architecture exception.
- Implement localization, security, workflow, or audit logic inside a business module — these are platform
  services consumed from `src/Platform/*`.

Current phase status is tracked in `PROGRESS.md`, not here — check it rather than assuming from this file,
since this file is not updated per-phase.

To actually run the application (backend `src/Gateway/Gateway.Api` + frontend `src/Apps/Apps.Shell`), see
`HOW-TO-RUN.md`.
