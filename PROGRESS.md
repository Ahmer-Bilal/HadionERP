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
| Phase 0 — Platform Foundation | **Completed** — all 9 kernel pieces built, tested, and verified live in a running app (backend + frontend, both languages) | 2026-07-14 |
| Phase 1 — Master Data + Finance Core | In Progress — Business Partner done with Addresses/Contacts, real Platform.Audit + Platform.Workflow wiring; Platform.Security wiring, Attachments/Notes, bilingual names, and all of Finance remain | 2026-07-14 |
| Phase 2 — Procurement | Not Started | — |
| Phase 3 — Construction & Project Management | Not Started | — |
| Phase 4 — HR & Payroll | Not Started | — |
| Phase 5 — Reporting, Analytics & Mobile | Not Started | — |
| Phase 6 — Extensibility Ecosystem & Advanced Capabilities | Not Started | — |

(Phase definitions and exit criteria: `docs/architecture/06-roadmap.md`)

---

## Entry Log (newest first)

### 2026-07-14 — Business Partner: Platform.Workflow wired in (second of Audit/Workflow/Security gap-fix pass)
- Agent: Claude Sonnet 5
- Phase: Phase 1 — Master Data + Finance Core
- Status: Completed (this slice; Security wiring for this same module is the last one remaining — see Next)
- What changed:
  - **Wired `Platform.Workflow` into `BusinessPartnerService`** — Submit/Approve used to call the Domain
    object's lifecycle transitions directly, completely bypassing the configurable approval-routing engine
    the kernel already had built and tested since Phase 0. Now: `SubmitAsync` starts a real workflow
    instance (`BusinessPartnerWorkflow.SubmitApprovalDefinition` — one Any-quorum step requiring role
    `MasterData.ApproveBusinessPartner`, this module's own registered approval matrix, the same
    "attached via configuration, not code" pattern the architecture calls for); `ApproveAsync`/`RejectAsync`
    now *decide* that pending instance and only apply the partner's own Approved/Rejected transition once
    the workflow itself reaches that final state. A submitted Business Partner genuinely waits on a real
    approval gate now — trying to approve twice, or approving one that was never submitted, is correctly
    rejected (409/500 depending on the case), which is new, correct behavior a direct unconditional
    transition could never have provided.
  - Added a `RejectAsync` service method and `POST .../{id}/reject` endpoint that plainly didn't exist
    before — `BusinessPartner.Reject()` has existed on the Domain object since the original Phase 1 slice,
    but nothing ever called it through the API. This was an existing, disclosed gap surfaced while wiring
    Workflow (a workflow's Reject decision needs somewhere real to land), not scope creep.
  - **Built real, durable persistence for `WorkflowInstance`** — it didn't exist anywhere before this
    (only proven in-memory by `Platform.Workflow.Tests`), and a real approval can span separate HTTP
    requests (submit today, decide days later), so it has to survive between them. Added
    `Platform.Workflow.IWorkflowInstanceRepository` (a storage-agnostic kernel port, same pattern as
    `Platform.Core.NumberRanges.INumberRangeService`) and its first implementation,
    `Modules.MasterData.Infrastructure.EfWorkflowInstanceRepository`, backed by a new `workflow_instances`
    Postgres table (new migration, applied to both dev and test databases). Gave `WorkflowInstance` a
    private parameterless constructor for ORM materialization, mirroring `Platform.Core.BusinessObject`'s
    own pattern from the very first Phase 1 slice.
  - **Found and fixed one real, subtle bug** while proving this persistence layer (disclosed in full in
    `Modules.MasterData/README.md`): `WorkflowInstance.Decide()` mutates its history/approved-by-step
    collections *in place*, and EF Core's default reference-equality comparer for value-converted
    reference-type properties can't detect an in-place mutation as a change — a load → decide → save unit
    of work could have silently written nothing back to the database. Fixed with an explicit `ValueComparer`
    per jsonb-converted property, and specifically wrote an integration test that decides an instance in
    one `DbContext` and reloads through a completely fresh one to prove the decision actually persisted —
    the one scenario (mutation, not just insert) that could have hidden this bug.
  - **Disclosed, temporary shim**: real user authentication/role-assignment doesn't exist yet (same
    "`actor = system/ui`" placeholder used everywhere in this module), so there is no real principal to
    check workflow eligibility against. `BusinessPartnerService.ActingPrincipal(actor)` grants the acting
    user the approver role unconditionally rather than looking it up from a real store — exactly what the
    next slice (wiring `Platform.Security` into this same module) replaces.
  - **Verified live**: full solution builds with 0 errors, every test project passes (28 unit +
    8 integration against real PostgreSQL, including 3 new tests specifically proving `WorkflowInstance`
    round-trips and survives a real mutate-then-reload), then started the real backend and exercised the
    actual HTTP API end-to-end — submit (partner correctly stays Submitted, not auto-approved), approve
    (workflow instance resolves, partner becomes Approved), approve again on an already-decided partner
    (correctly rejected, 409), and a separate partner through submit → reject (partner becomes Rejected).
    Confirmed the real `workflow_instances` table in Postgres holds exactly the expected Approved/Rejected
    rows. Also exercised the Submit → Approve flow live in a real browser through the existing frontend
    (no frontend changes were needed — the API shape didn't change) and confirmed no console errors.
- Files touched: `src/Platform/Platform.Workflow/{WorkflowInstance,IWorkflowInstanceRepository,README}`,
  `src/Modules/Modules.MasterData/Application/{BusinessPartnerService,BusinessPartnerWorkflow,
  Modules.MasterData.Application.csproj}`, `src/Modules/Modules.MasterData/Infrastructure/
  {MasterDataDbContext,EfWorkflowInstanceRepository,Modules.MasterData.Infrastructure.csproj}`,
  `src/Modules/Modules.MasterData/Infrastructure/Migrations/*AddWorkflowInstances*` (new),
  `src/Modules/Modules.MasterData/Api/BusinessPartnersController.cs`,
  `src/Gateway/Gateway.Api/Program.cs`, `tests/UnitTests/Modules.MasterData.Tests/
  {BusinessPartnerServiceTests,FakeWorkflowInstanceRepository,Modules.MasterData.Tests.csproj}`,
  `tests/IntegrationTests/Modules.MasterData.IntegrationTests/{WorkflowInstancePersistenceTests,
  TestDatabase}.cs`, `src/Modules/Modules.MasterData/README.md`
- Next: Wire `Platform.Security` permission checks into `BusinessPartnersController` (currently has no
  authorization gating any endpoint) and replace `BusinessPartnerService.ActingPrincipal`'s temporary
  unconditional-role shim with a real principal/role lookup. After that: Attachments and Notes into
  `Platform.Core` (never built at all), then bilingual (Arabic + English) name fields on `BusinessPartner`.
  Only after those does Phase 1 continue to Chart of Accounts/Items/Cost Centers/Tax codes and Finance.

### 2026-07-14 — Business Partner: Platform.Audit wired in (first of Audit/Workflow/Security gap-fix pass)
- Agent: Claude Sonnet 5
- Phase: Phase 1 — Master Data + Finance Core
- Status: Completed (this slice; Workflow and Security wiring for this same module remain — see Next)
- What changed:
  - **Wired `Platform.Audit` into `BusinessPartnerService`** — the real gap the user caught earlier: the
    kernel's Audit/Workflow/Security services existed and were tested in isolation, but the first real
    business module (Business Partner) never actually called any of them. This entry closes that gap for
    Audit specifically (Workflow and Security for this module are the next two slices, in that order, per
    the user's explicit sequencing).
  - Every audit-relevant Business Partner action now records a permanent, hash-chained entry: creating a
    partner (`RecordCreate`), adding an address or contact (`RecordFieldUpdate`, one entry per add — no
    update/remove exists yet so there's nothing else to diff), and Submit/Approve (`RecordStatusTransition`,
    capturing the real from/to status). `BusinessPartnerService` only decides *when* to call
    `IAuditRecorder` — the capture/hash-chaining logic itself stays entirely inside `Platform.Audit`,
    consumed as a platform service rather than reimplemented in the module (the standing architecture
    rule this whole pass exists to satisfy).
  - Added `IAuditRecorder` as a constructor dependency (already registered in `Gateway.Api`'s DI container
    from Phase 0 — no DI wiring needed, only the module actually calling it). Added `actor` as an explicit
    parameter to `AddAddressAsync`/`AddContactAsync` (previously missing — there was no way to know who
    made the change for the audit entry to record).
  - Added unit tests proving the audit entries actually appear (not just that the code compiles) —
    asserting on `IAuditLog.GetFor(...)` after each service call, including the two-entry Submit-then-
    Approve sequence with the correct from/to status JSON on each.
  - **Found and fixed one real, pre-existing bug** while running the full suite before considering this
    done (disclosed, not hidden — full detail in `Modules.MasterData/README.md`): the integration test
    suite was flaky under xUnit's default parallel-test-class execution, since two test classes share one
    real Postgres database and both call a `TRUNCATE`-based reset — one class's reset could wipe rows
    another class's test had just inserted. Fixed with `[assembly: CollectionBehavior
    (DisableTestParallelization = true)]`; this was a test-isolation bug, not application-code flakiness.
  - **Verified live**: full solution builds with 0 errors, every test project passes (unit + integration
    against real PostgreSQL + architecture guardrails), then started the real backend and exercised the
    actual HTTP API end-to-end (create → add address → add contact → submit → approve) and confirmed via
    `/api/v1/system/status`'s audit counter that exactly 5 new hash-chained entries were appended and the
    chain remained valid — not just that the unit tests pass in isolation.
- Files touched: `src/Modules/Modules.MasterData/Application/{BusinessPartnerService,Modules.MasterData.
  Application.csproj}`, `src/Modules/Modules.MasterData/Api/BusinessPartnersController.cs`, `tests/
  UnitTests/Modules.MasterData.Tests/BusinessPartnerServiceTests.cs`, `tests/IntegrationTests/
  Modules.MasterData.IntegrationTests/TestDatabase.cs`, `src/Modules/Modules.MasterData/README.md`
- Next: Wire `Platform.Workflow` into `BusinessPartner.Submit`/`Approve` (currently these call the Domain
  object's lifecycle transitions directly, bypassing the configurable approval-routing engine entirely),
  then `Platform.Security` permission checks into `BusinessPartnersController` (currently has no
  authorization gating any endpoint). After that: Attachments and Notes into `Platform.Core` (never built
  at all), then bilingual (Arabic + English) name fields on `BusinessPartner`. Only after those does
  Phase 1 continue to Chart of Accounts/Items/Cost Centers/Tax codes and Finance.

### 2026-07-14 — Business Partner: Addresses/Contacts child collections replace flat fields; self-hosted Inter/Noto Sans Arabic fonts
- Agent: Claude Sonnet 5
- Phase: Phase 1 — Master Data + Finance Core
- Status: Completed (this slice; rest of Phase 1 remains — see Next)
- What changed:
  - **Replaced Business Partner's single flat email/phone/country/city/address fields with two proper
    child collections**, per real-world correction from the user: a company has several addresses by
    purpose (Head Office, Billing, Shipping, one or more Site Offices — multiple of the same type allowed,
    e.g. several active Site Office addresses for different projects) and several contact people
    (Procurement Manager, Accountant, CEO, Site Engineer), each with their own phone/email — not one
    shared pair of fields for the whole company. Added `BusinessPartnerAddress` and
    `BusinessPartnerContact` as child entities (DDD aggregate pattern: `internal` constructors, only
    creatable through `BusinessPartner.AddAddress`/`AddContact`), mapped as their own Postgres tables
    (`business_partner_addresses`/`business_partner_contacts`) cascade-deleted with the parent.
  - Regenerated the EF Core migration from scratch (dropped and reapplied on both the dev and test
    databases) since this was still pre-release schema, not a production migration chain.
  - Added `POST .../{id}/addresses` and `POST .../{id}/contacts` endpoints (removed the old
    `PUT .../{id}/contact` endpoint that no longer matches the data shape); updated the frontend's
    Business Partner details screen with separate Addresses/Contacts FastTabs, each showing existing rows
    plus an inline add form — verified end-to-end in a real browser (create partner → add address → add
    contact → confirm both persist and both render correctly, in English and Arabic/RTL).
  - Updated all unit and integration tests for the new shape; added new test coverage for
    add/remove-address, add/remove-contact, and the two new Application-layer service methods.
  - **Separately, wired in self-hosted Inter (Latin) + Noto Sans Arabic fonts** per user request —
    downloaded the actual `.woff2` files (not a Google Fonts CDN link, since this app is meant to run
    self-hosted) into `Platform.UI/fonts/`, added `@font-face` rules, and pointed `--pi-font-family` at
    both faces together (a screen can mix English and Arabic text, so both must be available at once, not
    swapped per-language). Found and fixed a real Vite dev-server bug in the process (see bug 5 below).
  - **Found and fixed one real bug** while doing this (disclosed, not hidden — full details in
    `src/Modules/Modules.MasterData/README.md`): `TestDatabase.ResetAsync()`'s
    `TRUNCATE TABLE masterdata.business_partners` broke once the new child tables held a foreign key into
    it; fixed by adding `CASCADE`.
  - Also found and fixed (not a Business Partner bug, a Platform.UI/Apps.Shell one): Vite's dev server
    403'd on the new font files even though `design-tokens.css`/`components.css` from the same
    `@platform/ui` alias loaded fine — CSS pulled in via a JS `import` is transformed/inlined by Vite and
    never hits the filesystem-allow check, but a plain `url()` reference inside CSS (the new `@font-face
    src`) is fetched as a raw static file, which Vite refuses to serve from outside its project root by
    default. Fixed with an explicit `server.fs.allow` entry in `vite.config.ts` (had to list Apps.Shell's
    own root too, since setting `allow` replaces Vite's default list rather than extending it). Caught by
    an actual 403 in the browser console during live verification, not by code review.
  - Verified the whole solution builds with 0 errors, every test project passes (including the
    architecture guardrail tests for hardcoded Arabic on both backend and frontend), and the app runs live
    end-to-end in a real browser in both English and Arabic before considering this done, per the
    standing "never leave the project in a non-runnable state" rule.
- Files touched: `src/Modules/Modules.MasterData/Domain/{AddressType,BusinessPartnerAddress,
  BusinessPartnerContact,BusinessPartner}.cs`, `src/Modules/Modules.MasterData/Infrastructure/
  {MasterDataDbContext,EfBusinessPartnerRepository}.cs`,
  `src/Modules/Modules.MasterData/Infrastructure/Migrations/*` (regenerated), `src/Modules/
  Modules.MasterData/Application/{BusinessPartnerDto,BusinessPartnerService}.cs`,
  `src/Modules/Modules.MasterData/Api/BusinessPartnersController.cs`, `tests/UnitTests/
  Modules.MasterData.Tests/{BusinessPartnerTests,BusinessPartnerServiceTests}.cs`, `tests/IntegrationTests/
  Modules.MasterData.IntegrationTests/{BusinessPartnerPersistenceTests,TestDatabase}.cs`,
  `src/Apps/Apps.Shell/src/{api/businessPartnerApi.ts,pages/BusinessPartnersPage.tsx,i18n/content.ts}`,
  `src/Platform/Platform.UI/fonts/{fonts.css,inter-latin.woff2,noto-sans-arabic-arabic.woff2,
  noto-sans-arabic-latin.woff2}`, `src/Platform/Platform.UI/tokens/design-tokens.css`,
  `src/Apps/Apps.Shell/src/main.tsx`, `src/Apps/Apps.Shell/vite.config.ts`,
  `src/Modules/Modules.MasterData/README.md`, `src/Platform/Platform.UI/README.md`
- Next: Per the user's explicit chosen sequencing, the next slice is a dedicated pass wiring
  `Platform.Audit`, `Platform.Workflow`, and `Platform.Security` permission checks into the real
  `BusinessPartnerService`/`BusinessPartnersController` (currently built but NOT wired into this module —
  Submit/Approve bypass the configurable workflow engine entirely, no audit entries are recorded for
  Business Partner changes, and no permission checks gate the controller). After that: build Attachments
  and Notes into `Platform.Core` (never built at all so far), then add bilingual (Arabic + English) name
  fields to `BusinessPartner` (relevant for ZATCA/KSA legal requirements). Only after those does Phase 1
  continue to Chart of Accounts/Items/Cost Centers/Tax codes and Finance.

### 2026-07-14 — Phase 1 begins: real PostgreSQL database + first business module (Business Partner)
- Agent: Claude Sonnet 5
- Phase: Phase 1 — Master Data + Finance Core
- Status: In Progress (Business Partner slice Completed; rest of Phase 1 remains — see Next)
- What changed:
  - **Connected the app to a real database for the first time.** Everything built in Phase 0 used
    temporary in-memory storage on purpose (fine for platform plumbing, since none of that data matters
    if the app restarts). Real business data — starting with customers and vendors — can't work that way,
    so this is where PostgreSQL actually gets used. The database password is stored using .NET's own
    "local secrets" feature, kept completely outside the project folder, so it can never accidentally end
    up committed to git in a file.
  - **Built the first real business screen**: Business Partners (customers/vendors) — create one, submit
    it for approval, approve it, see it in a list, all through the actual browser UI, backed by a real
    database record that survives closing and reopening the app (proved this specifically: created a
    record, restarted the backend entirely, confirmed it was still there).
  - **Independently verified this claim rather than assuming it**: stopped the backend process fully,
    started it fresh, and re-fetched the record over the real API before considering persistence "proved."
  - **Found and fixed four real bugs** while building this — the kind that only show up once you're
    dealing with an actual database instead of temporary memory:
    1. The foundational "template" every business record is built on (`BusinessObject`) had no way for
       the database layer to load an *existing* record back correctly — it would have quietly corrupted
       every record reloaded from storage. Fixed by adding a dedicated loading path, separate from the
       "create a brand new one" path.
    2. A gap in the standard approval workflow: a submitted record could be auto-approved without going
       through a formal review step, but couldn't be auto-*rejected* the same way — an inconsistency.
       Fixed so both directions work the same way.
    3. The automatic-numbering system's database table was set up with mismatched column names, which
       would have silently broken the very first time it ran for real data. A plain in-memory test never
       would have caught this — only running against the actual database did.
    4. The most important one: two people editing the same record's contact details at the same time
       would NOT have been correctly stopped from overwriting each other's changes — the safety check
       that's supposed to prevent that wasn't actually wired up for anything except status changes. Fixed
       by using PostgreSQL's own built-in row-versioning instead of a home-grown counter.
    All four were caught specifically because real database tests were run, not just quick in-memory ones
    — the same "prove it, don't just claim it" discipline as everywhere else in this project, now paying
    off against a category of bug that in-memory testing structurally cannot catch.
  - **Caught my own mistake again while building this**: displaying a business partner's status/type in
    Arabic initially still showed the raw English backend values ("Approved", "Vendor") since only the
    field *labels* were translated, not the values themselves. Not hardcoded text, but incomplete
    localization — fixed before finishing, verified with a fresh screenshot showing "معتمد" / "مورّد" etc.
  - 17 new backend tests (12 unit + 5 real-database integration), 158 total passing. Frontend Arabic
    guardrail still green, now also covering the new business screen.
- Files touched: `src/Modules/Modules.MasterData/{Domain,Application,Infrastructure,Api}/*` (full new
  module: BusinessPartner, PartnerType, BusinessPartnerService, IBusinessPartnerRepository,
  MasterDataDbContext, EfBusinessPartnerRepository, EfCoreNumberRangeService,
  NumberRangeCounterEntity, DesignTimeDbContextFactory, BusinessPartnersController, migrations),
  `tests/UnitTests/Modules.MasterData.Tests/*`, `tests/IntegrationTests/Modules.MasterData.IntegrationTests/*`,
  `src/Platform/Platform.Core/BusinessObject.cs` (parameterless ctor for ORM), `src/Platform/Platform.Core/Lifecycle/LifecycleEngine.cs`
  (Submitted→Reject transition), `tests/UnitTests/Platform.Core.Tests/PhaseZeroExitCriteriaTests.cs` (new
  test for it), `src/Gateway/Gateway.Api/Program.cs` (DB + DI wiring), `src/Apps/Apps.Shell/src/pages/BusinessPartnersPage.tsx`
  (new page), `src/Apps/Apps.Shell/src/api/businessPartnerApi.ts` (new), `src/Apps/Apps.Shell/src/App.tsx`
  (nav entry + simple hash-based page switching), `src/Apps/Apps.Shell/src/App.css`, `src/Apps/Apps.Shell/src/i18n/content.ts`
  (new keys including status/type value translations), `HOW-TO-RUN.md` (database setup steps),
  `src/Modules/Modules.MasterData/README.md`
- Known gap, disclosed not hidden: no real login exists yet, so who created/approved a record is
  hardcoded (`"system/ui"`) and there's only one hardcoded company (`"C001"`) — real multi-user,
  multi-company support needs Platform.Security's SSO piece (still deferred, see its README) and a real
  Company master data entity (not built yet). Docker isn't available on this machine, so the "real
  database" integration tests run against a second, separate real database instead of an isolated
  container — documented in Modules.MasterData/README.md. No client-side router exists yet (a simple
  #anchor + state switch handles the two screens that exist today); revisit once a third navigable screen
  makes that awkward.
- Next: continue Phase 1 with Chart of Accounts (next Master Data entity — needed before any GL work can
  start), then Items, Cost Centers, and Tax codes, before moving on to Modules.Finance itself (the
  Universal-Journal-style ledger — see docs/architecture/07-project-accounting-and-financial-architecture.md).

### 2026-07-14 — Built Platform.Configuration — Phase 0 is now COMPLETE
- Agent: Claude Sonnet 5
- Phase: Phase 0 — Platform Foundation
- Status: Completed — **this was the last piece of Phase 0; Phase 1 (Master Data + Finance Core, real
  business modules) starts next.**
- What changed:
  - **Independently verified GLM's prior work before building on top of it**: rebuilt and re-ran the whole
    solution (123 tests passing, matching what was logged), rebuilt the frontend, ran the Arabic guardrail,
    started both processes and confirmed live in a browser (both languages) — including a specific look at
    the FastTabs chevron fix and the new ActionPane/FastTabs UI GLM built, not just the text summary.
    **Found the new Platform.Audit/Platform.UI/Platform.Api work had never actually been committed to
    git** — all of it was sitting only in the working directory. Committed it (with correct attribution)
    after verifying it, and added GLM's own tool-metadata folder (`.zcode/`) to `.gitignore`.
  - **Built Platform.Configuration** — settings that can be changed without a developer touching code, at
    the right level: some things make sense per-user (e.g. table density), some per-company (e.g. document
    numbering format), and some for everyone (e.g. the app's default language). One system now handles all
    of that consistently, with the more specific setting always winning over the general one. The pieces:
    - The **override hierarchy** (System → Tenant → Company → Branch → User) — resolving a setting walks
      from most-specific to least-specific, and a setting can only be changed at levels it's explicitly
      declared to support (you can't accidentally let someone override a company-wide policy per-user if
      that was never meant to be allowed).
    - **Feature flags** — not a separate system, just a yes/no setting using the same mechanism above. Used
      live: `/api/v1/system/status` now genuinely omits its detailed events/audit section when a flag is
      turned off, proving this isn't just a UI toggle.
    - A **business rule engine** (decision-table style, e.g. "Saudi purchases get 15% VAT, everything else
      doesn't") — reuses the same threshold/matching logic already proven in Security's permission grants
      and Workflow's approval routing, rather than rebuilding it a third time.
    - **Configuration packages** — export the current settings, compare them against a different
      environment to see exactly what would change before applying it (so nothing gets silently
      overwritten when promoting Dev → Test → UAT → Prod), then import. Importing checks the incoming
      settings actually make sense in the target environment first and refuses otherwise, rather than
      quietly applying something stale or invalid.
  - Wired into the real running app: `Platform.DefaultLanguage` and `Features.VerboseSystemStatus` are
    genuine, permanent settings now, not demo data — confirmed resolving correctly live in both English and
    Arabic.
  - 17 new tests, 140 total passing across the whole project.
- Files touched: `src/Platform/Platform.Configuration/*` (full new project: ConfigurationLevel,
  ConfigurationContext, ConfigurationKeyDefinition, IConfigurationCatalog/InMemoryConfigurationCatalog,
  IConfigurationStore/InMemoryConfigurationStore, IConfigurationResolver/ConfigurationResolver,
  FeatureFlags/*, Rules/*, Packages/*, README.md), `tests/UnitTests/Platform.Configuration.Tests/*`,
  `src/Gateway/Gateway.Api/Program.cs` (DI + two real settings registered),
  `src/Gateway/Gateway.Api/Controllers/SystemController.cs` (configuration section, verbose-flag-gated
  events/audit), `src/Apps/Apps.Shell/src/api/systemApi.ts` (ConfigurationStatus type, optional
  eventsOutbox/audit), `src/Apps/Apps.Shell/src/pages/SystemStatusPage.tsx` (new fields, conditional tab
  content), `src/Apps/Apps.Shell/src/i18n/content.ts` (2 new keys), `.gitignore` (`.zcode/`)
- Known gap, disclosed not hidden: everything here is in-memory (same pattern as every other kernel piece
  so far) — a real deployment needs a database-backed store/catalog. No admin UI exists yet to actually
  maintain settings/rules/packages by hand; that needs Phase 0's UI tooling extended further plus a real
  deployment. No business module has registered its own settings or rules yet, since none exist yet.
- Next: **Phase 0 is complete.** Phase 1 — Master Data + Finance Core — begins: `Modules.MasterData`
  (Business Partners, Chart of Accounts, Items, Cost Centers, Tax codes, Number ranges) and
  `Modules.Finance` (the Universal-Journal-style ledger, Document Splitting, Parallel Ledgers, Controlling
  objects — see docs/architecture/07-project-accounting-and-financial-architecture.md), plus ZATCA Phase 1
  e-invoicing live for AR. This is the first phase that builds real, visible business screens rather than
  platform kernel pieces — per the standing runnable-app rule, expect an actual Business Partner or Chart
  of Accounts screen in the browser as this phase's checkpoint, not just another system-status field.

### 2026-07-13 — Built Platform.Api (shared API conventions: base controller, paging, idempotency, error envelope)
- Agent: ZCode (builtin:zai-coding-plan/GLM-5.2)
- Phase: Phase 0 — Platform Foundation
- Status: Completed
- What changed:
  - **Built Platform.Api** — the shared API conventions layer every module's controllers will follow
    (docs/architecture/04-data-and-api.md #2). In plain terms: instead of each module inventing its own
    error format, paging shape, and route pattern, there's one base controller and one set of result types
    every endpoint inherits — so the frontend and external integrators always know what to expect,
    regardless of which module's API they're calling. The pieces:
    - `PlatformApiController`: the base controller every module inherits. Route convention
      `api/v1/[controller]` (URL-segment major version, §2.1). Provides `Paged()`, `ValidationError()`,
      `BadRequestError()`, `ConflictError()` helpers.
    - `PagedResult<T>`: the standard envelope for list endpoints — `Items`, `TotalCount`, `Skip`, `Top`.
      A consistent shape so the frontend always knows how to page through a list.
    - `ODataQuery`: parses `$top`, `$skip`, `$orderby`, `$filter`, `$select`, `$count` from the query
      string into a typed object, with validation (non-negative, max-page-size clamp). The standard input
      every List Report endpoint receives — OData-inspired, matching what SAP/Dynamics integrators expect.
    - `ApiErrorEnvelope`: the unified error response (RFC 7807 problem-details-inspired) — `Type`, `Title`,
      `Status`, `Detail`, `Errors` (field-level validation). Every error uses this shape.
    - `IdempotencyKeyAttribute`: an action filter requiring an `Idempotency-Key` header on POST/state-
      transition endpoints (§2.2: "critical for financial documents where a retried network call must never
      double-post"). Caches the response per key so a retried request returns the cached result instead of
      re-executing.
  - **Proved the idempotency guarantee, not just trusted the logic**: the headline test sends a request,
    caches its response, then sends a retried request with the SAME key and asserts it returns the cached
    response without re-executing. Separate tests cover: missing header → 400, different key → executes
    fresh, and a failed (non-2xx) response is NOT cached so it can be retried.
  - **Refactored SystemController onto the base** to prove the conventions work on real running code, not
    just in tests — it now inherits `PlatformApiController` instead of `ControllerBase` directly. The
    route `api/v1/system/status` still resolves correctly (via the base's `api/v1/[controller]` pattern),
    and all endpoints (status, greeting, health) return the same payloads as before. No behavior change to
    existing endpoints — the refactor only adds the shared base.
  - 24 new tests, 123 total passing across the whole project. Frontend Arabic guardrail still green (no
    frontend changes).
- Files touched: `src/Platform/Platform.Api/*` (full new project: Platform.Api.csproj, PagedResult.cs,
  ODataQuery.cs, ApiErrorEnvelope.cs, IdempotencyKeyAttribute.cs, PlatformApiController.cs, README.md),
  `tests/UnitTests/Platform.Api.Tests/*` (Platform.Api.Tests.csproj, ODataQueryTests, PagedResultTests,
  ApiErrorEnvelopeTests, IdempotencyKeyAttributeTests), `erp-platform.sln` (two projects added),
  `src/Gateway/Gateway.Api/Gateway.Api.csproj` (project ref), `src/Gateway/Gateway.Api/Controllers/
  SystemController.cs` (inherits PlatformApiController)
- Known gap, disclosed not hidden: the `$filter` expression engine (parsing `Amount gt 1000` into a real
  predicate) is deferred — it needs a proper grammar (OData ABNF / ANTLR) and a real list endpoint to drive
  it; the query structure + validation is built now so the contract is stable. The idempotency cache is
  in-memory (same swap-for-real-store pattern); a real deployment needs Redis/DB so it survives restarts
  and works across multiple Gateway instances. OpenAPI 3.1 contract-first spec generation, `$batch`/bulk
  operations, webhooks/event subscriptions, and header-based minor version negotiation are all deferred —
  documented in Platform.Api/README.md.
- Next: the last Phase 0 piece — `Platform.Configuration` (settings without needing new code: multi-level
  override hierarchy, business rule engine, feature flags). After that, Phase 0 is complete and Phase 1
  (Master Data + Finance Core) begins.

### 2026-07-13 — Built Platform.UI design system (tokens + reusable components, Vite alias stage)
- Agent: ZCode (builtin:zai-coding-plan/GLM-5.2)
- Phase: Phase 0 — Platform Foundation
- Status: Completed
- What changed:
  - **Built Platform.UI** — the in-house design system (docs/architecture/02-business-object-model.md
    #2–4): design tokens + reusable Dynamics-365-referenced components, consumed by Apps.Shell via a
    `@platform/ui` import path. In plain terms: the shell's ad-hoc styling and components (which worked but
    lived inline in the app and couldn't be reused by a second app) are now a real, self-contained design
    system that any future frontend app can consume. The pieces:
    - **Design tokens** (`tokens/design-tokens.css`): the single source of truth for color, spacing,
      typography, and radius — all `--pi-*` CSS variables. Extracted + expanded from Apps.Shell's original
      7 color-only tokens; the spacing/typography/radius scales that were hardcoded per-rule are now named
      tokens. Theme-able (light/dark today, per-tenant branding later) by redefining the same variables.
    - **ShellBar** + **NavigationPane**: extracted from Apps.Shell's originals and generalized —
      data-driven (the app passes translated strings, nav tree, and language options as props; the
      components have zero dependency on any translation system).
    - **ActionPane** (new): the Dynamics 365 command bar (§2.1). Stateless — the app passes only the actions
      available right now (driven by the document's FSM state + the user's security role). This is what
      makes a new BO transition surface its button everywhere automatically.
    - **FastTabs** (new): vertically stacked, collapsible panels where several can be open at once (§2.1:
      "FastTabs, not tabs"). Real `<button>` headers with `aria-expanded`/`aria-controls` for keyboard +
      screen-reader access (WCAG 2.1 AA, per doc 02 #4).
  - **Hard rule enforced: Platform.UI never imports from Apps.Shell.** It's self-contained — no dependency
    on any app's i18n, routing, or state. This keeps promoting it to a real npm package later a config
    change, not a rewrite (documented in its README).
  - **Two corrections caught by the user before they shipped**, both real bugs:
    1. The relative path from Apps.Shell to Platform.UI was `../../../` (overshooting to the repo root)
       instead of `../../` — would have broken both the TS checker and the Vite bundler. Fixed in both
       tsconfig and vite.config.
    2. The FastTabs chevron was initially a "never flip" physical transform — wrong. A directional disclosure
       arrow MUST mirror for RTL (point right collapsed in LTR, point left in Arabic). Fixed by building the
       triangle with `border-inline-start` (a logical property that auto-mirrors with `dir`), so collapsed
       it points toward the content's start side in both languages, and expanded it rotates to point down
       (direction-neutral) in both. Same category of bidi bug as the outbox counter reordering caught in the
       Platform.Events session.
  - **No hardcoded strings anywhere in Platform.UI** — including aria-labels. Every screen-reader-facing
    label (language switcher group, nav landmark, action toolbar) comes in as a prop, translated by the
    consumer via its own `t()`. The FastTabs button needs no separate label — its accessible name is the
    visible tab title text.
  - **Apps.Shell refactored to consume Platform.UI**: ShellBar + NavigationPane now imported from
    `@platform/ui` (old local copies deleted); System Status page restructured with an ActionPane (functional
    Refresh button) + FastTabs organizing the facts into General / Events & audit / Localization tabs.
  - **Extended the frontend Arabic guardrail to scan Platform.UI too** — `check-no-hardcoded-arabic.mjs`
    now scans both Apps.Shell's `src/` and `src/Platform/Platform.UI/`, so a hardcoded string can't slip in
    through the design system any more than through an app component.
  - Verified live: backend returns `audit.chainValid: true`, frontend serves with `@platform/ui` resolving
    through Vite, 99 .NET tests + full `npm run build` + Arabic guardrail all green.
- Files touched: `src/Platform/Platform.UI/*` (tokens/design-tokens.css, types.ts, components/{ShellBar,
  NavigationPane, ActionPane, FastTabs}.tsx, components/components.css, index.ts, README.md),
  `src/Apps/Apps.Shell/vite.config.ts` (alias + react dedupe), `src/Apps/Apps.Shell/tsconfig.app.json`
  (paths + include), `src/Apps/Apps.Shell/src/main.tsx` (Platform.UI CSS imports),
  `src/Apps/Apps.Shell/src/App.tsx` (consume Platform.UI, data-driven props),
  `src/Apps/Apps.Shell/src/index.css` (tokens removed, resets point at --pi-*),
  `src/Apps/Apps.Shell/src/App.css` (token refs updated, component styles removed),
  `src/Apps/Apps.Shell/src/pages/SystemStatusPage.tsx` (ActionPane + FastTabs restructure),
  `src/Apps/Apps.Shell/src/i18n/content.ts` (7 new keys: 3 tab titles, refresh action, 3 aria labels),
  `src/Apps/Apps.Shell/scripts/check-no-hardcoded-arabic.mjs` (now scans Platform.UI),
  deleted `src/Apps/Apps.Shell/src/components/ShellBar.tsx` + `NavigationPane.tsx` (moved to Platform.UI)
- Known gap, disclosed not hidden: Platform.UI is at the Vite-alias stage — not a real npm package yet
  (no package.json, no independent build). Promoting it to a workspace package later is a config change
  (documented in its README), not a rewrite, because it's self-contained now. The List+Details form template
  (§2.1) and Workspace component (§2.2) are deferred — they need a real business object to render, built
  when Phase 1's first module lands. Accessibility CI checks (axe-core) not wired yet; components are
  authored to WCAG 2.1 AA but not yet verified by an automated scanner.
- Next: continue Phase 0 with `Platform.Api` (API conventions: how screens talk to the server), then
  `Platform.Configuration` (settings without needing new code). These are the last two Phase 0 pieces.

### 2026-07-13 — Built Platform.Audit (permanent, tamper-evident change history)
- Agent: ZCode (builtin:zai-coding-plan/GLM-5.2)
- Phase: Phase 0 — Platform Foundation
- Status: Completed
- What changed:
  - **Built Platform.Audit** — the permanent, tamper-evident change history every module will feed, per
    docs/architecture/03-platform-services.md #5. In plain terms: every create / update / status-transition /
    delete-attempt on a record is captured (who, what field-level before/after, when, from where, why), and
    the records are hash-chained so a retroactive edit is computationally detectable — the same spirit as
    SAP's change-document framework, and a real requirement for financial/statutory defensibility in Saudi
    Arabia (ZATCA, external auditors). The pieces:
    - `AuditEntry` captures the full who/what/when/from-where/why set, carrying `FieldValueChange`s for the
      before/after of each changed field.
    - `AuditHasher` is the tamper-evidence core: SHA-256 over a deterministic canonical string of the
      entry's content plus the PREVIOUS entry's hash. That link is what makes tampering detectable — change
      any old field and its hash changes, which breaks every later entry's PreviousHash link.
    - `IAuditLog`/`InMemoryAuditLog` is append-only (no Update/Delete by design — §5: append-only). `Append`
      computes the hash from the current tail. `VerifyChain()` recomputes every hash from the genesis entry
      forward and returns the first entry that disagrees.
    - `IAuditRecorder`/`AuditRecorder` is the friendly facade modules call (RecordCreate,
      RecordFieldUpdate, RecordStatusTransition, RecordDeleteAttempt) — same single-entry-point pattern as
      IIntegrationEventPublisher for events; modules never call IAuditLog directly.
  - **Proved the tamper-evidence guarantee, not just trusted the logic**: the headline test appends a few
    entries, then mutates a stored record's content via reflection (standing in for a hostile in-place DB
    UPDATE — exactly the threat the chain defends against), re-runs VerifyChain, and asserts it returns the
    tampered entry. Separate tests cover tampering with the first record, the last record, and a swap of two
    records (a reordering that breaks the PreviousHash links even though no record's own content changed).
  - **Proved the whole pipeline works in the running application**, not just in tests: Gateway.Api records
    one real, permanent operational audit entry at boot ("Application started"), and the System Status page
    now shows `audit.entries` + `audit.chainValid` (re-verified on every status read — a broken chain would
    be a serious integrity signal an operator would see, not a silent failure).
  - 20 new backend tests, 99 total passing across the whole project. Frontend Arabic guardrail still green
    (Platform.Audit has no display text in the kernel; the one new frontend label lives in content.ts as
    required).
- Files touched: `src/Platform/Platform.Audit/*` (full new project: AuditAction, FieldValueChange,
  AuditEntry, Hashing/AuditHasher, IAuditLog, InMemoryAuditLog, IAuditRecorder, AuditRecorder, README.md),
  `tests/UnitTests/Platform.Audit.Tests/*` (Platform.Audit.Tests.csproj, AuditHasherTests,
  InMemoryAuditLogTests, TamperEvidenceTests, AuditRecorderTests), `erp-platform.sln` (two projects added),
  `src/Gateway/Gateway.Api/Gateway.Api.csproj` (project ref), `src/Gateway/Gateway.Api/Program.cs` (DI +
  boot-time audit entry), `src/Gateway/Gateway.Api/Controllers/SystemController.cs` (audit in status payload
  + Platform.Audit in kernelServicesWired), `src/Apps/Apps.Shell/src/api/systemApi.ts` (AuditStatus type),
  `src/Apps/Apps.Shell/src/i18n/content.ts` (3 new keys: audit label + chain valid/broken), 
  `src/Apps/Apps.Shell/src/pages/SystemStatusPage.tsx` (audit row with bidi-isolated count)
- Known gap, disclosed not hidden: `InMemoryAuditLog` proves the chain mechanics; a real deployment needs an
  actual append-only audit table with no UPDATE/DELETE grants at the DB-role level (§5), with the audit write
  committed in the same transaction as the business change. Automatic audit at every lifecycle transition
  isn't wired yet because no business module exists yet to drive it; Gateway.Api records the one boot entry
  to prove the live pipeline. Retention/archiving and compliance exports (§5) need Modules.Reporting, not
  built yet.
- Next: continue Phase 0 with the fuller `Platform.UI` design system, then `Platform.Api` conventions and
  `Platform.Configuration`. (Phase 0's remaining pieces after Audit are these three.)

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
