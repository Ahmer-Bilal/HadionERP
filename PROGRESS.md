# Project Progress Log

This is the **single source of truth for "what has been done so far."** Any contributor — human or AI
(Claude, Codex, or any other tool) — must read this file before starting work, and append an entry after
finishing a unit of work. This file is never rewritten or reorganized to "clean it up" — like the platform's
own audit principle (`docs/architecture/03-platform-services.md` §5), it is **append-only**: newest entries
go at the top of the Entry Log, older entries are never edited or deleted.

## Rules for updating this file

1. **Read before you work**: check the Phase Status Summary + the most recent 3–5 entries so you don't
   redo or contradict work already done.
2. **Write after you work**: add one entry to the Entry Log (top of the list) for each meaningful unit of
   work — a completed task, a design decision, a correction, or a phase transition. Don't batch unrelated
   work into one entry.
3. **Never delete or rewrite past entries.** If something you did earlier turns out to be wrong, add a
   *new* entry that says so and links back to the entry it corrects — don't silently edit history.
4. **Update the Phase Status Summary table** whenever an entry changes a phase's status.
5. **Status values**: `Not Started` / `In Progress` / `Blocked` / `Completed`. Use `Blocked` with a reason
   if you stopped without finishing — the next agent (AI or human) needs to know why.
6. **Identify yourself**: name the agent/tool and model if known (e.g. "Claude Sonnet 5", "Codex", "Ahmer
   (human)") — this is a multi-agent, multi-session project by design, so attribution matters.

## Entry template

```
### {YYYY-MM-DD} — {short title}
- Agent: {who/what did this — name the AI tool/model, or the human}
- Phase: {Phase 0 / Phase 1 / ... / Architecture, from docs/architecture/06-roadmap.md}
- Status: {Not Started | In Progress | Blocked | Completed}
- What changed: {1-3 sentences, plain language}
- Files touched: {paths}
- Next: {what the next contributor should pick up, or "none — phase complete"}
```

---

## Phase Status Summary

| Phase | Status | Last Updated |
|---|---|---|
| Architecture Baseline | Completed | 2026-07-13 |
| Phase 0 — Platform Foundation | In Progress — application runnable; Platform.Core/Security/Localization/Workflow/Events done; Audit/full UI design system/Configuration remain | 2026-07-13 |
| Phase 1 — Master Data + Finance Core | Not Started | — |
| Phase 2 — Procurement | Not Started | — |
| Phase 3 — Construction & Project Management | Not Started | — |
| Phase 4 — HR & Payroll | Not Started | — |
| Phase 5 — Reporting, Analytics & Mobile | Not Started | — |
| Phase 6 — Extensibility Ecosystem & Advanced Capabilities | Not Started | — |

(Phase definitions and exit criteria: `docs/architecture/06-roadmap.md`)

---

## Entry Log (newest first)

### 2026-07-13 — Built Platform.Events (cross-module messaging), closed the frontend translation gap, fixed a display bug
- Agent: Claude Sonnet 5
- Phase: Phase 0 — Platform Foundation
- Status: Completed
- What changed:
  - **Built Platform.Events** — how different parts of the system will tell each other "something just
    happened" without being directly wired together. In plain terms: when Procurement approves a Purchase
    Order, Finance needs to know about it (to set aside budget) without Procurement's code needing to know
    Finance exists. This piece is that messaging system:
    - Events are staged first ("outbox pattern") before being sent out, so nothing is lost even if the
      messaging system is briefly unavailable — an event sits safely waiting rather than vanishing.
    - Only registered, named, versioned events can be sent — an ad-hoc, undocumented event can't slip
      through, which keeps the system's cross-module "contracts" deliberate and traceable.
    - Proved the entire pipeline (publish → hold safely → deliver → received) works, including inside the
      actual running application: it now publishes one real "application started" event at boot and logs
      that it received its own message, visible in the System Status page as a live published/pending count.
  - **Closed a real gap the user flagged**: after the backend's hardcoded-Arabic safety check was built
    two sessions ago, the frontend (website) side never got the same protection. The user asked again
    whether text was still being hardcoded — right to ask, since it hadn't been fixed yet. Built the
    frontend equivalent (an automated script scanning every frontend file), proved it actually catches a
    planted violation (twice — once for a normal case, once for text embedded directly in on-screen
    markup), then removed the test violations. Both the backend and frontend now have their own automated
    checks; before this, only the backend did.
  - **Found and fixed a real display bug while re-verifying**, not just re-confirming the change worked:
    the new published/pending counter displayed backwards in the Arabic view (showing "0 / 1" instead of
    "1 / 0") — a known quirk where plain numbers inside Arabic (right-to-left) text can get visually
    reordered by the browser. Fixed by isolating that number sequence so it always displays correctly
    regardless of language, and confirmed the fix with a new screenshot before moving on.
  - 13 new backend tests, 79 total passing across the whole project, plus a new, separately-run frontend
    check (not a counted "test" in the .NET sense, but enforced the same way — see its own README).
- Files touched: `src/Platform/Platform.Events/*` (full new project: IntegrationEvent, IEventCatalog,
  IEventBus, Outbox/*, IIntegrationEventPublisher), `tests/UnitTests/Platform.Events.Tests/*`,
  `src/Gateway/Gateway.Api/Program.cs` and `Controllers/SystemController.cs` and new `Events/GatewayApiEvents.cs`,
  `src/Apps/Apps.Shell/scripts/check-no-hardcoded-arabic.mjs` (new frontend guardrail),
  `src/Apps/Apps.Shell/package.json` (new npm script), `src/Apps/Apps.Shell/src/i18n/content.ts` (new
  label), `src/Apps/Apps.Shell/src/pages/SystemStatusPage.tsx` (new field + bidi display fix),
  `src/Platform/Platform.Events/README.md`, `src/Apps/Apps.Shell/README.md`
- Known gap, disclosed not hidden: the outbox relay runs once at startup only — no recurring background
  job yet (same deferred-scheduler pattern as Platform.Workflow's escalation check). RabbitMQ (the real
  messaging system per the architecture doc) is still an in-memory stand-in. No business-module events
  exist yet since no business module exists yet to own one.
- Next: continue Phase 0 with `Platform.Audit` (permanent, tamper-evident change history), then the fuller
  `Platform.UI` design system, `Platform.Api` conventions, and `Platform.Configuration`.

### 2026-07-13 — Fixed a file-lock build error, then built Platform.Workflow (approval routing)
- Agent: Claude Sonnet 5
- Phase: Phase 0 — Platform Foundation
- Status: Completed
- What changed:
  - **Build error fix**: the user hit `MSB3027`/`MSB3021` "file is locked by Gateway.Api" errors twice —
    once from a background process I'd left running after the previous session's verification, and once
    from the user's own terminal where they were watching `dotnet run`. Root cause both times: Windows
    locks a running program's files, so a second build can't overwrite them. Fixed by identifying the
    exact locking process (`tasklist`/`netstat -ano`) and stopping it, then confirmed the build was clean
    again. Also fixed a separate, real (if harmless) startup warning about HTTPS redirection — the backend
    is meant to run over plain HTTP locally (matching the frontend), so the redirect now only activates
    outside local development.
  - **Built Platform.Workflow** — the approval-routing system. In plain terms: this is what lets you say
    "a Purchase Order under 50,000 SAR needs one manager's approval, but above that it also needs Finance's
    approval," and have the system actually enforce that, including:
    - Skipping approval steps that don't apply (e.g. a small purchase needs no approval at all).
    - Requiring either "any one approver" or "everyone on a specific list must approve" (e.g. a capital
      spending committee).
    - Stopping immediately if anyone rejects, rather than continuing to ask other approvers.
    - Letting someone cover for an approver who's out of office for a date range, without giving them that
      person's permissions permanently.
    - Detecting when an approval has sat too long (a configurable time limit) so it can be escalated — the
      actual "check this every few minutes automatically" scheduling is not built yet (documented below),
      but the detection logic itself is built and tested.
  - **Documented deliberate deviation from the original plan**: the architecture document had said we'd
    use a third-party workflow engine (Elsa Workflows). Building the actual approval-routing logic
    ourselves — the same way every other kernel piece so far was built — turned out to be the right call
    for what's actually needed right now, without pulling in a large framework dependency. This is written
    down as a reasoned, dated change to that decision (`ARCHITECTURE.md` ADR-6), not a silent contradiction
    of it — the third-party engine remains an option later if genuinely complex scenarios need it.
  - Wired it into the real running backend and **re-verified the whole application still works in a
    browser** after the change (same screenshot-based check as before), not just that it compiles.
  - 18 new tests, 66 total passing across the whole project.
- Files touched: `src/Platform/Platform.Core/AttributeConstraints.cs` (shared threshold-checking logic,
  extracted so Security and Workflow don't each duplicate it), `src/Platform/Platform.Security/AuthorizationService.cs`
  (refactored to use it), `src/Platform/Platform.Workflow/*` (full new project), `tests/UnitTests/Platform.Workflow.Tests/*`,
  `src/Gateway/Gateway.Api/Program.cs` and `Controllers/SystemController.cs` (wired in), `ARCHITECTURE.md`
  (ADR-6 updated), `src/Gateway/Gateway.Api/Program.cs` (HTTPS warning fix)
- Known gap, disclosed not hidden: no actual background job runs the escalation check automatically yet —
  that needs a scheduled task in Gateway.Api, planned but not built. No real approval matrix is registered
  yet either, since no business module (e.g. Procurement) exists yet to own one.
- Next: continue Phase 0 with `Platform.Events` (cross-module messaging), then `Platform.Audit`, the fuller
  `Platform.UI` design system, `Platform.Api` conventions, and `Platform.Configuration`.

### 2026-07-13 — First runnable application: real backend + real frontend, verified in a browser
- Agent: Claude Sonnet 5
- Phase: Phase 0 — Platform Foundation
- Status: In Progress (this checkpoint Completed; Phase 0 overall is not — see Next)
- What changed: The user set a new standing rule for all future work: every phase must leave a real,
  compiling, running application the user can open in a browser — never just a class library with test
  results, and never throwaway/prototype code that gets rewritten later. This is now written into
  `AGENTS.md`/`CLAUDE.md` and `docs/architecture/06-roadmap.md` as a permanent rule, not a one-time ask.
  To meet it immediately (retroactively covering all Platform.Core/Security/Localization work so far):
  - Built **Gateway.Api** — a real backend server (not a demo) at `src/Gateway/Gateway.Api`, wired to the
    permissions and translation systems already built. It exposes: a health check, a system-status page
    (shows what's running), and a greeting endpoint that proves the Arabic/English translation system
    works through an actual running server, not just in tests.
  - Built **Apps.Shell** — a real website (not a prototype) at `src/Apps/Apps.Shell`, showing a "System
    Status" page that calls the backend above and displays it live, plus a working English/Arabic language
    switcher that flips the entire page layout right-to-left for Arabic, not just the text.
  - **Actually verified it, not just trusted it compiles**: started both, used an automated browser
    (Playwright/headless Chromium — `chromium-cli` wasn't available on this Windows machine, so adapted)
    to load the real page, click the Arabic button, and took screenshots of both states. Confirmed no
    browser console errors and that the Arabic screenshot shows a properly mirrored layout (nav pane
    moved to the other side, text right-aligned), not just translated words.
  - Added `HOW-TO-RUN.md` with plain-language, copy-pasteable steps for starting both and opening it in a
    browser — two terminal commands and one URL.
  - Caught and fixed a small hardcoded-text slip while building this: the language switcher briefly had
    "العربية" written directly in a component instead of going through the same centralized-content
    pattern used everywhere else. Fixed before it shipped.
- Files touched: `src/Gateway/Gateway.Api/*` (Program.cs, Controllers/SystemController.cs,
  Localization/GatewayApiLocalizationDefaults.cs, launchSettings.json), `src/Apps/Apps.Shell/*` (full Vite
  React TypeScript app — App.tsx, components/, pages/, api/, i18n/, styles), `HOW-TO-RUN.md`,
  `tests/ArchitectureTests/.../NoHardcodedTranslatableTextTests.cs` (allow-list entry for Gateway.Api's
  content file), `AGENTS.md`, `CLAUDE.md`, `docs/architecture/06-roadmap.md`
- Known gap, disclosed not hidden: the "no hardcoded text" automated check only covers C# (backend) code;
  the frontend follows the same discipline by convention (`src/Apps/Apps.Shell/src/i18n/`) but doesn't yet
  have an automated test enforcing it the way the backend does. Also: the Arabic backend/frontend text
  written so far is a draft translation, not reviewed by a professional Arabic linguist.
- Next: continue Phase 0 with `Platform.Workflow`, extending Gateway.Api/Apps.Shell rather than building
  them separately — every future piece plugs into this same running application from now on.

### 2026-07-13 — Added a permanent, automated guardrail against hardcoded Arabic text
- Agent: Claude Sonnet 5
- Phase: Phase 0 — Platform Foundation (cross-cutting engineering-standards work, not tied to one module)
- Status: Completed
- What changed: The user was (rightly) worried that the hardcoded-Arabic mistake caught earlier today
  (see the Platform.Localization entry below) could quietly happen again in a future session — by me, by
  Codex, by anyone — since fixing one file doesn't stop it from being reintroduced elsewhere later. Rather
  than just promising to be careful, built an automated check:
  - A new test project (`tests/ArchitectureTests/Platform.ArchitectureTests`) parses every source file
    with a real C# code parser (Roslyn) and fails the test run if any file contains actual Arabic text in
    code — unless that file is on a short, explicitly justified allow-list (currently: the one file where
    default translations are deliberately stored, and one file that's a technical character-mapping table,
    not language content).
  - **Proved it actually works**, not just trusted the logic: deliberately added a fake violation (a
    hardcoded Arabic string in an unrelated file), ran the test, confirmed it failed with a clear message
    pointing at the exact file and line, then removed the fake violation and confirmed the suite was green
    again.
  - Audited the entire codebase first (not just the one file already fixed) — confirmed no other
    hardcoded Arabic existed anywhere else in the project.
  - Documented this as an enforced rule in docs/architecture/05-engineering-standards.md, not just a
    convention that depends on someone remembering it.
  - 48 tests passing across the whole project now (2 new architecture tests + the 46 from before).
- Files touched: `tests/ArchitectureTests/Platform.ArchitectureTests/*.cs` (RepoPaths, ArabicScript,
  NoHardcodedTranslatableTextTests), its README.md, `docs/architecture/05-engineering-standards.md`
- Next: this same "write an automated test, don't just rely on a promise" pattern should be applied to
  other architecture rules as modules get built (e.g. checking that a module's Domain layer never
  references another module's internals, per docs/architecture/01-architecture-foundation.md #3.2).

### 2026-07-13 — Phase 0: Platform.Localization implemented and tested (Arabic/English, Saudi calendar, e-invoicing)
- Agent: Claude Sonnet 5
- Phase: Phase 0 — Platform Foundation
- Status: In Progress (Platform.Localization sub-piece Completed; Phase 0 overall is not — see Next)
- What changed: Built the piece that makes the system genuinely bilingual and Saudi-compliant, not just
  "English with an Arabic option bolted on." In plain terms:
  - **Text/translation lookup**: every screen label is resolved by a key (e.g. "Purchase Order Header"),
    not hardcoded — so a customer can override wording without a developer changing code and redeploying.
    Falls back sensibly (customer's own wording → standard Arabic/English → a visibly-flagged "missing
    translation" marker, never a silent blank).
  - **Arabic/English direction handling**: a single place decides that Arabic reads right-to-left and
    English left-to-right, so screens flip correctly instead of each screen having its own logic.
  - **Saudi (Hijri) calendar**: dates convert correctly between the Gregorian calendar (what's always
    stored internally) and the Hijri calendar (what a user may prefer to see), using the same official
    Saudi-government calendar table Microsoft ships in .NET — not a hand-built approximation.
  - **Saudi Riyal number formatting**, correctly ordered for each language (e.g. "SAR 12,500.50" in
    English vs "12,500.50 ر.س" in Arabic).
  - **ZATCA-compliant QR codes** for tax invoices (the government e-invoicing requirement) — verified the
    exact required format against ZATCA's own published specification before writing code, since getting
    a government compliance format wrong is a real business risk, not just a bug. Handles Arabic company
    names correctly in the encoding.
  - **Caught and fixed a mistake mid-build**: initially wrote the Arabic/English currency labels directly
    into the formatting code, which contradicts the "text isn't hardcoded" rule above — the user asked
    "do you hardcode Arabic, does SAP do that?" which caught it. Fixed by moving the actual words into a
    dedicated "defaults" file (mirroring how SAP/Dynamics ship default translations in a text repository,
    not inside program logic) and having the formatter accept an already-looked-up label instead.
  - 23 new automated tests, all passing — 46 total across the whole project now.
  - **Deliberately not built yet**: WPS payroll files and GOSI integration (belong to Payroll, not
    general localization), ZATCA's more advanced "Phase 2" government-integrated invoicing (needs a live
    government API connection and real invoices to send), and the actual admin screen a non-developer
    would use to edit translations (needs the on-screen UI framework, not built yet).
- Files touched: `src/Platform/Platform.Localization/*.cs` (SupportedLanguage, TextDirection,
  LocalizationResourceKeys, LocalizationDefaults, Translation/*, Calendar/*, Formatting/*, Zatca/*),
  `src/Platform/Platform.Localization/README.md`, `tests/UnitTests/Platform.Localization.Tests/*.cs`
- Next: continue Phase 0 with `Platform.Workflow` (approval routing), then `Platform.Events`,
  `Platform.Audit`, `Platform.UI`, `Platform.Api`, `Platform.Configuration`.

### 2026-07-13 — Phase 0: Platform.Security implemented and tested (who's allowed to do what)
- Agent: Claude Sonnet 5
- Phase: Phase 0 — Platform Foundation
- Status: In Progress (Platform.Security sub-piece Completed; Phase 0 overall is not — see Next)
- What changed: Built the permissions engine every module will check before letting a user do anything.
  In plain terms:
  - **Roles, Duties, Privileges**: a user is assigned Roles (e.g. "Junior Finance Approver"); a Role is a
    bundle of Duties (job functions, e.g. "Approve Small Purchase Orders"); a Duty grants specific
    Privileges (the smallest permission, e.g. "approve a purchase order"). This 3-level structure is what
    lets amount-based limits work — e.g. one Duty grants "approve up to 50,000 SAR" and another grants
    "approve any amount," and a user holding either (or both) gets the correct combined limit.
  - **Segregation of Duties (SoD)**: the system can detect when a user has been given two job functions
    that shouldn't be combined (e.g. "create a vendor" and "approve payment to a vendor") — a classic
    fraud-prevention control auditors look for. If a business genuinely needs an exception (common in a
    small office with too few staff to separate every duty), it can be explicitly granted and is
    permanently logged with who approved it and why — never silently allowed.
  - **Row-level security**: restricts which company/branch/project a user can see records for (e.g. a
    branch accountant only sees their own branch's invoices).
  - **Field-level masking**: sensitive fields (salary, bank account/IBAN) are automatically hidden from
    anyone who doesn't specifically hold the permission to view them, showing asterisks instead.
  - Proved all four pieces with 14 automated tests (all passing) on top of the 9 from Platform.Core — 23
    total, `dotnet test` green across the whole solution.
  - **Deliberately not built yet**: the actual login screen/single sign-on and multi-factor authentication.
    Those require a real identity provider to be stood up (a hosting decision, not a code decision) — this
    piece decides "what can a user do" once they're already logged in; a later piece will handle "how do
    they log in."
- Files touched: `src/Platform/Platform.Security/*.cs` (Privilege, PrivilegeGrant, Duty, Role,
  SecurityPrincipal, ISecurityCatalog/InMemorySecurityCatalog, IAuthorizationService/AuthorizationService,
  AuthorizationResult, Sod/*, RowLevel/*, FieldLevel/*), `src/Platform/Platform.Security/README.md`,
  `tests/UnitTests/Platform.Security.Tests/*.cs`
- Next: continue Phase 0 with `Platform.Localization` (Arabic/English, Hijri calendar, ZATCA), then
  `Platform.Workflow`, `Platform.Events`, `Platform.Audit`, `Platform.UI`, `Platform.Api`,
  `Platform.Configuration` — same "build the reusable piece, prove it with tests" approach as these two.

### 2026-07-13 — Phase 0: Platform.Core implemented and tested (first working code)
- Agent: Claude Sonnet 5
- Phase: Phase 0 — Platform Foundation
- Status: In Progress (Platform.Core sub-piece Completed; Phase 0 overall is not — see Next)
- What changed: This is the first actual code in the project (everything before this was architecture
  docs and folder scaffolding). Installed .NET 8 SDK (was missing on this machine), initialized git
  (repo had no version control before), and built `Platform.Core` — the foundation every other module
  will build on:
  - The shared Business Object base class (`BusinessObject`) that every future module's documents
    (Purchase Orders, Journal Entries, Subcontracts, etc.) will inherit from, giving them a document
    number, a status, an audit trail, and custom fields for free.
  - The lifecycle rules engine (`LifecycleEngine`) enforcing the one allowed set of status changes
    (Draft → Submitted → Approved → Posted, or Reversed/Cancelled) so no document can be pushed into an
    illegal state, and so every status change automatically raises an event other services (audit,
    workflow) can react to later.
  - The document numbering service (`INumberRangeService` / `InMemoryNumberRangeService`) — assigns
    sequential numbers like `PROC-PO-2026-000123` per company and fiscal year (in-memory version now;
    swapped for a database-backed one later without touching any module code).
  - Custom-field storage (`ExtensionFieldBag`) — lets a future site-specific requirement add a field to
    any document without a database schema change.
  - Proved all of it end-to-end with a throwaway "demo document" (no real business meaning) taken through
    create → number → submit → approve → post → reverse, matching the Phase 0 exit criteria in
    `docs/architecture/06-roadmap.md`. 9 automated tests, all passing (`dotnet test`).
- Files touched: `erp-platform.sln`, `src/Platform/Platform.Core/*.cs` (BusinessObject, IBusinessObject,
  BusinessObjectStatus, BusinessObjectTransition, BusinessObjectReference, ExtensionFieldBag,
  Lifecycle/LifecycleEngine.cs, Events/IDomainEvent.cs, Events/BusinessObjectStatusChangedEvent.cs,
  NumberRanges/INumberRangeService.cs, NumberRanges/NumberRangeDefinition.cs,
  NumberRanges/InMemoryNumberRangeService.cs), `tests/UnitTests/Platform.Core.Tests/*.cs`, `.gitignore`
- Next: Phase 0 is not finished — `Platform.Core` is one of nine pieces. Still to build:
  `Platform.Security` (login/permissions), `Platform.Localization` (Arabic/English, Hijri calendar,
  ZATCA), `Platform.Workflow` (approval routing), `Platform.Events` (cross-module messaging, database-
  backed), `Platform.Audit` (permanent change history), `Platform.UI` (the actual screens), `Platform.Api`
  (how the screens talk to the server), `Platform.Configuration` (settings without needing new code). No
  business modules (Finance, Procurement, Construction, etc.) start until these are done.

### 2026-07-13 — Architecture corrected against SAP/Dynamics source material
- Agent: Claude Sonnet 5
- Phase: Architecture Baseline
- Status: Completed
- What changed: Corrected the initial architecture draft after it conflated SAP Fiori UI terminology
  ("Object Page"/"List Report") with the Dynamics-365-referenced UI layer, and modeled Finance/Construction
  too shallowly (siloed GL/AP/AR/Assets; flat document list with no shared cost backbone). Verified against
  SAP Help Portal/SAP-PRESS and Microsoft Learn, then rewrote: doc 02 §2–3 (Dynamics-accurate Workspace /
  Navigation Pane / merged List+Details form / Action Pane / FastTabs), doc 01 §3 (module boundaries —
  ProjectManagement now owns WBS elements as the Controlling backbone; Construction is the commercial layer
  on top of it; Finance is one Universal-Journal-style store, not four siloed ledgers), and added new
  doc 07 (Universal Journal, Document Splitting, Parallel Ledgers, WBS/Networks, Results Analysis + Settlement
  to CO-PA — the percentage-of-completion revenue recognition engine, prioritized because Project Management
  is this project's top priority). Added this progress-log system (PROGRESS.md, AGENTS.md) so future work by
  any AI tool or human stays visible and consistent.
- Files touched: ARCHITECTURE.md, docs/architecture/01-architecture-foundation.md,
  docs/architecture/02-business-object-model.md, docs/architecture/07-project-accounting-and-financial-architecture.md,
  src/Modules/Modules.Finance/README.md, src/Modules/Modules.Construction/README.md,
  src/Modules/Modules.ProjectManagement/README.md, PROGRESS.md, AGENTS.md
- Next: Phase 0 — implement `Platform.Core` (BO base classes, lifecycle FSM, number ranges, extension-field
  storage) per `docs/architecture/06-roadmap.md`. Exit criteria: a trivial demo BO can be created, submitted,
  approved via a configured workflow, posted, audited, and printed bilingually, with zero business logic.

### 2026-07-13 — Initial architecture baseline authored
- Agent: Claude Sonnet 5
- Phase: Architecture Baseline
- Status: Completed (superseded in part by the correction entry above — see it for what changed)
- What changed: Authored the first full technical architecture: layered architecture, folder structure,
  module boundaries, Business Object model, Object Page standard, navigation model, localization, security,
  API, database, coding standards, configuration, events, workflow, reporting, audit, extension model, and
  roadmap. Scaffolded the physical repo skeleton (src/Platform, src/Modules, src/Apps, Gateway, tests, infra,
  tools) with README stubs and a sample module manifest.
- Files touched: ARCHITECTURE.md, docs/architecture/01–06*.md, CLAUDE.md, full src/ + tests/ + infra/ + tools/
  directory skeleton
- Next: see correction entry above — this draft's UI and Finance/Construction sections needed rework before
  Phase 0 implementation should start.
