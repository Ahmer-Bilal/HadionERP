# HadionERP — Project Progress Log

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
| Phase 1 — Master Data + Finance Core | **Exit criteria met** — all 5 Master Data pieces done; Modules.Finance has GL Journal Entry + AP Invoice, both post/reverse-able with full audit trail. AR/Cash-Bank, Document Splitting, Parallel Ledgers, Budget Control, Results Analysis/CO-PA remain as later Finance depth, not required for Phase 1's exit bar | 2026-07-14 |
| Phase 2 — Procurement | **Exit criteria met** — full procure-to-pay cycle (BusinessRoles/Vendor Prequal → PR → RFQ → PO → GRN) built, tested, and live-verified, with a working 3-way match against AP closing the loop. Real Budget Control enforcement and a real line-by-line invoice match remain deferred Finance/Procurement depth, not required for Phase 2's exit bar | 2026-07-15 |
| **Checkpoint** — UI/Visual Density Pass | **Paused by explicit user instruction** — teal identity (design-tokens.css) + `Platform.UI.SplitView` are live and retrofitted on `PurchaseOrdersPage`/`BusinessPartnersPage`, but the user judged this insufficient to compete visually with Fiori/Dynamics (color tokens + one layout mechanic, no icons/status pills/KPIs/avatars — see `feedback_visual_richness_gap` in memory) and told the AI to stop touching it and move to roadmap phases instead. Left as-is, not reverted. Resume only when the user explicitly asks for the visual pass again, with real surface richness this time | 2026-07-15 |
| Phase 3 — Construction & Project Management | In Progress — `Modules.ProjectManagement`'s WBS foundation (Project + WBS Element) built, tested, and live-verified. Networks/Activities and `Modules.Construction` itself not started | 2026-07-15 |
| **Checkpoint** — Lookup Data / Admin Panel | **Completed** — a real, admin-configurable picklist engine (`LookupType`/`LookupValue`, `LookupService`, `LookupsController`) replaced the hardcoded `BusinessRoleType`/`AddressType` enums and unvalidated `Country`/`UnitOfMeasure` free text; a genuine multi-page Admin Panel (`LookupDataPage.tsx`, inline-editable SAP-style grids, one nav entry per type) lets an administrator add/edit/deactivate/delete lookup values and even define brand-new lookup types, with in-use delete protection. Live-verified end-to-end, EN+AR | 2026-07-15 |
| **Checkpoint** — Architecture Gap Audit | **Completed** — full SAP/Dynamics-vs-HadionERP gap audit performed, evidence-grounded (file paths/grep results, not speculation), in two parts (platform capabilities + core data model/missing modules). 23 gap findings total, severity-rated; findings live in `ARCHITECTURE-AUDIT.md` at repo root, mapped onto existing/new roadmap phases in `docs/architecture/06-roadmap.md`'s "Architecture Gap Audit & Platform Hardening" checkpoint. Two findings rated Blocking: Authentication (§1, now resolved — see next row) and AP Payment Recording (Part 2 §16, still open — no way in this system to record that an invoice was ever paid) | 2026-07-15 |
| **Checkpoint** — Real Authentication & Identity | **Completed** — closes audit §1/§3. `Modules.Identity` (JWT bearer auth, persisted Users, global default-deny) replaced every hardcoded actor literal solution-wide; role assignment now runs real Segregation of Duties conflict checking for the first time ever (block → override-with-reason → succeed, live-verified). Live-verified end-to-end, EN+AR, zero regressions (22 test projects) | 2026-07-15 |
| Phase 4 — HR & Payroll | Not Started | — |
| Phase 5 — Reporting, Analytics & Mobile | Not Started | — |
| Phase 6 — Extensibility Ecosystem & Advanced Capabilities | Not Started | — |

(Phase definitions and exit criteria: `docs/architecture/06-roadmap.md`)

---

## Entry Log (newest first)

### 2026-07-15 — Real Authentication & Identity built (closes ARCHITECTURE-AUDIT.md Part 1 §1 and §3)

- Agent: Claude Sonnet 5
- Phase: Checkpoint — Real Authentication & Identity
- Status: Completed
- What changed: The user chose to close the audit's top-priority Blocking finding before moving to new
  feature phases ("i think better to improve before moving forward"). Entered plan mode given the scope
  (security-critical, multi-file, several valid architectural approaches) and got explicit plan approval
  before writing code. Built a new `Modules.Identity` module — real username/password authentication via
  JWT bearer tokens, a persisted Users admin surface, and a global default-deny authorization policy —
  replacing every hardcoded actor literal (`"system/ui"`/`"system/approver"`/`"system/startup"`, 32 usages)
  across the entire solution.
  - **Design**: JWT bearer tokens, not cookies (frontend/backend are different origins in dev, and bearer
    tokens sidestep CORS-credential complexity). Lean `PasswordHasher<User>` (from
    `Microsoft.Extensions.Identity.Core`), not the full ASP.NET Core Identity framework — matches
    `Platform.Workflow`'s own documented precedent of a hand-built engine over unneeded framework surface.
    `User` is deliberately not a `BusinessObject` (no Draft/Approve lifecycle) — same reasoning as this
    session's earlier `LookupType`/`LookupValue`: real user administration is immediate-effect, gated by a
    security role, not a workflow. `Username` is exactly the `actor: string` value every Application-layer
    service across every module already accepted — **zero changes to any Application-layer service**; only
    what produces that string changed.
  - **The single biggest structural win**: role assignment (`UserService.AssignRoleAsync`) now calls
    `Platform.Security.Sod.ISodEngine.FindUnresolvedConflicts` — the first live call this already-built,
    already-tested engine has ever received in this codebase's history (every module registered real SoD
    conflict rules since Phase 1, but nothing could ever check them without a role-*assignment* action to
    guard). A conflict throws `SodConflictException` (mapped to a structured 409) unless the caller supplies
    an override reason, which grants a logged exception via the already-existing `ISodExceptionLog.Grant` —
    the real SAP GRC "risk acceptance" pattern. Live-verified via curl: assigning
    `MasterData.BusinessPartner.Maintainer` then `MasterData.ApproveBusinessPartner` to the same user
    correctly 409s with the registered rule's own reason text; retrying with an override reason succeeds.
  - **Bootstrap seeding** (`IdentitySeeder`, mirrors `LookupSeeder`'s idempotent pattern): creates one
    `admin` user with every currently-registered role on first run if the `users` table is empty, so the
    system is immediately usable after this change with no separate manual step — confirmed by restarting
    Gateway.Api from a clean state and logging in immediately.
  - **All 14 pre-existing controllers retrofitted**: `PlatformApiController` (in `Platform.Api`, shared
    across every module) gained a `CurrentActor` property reading the validated JWT's username claim;
    `MaintainerActor`/`ApproverActor`/`ReviewerActor`/`AdministratorActor` hardcoded constants removed from
    every controller in `Modules.MasterData`/`Modules.Finance`/`Modules.Procurement`/`Modules.ProjectManagement`,
    replaced with `CurrentActor`. `BusinessPartnersController`'s doc comment (which specifically explained
    the old two-hardcoded-actor SoD workaround) rewritten to describe the real mechanism.
  - **Frontend**: `authApi.ts`/`AuthContext`/`LoginPage.tsx`; every one of the 15 existing `api/*.ts` files
    retrofitted (via a small Python transform script, since the fetch-call shapes were mechanically
    consistent across ~120 call sites) to attach `Authorization: Bearer <token>` through a shared
    `authHeaders()` helper. `Platform.UI.ShellBar` gained optional `currentUserLabel`/`onLogout` props
    (only rendered when provided, same optional-prop pattern as `tagline`). New `UsersPage.tsx` (list/
    create/deactivate/reset-password, a Roles FastTab surfacing SoD conflicts inline with a required
    override-reason field) under a new "Users" nav area alongside "Lookup Data."
- Verified: 13 new unit tests (`UserServiceTests` — hash/verify round-trip, duplicate-username rejection,
  authorization denial, deactivation blocking authentication, SoD conflict blocking then override-succeeding,
  role removal, password reset) + 4 new integration tests against real PostgreSQL. **22 test projects pass
  solution-wide, zero regressions** — confirmed by design: every pre-existing Application-service test
  builds its own fake `IActorRoleAssignmentStore` directly, bypassing HTTP/controllers/JWT entirely, exactly
  as predicted before writing any code. Live `curl` exercise: unauthenticated request to a protected
  endpoint correctly 401s; bootstrap admin login returns a real JWT carrying every role as claims; the same
  token against a protected endpoint returns 200; creating a Business Partner with the real token attributes
  it to `"admin"` in the response (`createdBy`), not `"system/ui"`; the full SoD block→override→succeed
  sequence above. Live Playwright pass (screenshots, zero console errors) in both English and Arabic: login
  page, authenticated shell showing "Logged in as System Administrator · Logout" (RTL-correct in Arabic), an
  existing page (Business Partners) still working end-to-end with a real session, the Users admin page
  (list, create, detail FastTabs), logout correctly returning to the login page.
- `ARCHITECTURE-AUDIT.md` §1 and §3 marked Resolved with a link to this entry (per the file's own "add a
  Resolved note against the specific bullet" convention — Part 1's findings are otherwise left unedited).
  `docs/architecture/06-roadmap.md`'s checkpoint section updated to match.
- Files touched: `src/Modules/Modules.Identity/**` (new module — `Domain/User.cs`, `UserRole.cs`;
  `Application/IUserRepository.cs`, `UserDto.cs`, `IdentitySecurity.cs`, `UserService.cs`,
  `SodConflictException.cs`; `Infrastructure/IdentityDbContext.cs`, `EfUserRepository.cs`,
  `EfActorRoleAssignmentStore.cs`, `JwtTokenService.cs`, `IdentitySeeder.cs`,
  `DesignTimeDbContextFactory.cs`, migration `20260715120755_InitialCreate`; `Api/AuthController.cs`,
  `UsersController.cs`; `README.md`); `src/Platform/Platform.Security/ITokenService.cs` (new);
  `src/Platform/Platform.Api/PlatformApiController.cs` (`CurrentActor`); `src/Gateway/Gateway.Api/Program.cs`
  (JWT auth, global `AuthorizeFilter`, Swagger bearer scheme, DI wiring, bootstrap seeding call), `.csproj`
  (new package + project references); 14 existing controllers across
  `Modules.MasterData`/`Modules.Finance`/`Modules.Procurement`/`Modules.ProjectManagement` (actor retrofit);
  `src/Apps/Apps.Shell/src/api/authApi.ts`, `usersApi.ts` (new), all 15 pre-existing `api/*.ts` files + `systemApi.ts`
  (auth headers), `AuthContext.tsx` (new), `pages/LoginPage.tsx`, `pages/UsersPage.tsx` (new), `App.tsx`
  (auth gate, nav, routing), `main.tsx` (`AuthProvider`), `App.css` (login page styles), `i18n/content.ts`
  (`auth.*`/`users.*`/`nav.users*` keys, EN+AR); `src/Platform/Platform.UI/components/ShellBar.tsx`,
  `components.css` (logout slot); `tests/UnitTests/Modules.Identity.Tests/**` (new),
  `tests/IntegrationTests/Modules.Identity.IntegrationTests/**` (new); `ARCHITECTURE-AUDIT.md`,
  `docs/architecture/06-roadmap.md` (Resolved notes).
- Next: the audit's remaining Blocking finding — AP Payment Recording & Cash/Bank Management
  (`ARCHITECTURE-AUDIT.md` Part 2 §16, no way in this system to record that an invoice was ever paid) — or
  continue Phase 3 (`Modules.Construction`, Networks/Activities for `Modules.ProjectManagement`) per the
  existing roadmap; the user should choose which, per this session's own established pattern of not assuming
  an order between two Blocking findings.

### 2026-07-15 — Architecture audit Part 2: core data model & module completeness

- Agent: Claude Sonnet 5
- Phase: Checkpoint — Architecture Gap Audit (Part 2)
- Status: Completed
- What changed: The user asked for a second pass distinct from Part 1's cross-cutting platform findings —
  "i also want other things which are missing like modules and core data... that sap/dynamic have in module
  1 but we are missing," explicitly not just security/auth. Used a second research fork to ground the actual
  current field lists on `BusinessPartner`/`GLAccount`/`Item`/`CostCenter`/`TaxCode`/`JournalEntry`/
  `APInvoice` (reading the real code, not guessing), then compared against real SAP FI/CO/MM and Dynamics
  365 F&O field-level completeness. Appended as "Part 2" to `ARCHITECTURE-AUDIT.md` (9 new numbered findings,
  §15-23), preserving Part 1 unedited per the file's own "add a new dated section, don't silently edit past
  findings" convention.
  - **Single most important finding**: AP Payment Recording & Cash/Bank Management (§16) — there is
    currently no way anywhere in this system to record that an AP invoice was actually paid.
    `APInvoice.Post()` only ever posts Debit Expense/Credit Payable; nothing in the entire codebase ever
    debits Payable/credits a bank account (confirmed by a solution-wide grep — zero hits for `Payment`/
    `Disbursement`/`BankAccount`/`HouseBank` as class names anywhere). The AP cycle as built today stops one
    real-world step short of complete. Rated Blocking — the highest-priority *data-model* gap in the whole
    audit (Part 1's Authentication finding remains the highest-priority *platform* gap overall).
  - Business Partner master data (§15) — no Payment Terms, Bank/IBAN, Credit Limit, Reconciliation Account,
    or Withholding Tax field anywhere on `BusinessPartner`; this is why `APInvoiceService` requires
    `PayableAccountId` picked by hand on every invoice instead of defaulting from the vendor — the
    underlying master-data field to default *from* doesn't exist, a materially bigger gap than
    `Modules.Finance/README.md`'s existing "not yet defaulted" framing suggested.
  - GL Document Type concept (§17) and Profit Center/Internal Order (§18) — both genuinely missing, but
    Internal Order specifically flagged as likely already subsumed by `Modules.ProjectManagement`'s WBS
    Element (`IsAccountAssignmentElement` does structurally the same job) — worth confirming intentionally
    when Phase 3 builds real cost-posting against WBS, not building both.
  - Withholding Tax/Tax Jurisdiction Code (§19) — real KSA requirement, low priority given KSA's flat VAT.
  - **Three entire modules confirmed absent from both code and roadmap**: Fixed Assets (§20 — genuinely
    construction-relevant: cranes/trucks/generators/formwork are real capital assets for this business),
    Inventory/Warehouse Management (§21 — `Item.cs`'s own doc comment already presupposes an Inventory
    module that doesn't exist; Procurement's GRN records receipt but never increments any stock balance
    anywhere), Plant/Equipment Maintenance (§22 — a distinct concern from Fixed Assets' depreciation
    accounting, naturally paired with it).
  - Real Estate/Site-Land Management (§23) flagged as an open question, not a confirmed gap — whether it's
    needed depends on the user's own land/site-ownership model, not guessed into the roadmap.
- `docs/architecture/06-roadmap.md`'s "Architecture Gap Audit & Platform Hardening" checkpoint section
  extended with a "Part 2" subsection mapping each new finding to a phase — explicit that §16/§15 (AP
  payment recording) is Phase 1 depth that should land before real Finance production use, and that Fixed
  Assets/Plant Maintenance/Inventory are new roadmap items (recommended as a checkpoint between Phase 3 and
  Phase 4, or folded into Phase 4) that weren't named anywhere before this audit.
- Files touched: `ARCHITECTURE-AUDIT.md` (Part 2 section, §15-23 + summary table), `docs/architecture/06-roadmap.md`
  (checkpoint section extended), `PROGRESS.md` (this entry).
- Next: no code changes in this entry — documentation/planning only. Highest-leverage next real
  implementation work per this audit's own severity ratings is either real Authentication & Identity (Part 1
  §1, Blocking, platform-wide) or AP Payment Recording & Cash/Bank Management (Part 2 §16, Blocking,
  Finance-specific) — both are rated Blocking for different reasons (one blocks trustworthy attribution
  everywhere, the other blocks the AP cycle from ever actually completing) and the user should pick which to
  tackle first rather than this session assuming an order.

### 2026-07-15 — Trade lookup split into three role-scoped categories; docs reconciled against the audit

- Agent: Claude Sonnet 5
- Phase: Checkpoint — Lookup Data / Admin Panel (follow-up correction)
- Status: Completed
- What changed: The user caught a real design shortfall in the just-shipped Lookup Data checkpoint — "Trade"
  had been seeded as one flat, undifferentiated 20-value list, even though this project's own
  `docs/architecture/06-roadmap.md` (written well before this session) already explicitly describes trades as
  three separate taxonomies per role family (Subcontractor → Electrical/Concrete/Steel Structure/...,
  Supplier → Steel/Cement/MEP Materials/..., Consultant → Structural/Architectural/MEP Design/...). Also
  caught: the Trade *field* itself was still a plain free-text `<input>` in `BusinessPartnersPage.tsx`,
  never actually wired to any lookup suggestion despite the earlier session's README claiming it was. Fixed
  both: replaced the single `Trade` lookup type with `SubcontractorTrade`/`SupplierTrade`/`ConsultantTrade`
  (each its own admin-panel heading/nav item, matching how the roadmap already separates them), and wired
  `BusinessPartnersPage.tsx`'s Trade field to a role-scoped HTML `<datalist>` — selecting "Subcontractor"
  shows Subcontractor trade suggestions, "Supplier" shows Supplier trade suggestions, still free-text-entry
  underneath per the roadmap's own deliberate "suggestion, not enforced" design.
  - Confirmed the underlying design (immediate-effect, single-privilege-gated CRUD; in-use delete
    protection; the enum-vs-lookup-table split) matches real SAP (Domain/Value-Table maintenance via SM30,
    foreign-key-checked deletion) and Dynamics 365 (the Option-Set-vs-reference-table-entity distinction) —
    not just an invented pattern, per user request to verify against the reference products.
  - Cleaned up stale dev/test data: the old flat "Trade" lookup type/values (orphaned by the split) and a
    leftover "PWTest" Country value from an earlier Playwright verification pass were removed from the dev
    database directly.
  - Reconciled existing docs against `ARCHITECTURE-AUDIT.md`'s findings per explicit user instruction ("update
    the existing documents if they are not aligning with the audit report"): `Platform.Localization/README.md`,
    `Platform.Workflow/README.md`, and `Platform.Security/README.md` each got a new disclosure noting which of
    their described capabilities are real-but-not-yet-consumed-anywhere (Hijri calendar, ZATCA QR generation,
    Delegation, FieldLevel/RowLevel), cross-referencing `ARCHITECTURE-AUDIT.md` instead of silently reading as
    more complete than they are.
  - One real regression caught only by the full test suite (not by any check run during development): the
    new `LookupSeeder.cs` seed data (real Arabic country/role/trade names) tripped
    `Platform.ArchitectureTests`'s "no hardcoded Arabic string outside the allow-list" guard — correctly so,
    since that test can't distinguish seed business data from hardcoded UI copy by itself. Added
    `LookupSeeder.cs` to the test's explicit `AllowedFiles` list with a written justification (the same
    "deliberate, reviewed decision" pattern the test's own doc comment calls for), not a workaround.
- Verified: full solution test suite (20 test projects) green with zero failures after the fix. Live `curl`
  exercise confirmed the three new trade types seed correctly (15/10/8 values) and the old flat type is gone.
  Live Playwright pass: the admin hub shows 7 correctly-separated lookup types (not the old 5 with one
  mixed-together "Trade"), and the Business Partner create form's Trade suggestions genuinely change based on
  the selected role (verified both Subcontractor's and Supplier's distinct suggestion lists render correctly),
  zero console errors.
- Files touched: `src/Modules/Modules.MasterData/Infrastructure/LookupSeeder.cs` (Trade → three role-scoped
  types), `Domain/BusinessRole.cs` (doc comment), `src/Apps/Apps.Shell/src/App.tsx` (nav/routing — one Trades
  nav item → three), `i18n/content.ts` (`nav.lookup*Trades` keys), `pages/BusinessPartnersPage.tsx` (role-scoped
  Trade datalist); `tests/ArchitectureTests/Platform.ArchitectureTests/NoHardcodedTranslatableTextTests.cs`
  (allow-list entry); `src/Platform/Platform.Localization/README.md`, `Platform.Workflow/README.md`,
  `Platform.Security/README.md` (audit-alignment disclosures).
- Next: none — this closes out the Lookup Data checkpoint and the audit-alignment request together. Continue
  Phase 3 or the audit's own prioritized next step (real Authentication & Identity) per
  `ARCHITECTURE-AUDIT.md`'s summary table.

### 2026-07-15 — Comprehensive SAP/Dynamics architecture gap audit performed, persistent audit file created

- Agent: Claude Sonnet 5
- Phase: Checkpoint — Architecture Gap Audit
- Status: Completed
- What changed: Per explicit user instruction ("act as real architecture of sap... audit the system... tell
  me what is missing... check others too. list them in roadmap and update all relevant docs. keep the audit
  file too"), performed a full, evidence-grounded comparison of HadionERP against real SAP S/4HANA and
  Dynamics 365 F&O capabilities. Used a research fork to gather hard evidence (grep results, specific file
  paths — not speculation) across 10 categories before writing any findings. Created `ARCHITECTURE-AUDIT.md`
  at the repo root (same tier as `ARCHITECTURE.md`/`PROGRESS.md`) with 14 rated gap findings:
  1. **Authentication & Identity** — Blocking. Zero real auth exists anywhere (no auth package in any
     `.csproj`, no `[Authorize]` attribute anywhere, `UseAuthorization()` is a no-op since nothing populates
     `HttpContext.User`); every actor across the whole solution is one of three hardcoded literals
     (`"system/ui"`/`"system/approver"`/`"system/startup"`). This is the top-priority finding — it's why two
     already-real platform capabilities are currently inert (see next two items).
  2. **Segregation of Duties enforcement** — Structural. `ISodEngine`/`SodEngine` and every module's
     registered conflict rules are real and unit-tested, but `FindConflicts` is never called from any live
     request path (it's meant to run at role-*assignment* time, and there's no assignment UI yet).
  3. **Delegation** — Structural. `IDelegationRegistry` is genuinely wired into
     `RoleBasedWorkflowEligibilityService`'s live eligibility check, but `Program.cs` registers an empty
     `InMemoryDelegationRegistry` with no API/UI to ever populate it.
  4. **Escalation** — Missing/orphaned. `Platform.Workflow/Escalation/` has zero references anywhere outside
     its own two files, not even wired into `WorkflowEngine`.
  5. **Row-Level/Field-Level Security** — Structural/dead code. `Platform.Security/FieldLevel/`/`RowLevel/`
     exist with real types but are referenced nowhere outside their own directory.
  6. **Amount-conditioned approval matrices** — Depth gap. Phase 2's exit criteria named "configurable
     approval matrix" but every workflow built so far (across every module) is role-based only, fixed
     regardless of document value — the engine's `AttributeConstraints` condition-gating primitive already
     exists (proven by Vendor Prequalification's 5-step chain) and has simply never been pointed at an
     amount field.
  7. **ZATCA e-invoicing** — Structural. `Platform.Localization/Zatca/*` exists but is never wired into
     `APInvoice`/`APInvoiceService` — no real QR code or invoice XML is ever generated, despite Phase 1's
     original roadmap scope naming this as in-scope.
  8. **Hijri calendar** — Structural. `Platform.Localization/Calendar/*` exists but is never referenced by
     any UI date field (all 7 date inputs across `Apps.Shell` are plain Gregorian HTML date inputs).
  9. **Multi-currency** — Missing. Zero `Currency` field on any Domain entity anywhere; the whole system
     implicitly assumes SAR.
  10. **Multi-company/legal entity** — Missing. No `Company`/`LegalEntity` entity anywhere; `"C001"` is a
      hardcoded literal in ~15 places.
  11. **Fiscal Year/Period management** — Missing. No period-open/close/lock concept at all; fiscal year is
      just `DateTimeOffset.UtcNow.Year` passed into number-range calls.
  12. **Notifications & output management** — Missing entirely, not even a stub (zero email/PDF/print code
      anywhere in `src/`).
  13. **Reporting/Analytics/Extensibility/Integration** — Missing, but this confirms rather than surfaces a
      new gap: `Platform.Reporting`/`Platform.Extensibility`/`Platform.Integration` are all README-only
      placeholders, and Phases 5/6 already correctly scope this work — nothing has been started early that
      needs correcting.
  14. **Attachments** — Depth gap. Real, correctly-wired Postgres `bytea` storage (not dead code — proven
      live by Vendor Prequalification's attachment cycle), but not real object storage, and no virus
      scanning anywhere.
  Also explicitly documented what this audit deliberately does NOT flag as a gap (the just-closed
  Configurable Lookup Data engine, Trade's deliberate non-enforcement, the module-dependency/Contracts-
  package/BO-lifecycle architecture itself) — to avoid the roadmap being over-scoped with items that are
  working exactly as designed.
- `docs/architecture/06-roadmap.md` updated with a new "Checkpoint — Architecture Gap Audit & Platform
  Hardening" section mapping each finding onto an existing or new roadmap phase (a new "Phase 0.5 — Identity
  & Access" is implied for the Authentication finding; the rest slot into Phase 1 depth, next Procurement/
  Finance approval work, next UI pass, Phase 4, Phase 5, or Phase 6 as appropriate) — explicit that none of
  this blocks Phase 3 (in progress) from continuing.
- Files touched: `ARCHITECTURE-AUDIT.md` (new, repo root), `docs/architecture/06-roadmap.md` (new checkpoint
  section), `PROGRESS.md` (this entry + Phase Status Summary row).
- Next: this was a documentation/planning checkpoint, not an implementation one — no code changes. The
  highest-leverage next real implementation work per this audit's own severity ratings is real Authentication
  & Identity (§1 of `ARCHITECTURE-AUDIT.md`), since it's rated Blocking and unblocks two already-built
  platform capabilities for free. Otherwise, continue Phase 3 (`Modules.Construction`, Networks/Activities for
  `Modules.ProjectManagement`) per the existing roadmap.

### 2026-07-15 — Configurable Lookup Data engine + Admin Panel built (checkpoint complete)

- Agent: Claude Sonnet 5
- Phase: Checkpoint — Lookup Data / Admin Panel
- Status: Completed
- What changed: The user explicitly instructed (repeatedly, escalating) that hardcoded lookup/classification
  data — "customers, vendors — these words" — must become admin-editable like real SAP domain-value
  maintenance or Dynamics 365 Option Sets, with a real multi-page Admin Panel, not a token gesture. Built:
  1. **`Modules.MasterData.Domain.LookupType`/`LookupValue`** — deliberately not `BusinessObject`s (no
     Draft/Approve lifecycle; immediate-effect, single-privilege-gated, matching how real ERP picklist
     maintenance actually works). `LookupService` gives full CRUD on both levels, including letting an
     administrator define an entirely new lookup *category* from scratch (not just add values to existing
     ones) — the most general form of "keep option so we can add and change."
  2. **Retrofitted real hardcoded data onto it**: `BusinessRole.RoleType` and `BusinessPartnerAddress.AddressType`
     changed from C# enums to lookup-validated strings (no data migration needed — the EF column was already
     `character varying`). `BusinessPartnerAddress.Country` (previously free text, zero validation) and
     `Item.UnitOfMeasure` (previously free text, a disclosed Phase 1 gap) are now validated the same way.
     `BusinessRole.Trade` deliberately stays an unenforced suggestion, per the roadmap's own prior design
     decision — now backed by real seeded data instead of nothing.
  3. **Seeded 5 real lookup types at every startup** (idempotent, never overwrites an admin's own edits):
     Country (~74 countries, EN+AR), BusinessRoleType (10), AddressType (4), UnitOfMeasure (15), Trade (20).
  4. **A genuine Admin Panel, not one page**: `LookupDataPage.tsx` gets its own distinct nav entry per type
     (Countries/Business Role Types/Address Types/Units of Measure/Trades, each its own heading and URL)
     plus an "All Lookup Types" hub — inline-editable SAP-style table maintenance grids (Edit/Deactivate/
     Delete per row, an always-visible add-row), Name and Name (Arabic) always shown side by side so a
     bilingual entry is never guessing at the other language (an explicit user requirement). Delete refuses
     when a value is actually in use (409) or when a type is system-defined or non-empty — deactivate
     instead, same "correct by reversal" principle as the rest of the platform.
  5. **Retrofitted the dropdowns that used to be hardcoded**: `BusinessPartnersPage.tsx`'s Business Role
     Type/Address Type/Country fields and `ItemsPage.tsx`'s Unit of Measure field now fetch their options
     live from this engine — an admin's own addition is immediately selectable, no code change, no rebuild.
- Two real bugs caught only by the live Playwright pass (not by unit/integration tests): the Lookup Data
  page's own heading only ever read the English `Name`, never `NameArabic`, so it stayed in English even in
  Arabic mode; and entering a type's grid via its direct nav link (not via the hub) never populated the
  `types` list the heading needed, so the heading fell back to the raw code (e.g. "BusinessRoleType" instead
  of "Business Role Type"/"نوع دور الشريك التجاري"). Both fixed, re-verified live in both languages.
- Verified: 12 new unit tests (`LookupServiceTests`) + 3 new integration tests against real PostgreSQL — all
  pass; 138 unit + 26 integration tests in Modules.MasterData alone, zero regressions solution-wide (19 test
  projects). Frontend typecheck + Arabic-hardcoding guardrail pass. Live `curl` exercise: full value CRUD
  lifecycle (create/rename/deactivate/reactivate/delete), confirmed in-use-value delete and system-defined-
  type delete both correctly 409, confirmed a bogus role/country/UoM is rejected with 400 while real seeded
  values succeed. Live Playwright pass (screenshots, zero console errors) on the hub, an inline add on the
  Countries grid, and Business Role Types, in both English and Arabic — full RTL mirroring confirmed.
- Files touched: `src/Modules/Modules.MasterData/Domain/LookupType.cs`, `LookupValue.cs` (new);
  `Application/LookupDto.cs`, `ILookupRepository.cs`, `LookupSecurity.cs`, `LookupService.cs` (new);
  `Infrastructure/EfLookupRepository.cs`, `LookupSeeder.cs` (new), `MasterDataDbContext.cs` (new
  DbSets/mapping, dropped `.HasConversion<string>()` on RoleType/AddressType), migration
  `20260715102410_AddLookupEngine`; `Api/LookupsController.cs` (new); `Domain/BusinessPartner.cs`,
  `BusinessRole.cs`, `BusinessPartnerAddress.cs` (enum → string retrofit), deleted `BusinessRoleType.cs`/
  `AddressType.cs`; `Application/BusinessPartnerService.cs`, `ItemService.cs` (lookup validation replacing
  `Enum.TryParse`/free text); `src/Gateway/Gateway.Api/Program.cs` (DI wiring, security catalog, actor role
  assignment, startup seeding call); `src/Apps/Apps.Shell/src/api/lookupApi.ts` (new),
  `src/Apps/Apps.Shell/src/pages/LookupDataPage.tsx` (new), `App.tsx` (nav/routing),
  `i18n/content.ts` (`lookup.*`/`nav.lookup*` keys, EN+AR), `pages/BusinessPartnersPage.tsx`,
  `pages/ItemsPage.tsx` (dropdown retrofit); `tests/UnitTests/Modules.MasterData.Tests/LookupServiceTests.cs`,
  `FakeLookupRepository.cs` (new), `BusinessPartnerServiceTests.cs`, `ItemServiceTests.cs` (wired in the
  fake), `BusinessPartnerTests.cs` (enum literals → string literals);
  `tests/IntegrationTests/Modules.MasterData.IntegrationTests/LookupPersistenceTests.cs` (new),
  `TestDatabase.cs` (truncate the two new tables), `BusinessPartnerPersistenceTests.cs` (enum → string);
  `src/Modules/Modules.MasterData/README.md` (new "Lookup Data" section, updated Deferred list).
- Next: this checkpoint is complete. Still pending from the same user instructions, not part of this slice:
  the comprehensive SAP/Dynamics architecture gap audit with a persistent audit file (see the roadmap/
  PROGRESS entries this session already flagged as the next piece of work). Minor, disclosed gaps left in
  the admin panel itself (no rename-type UI, no bulk import/export, no cross-module `Contracts` publication
  of lookup values) are listed in `Modules.MasterData/README.md`'s Deferred section, not hidden.

### 2026-07-15 — Phase 3 slice 1: Modules.ProjectManagement (Project + WBS Element) built, tested, live-verified

- Agent: Claude Sonnet 5
- Phase: Phase 3 — Construction & Project Management
- Status: In Progress — this slice (WBS foundation) is complete; Networks/Activities and Modules.Construction
  itself are not started
- What changed: Scaffolded a new `Modules.ProjectManagement` module (Domain/Application/Infrastructure/Api,
  own `"projectmanagement"` Postgres schema, wired into Gateway.Api) implementing the opening piece of Phase
  3's roadmap item, per the user's earlier confirmed choice ("ProjectManagement: WBS/Networks foundation").
  Built `Project : BusinessObject` (ProjectName/ProjectNameArabic/optional Customer validated as a "Client"
  role via `IBusinessPartnerLookup`/Start-End dates) owning a child `WbsElement` hierarchy
  (Code/Name/ParentWbsElementId/IsPlanningElement/IsAccountAssignmentElement/IsBillingElement — the three
  real-SAP Controlling-object flags). `ProjectService.CreateAsync` accepts the entire WBS hierarchy in one
  request via a tempId/parentTempId scheme (no element has a real Guid yet at request time) and resolves it
  in a single parent-before-child pass. Stops at Approved (no Post/Reverse — a Project Definition is
  organizational, not a financial document). Standard one-step Any-quorum Maintainer/Approver workflow and
  security, same shape as every other module's first workflow cut.
- Verified: 19 new unit tests + 3 new integration tests against real PostgreSQL — 22/22 pass, zero
  regressions solution-wide (19 test projects, full `dotnet test erp-platform.sln` run clean). Frontend
  (`ProjectsPage.tsx`, using the established `SplitView` pattern) builds clean, Arabic-hardcoding guardrail
  passes. Live `curl` exercise: created a Project with a 3-element WBS hierarchy (root + 2 children) in one
  request, confirmed correct tempId→Guid parent resolution in the response, drove it through Submit→Approve
  against the real running backend. Live Playwright pass (screenshots, zero console errors) on the list,
  details (General + WBS Elements FastTabs), and create form, in both English and Arabic — full RTL
  mirroring confirmed including the WBS table's parent-reference column.
- Files touched: `src/Modules/Modules.ProjectManagement/**` (new module — Domain: `Project.cs`,
  `WbsElement.cs`; Application: `ProjectDto.cs`, `IProjectRepository.cs`, `ProjectSecurity.cs`,
  `ProjectWorkflow.cs`, `ProjectService.cs`; Infrastructure: `ProjectManagementDbContext.cs`,
  `EfProjectRepository.cs`, `EfCoreNumberRangeService.cs`, `EfWorkflowInstanceRepository.cs`,
  `NumberRangeCounterEntity.cs`, `DesignTimeDbContextFactory.cs`, migration `20260715003053_InitialCreate`;
  Api: `ProjectsController.cs`); `src/Modules/Modules.ProjectManagement/README.md` (new, replaces the earlier
  planning stub); `src/Gateway/Gateway.Api/Gateway.Api.csproj`, `Program.cs` (module wiring — security
  catalog, workflow catalog, DbContext, DI, number range); `tests/UnitTests/Modules.ProjectManagement.Tests/**`
  (new), `tests/IntegrationTests/Modules.ProjectManagement.IntegrationTests/**` (new);
  `src/Apps/Apps.Shell/src/api/projectApi.ts` (new), `src/Apps/Apps.Shell/src/pages/ProjectsPage.tsx` (new),
  `src/Apps/Apps.Shell/src/App.tsx` (routing/nav), `src/Apps/Apps.Shell/src/i18n/content.ts` (`proj.*`/
  `nav.project*` keys, EN+AR).
- Next: Networks/Activities/Milestones (the other half of this module's roadmap name — scheduling,
  dependencies, resource/equipment allocation) is not built. `Modules.Construction` (the commercial layer —
  subcontracts, variation orders, progress billing — referencing this module's WBS elements) has not been
  started. Also still pending from prior user instructions, not part of this slice: (1) build a real
  configurable lookup-data platform capability (admin-editable Country/BusinessRoleType/AddressType etc.,
  not hardcoded enums/arrays) with actual CRUD, organized in a proper Admin Panel by heading; (2) a
  comprehensive SAP/Dynamics architecture gap audit with a persistent audit file and roadmap updates.

### 2026-07-15 — UI/Visual Density Pass paused per explicit user instruction

- Agent: Claude Sonnet 5
- Phase: Checkpoint — UI/Visual Density Pass
- Status: Paused (not reverted) — see Phase Status Summary above
- What changed: After the proof-of-concept slice below (teal identity + `SplitView` retrofit on two pages,
  published as an Artifact for review), the user rejected the result as not competitive with SAP
  Fiori/Dynamics 365 ("shitting color... opening one window") and explicitly instructed: stop touching the
  visual work, do not change or revert it, move directly to roadmap-driven phase work instead ("go abhead
  with ither phase just forget do not change or do anything go with road map and create the next phase in 1
  go"). Complied: made zero further edits to `Platform.UI` tokens/`SplitView`/the two retrofitted pages'
  visual styling. `SplitView` continued to be used for the new `ProjectsPage.tsx` below purely because it is
  now this codebase's established page-building pattern, not as further "visual work."
  - Root-cause read (recorded in memory as `feedback_visual_richness_gap` for future sessions): the pass
    delivered infrastructure/plumbing (a color-token swap, one layout mechanic, one CSS blur effect) but no
    actual surface richness — no icons anywhere, plain-text status instead of colored pills, no KPI/summary
    numbers, no avatar/initials badges. A real competitive pass needs to add visual weight/detail per
    surface, not just restructure layout and recolor tokens.
- Files touched: none (no code changes in this entry — status/documentation only).
- Next: do not resume this checkpoint unless the user explicitly asks for it again. When resumed, prioritize
  actual surface richness (icons, status pills, KPI numbers, avatar badges) over further layout/token
  changes, per `feedback_visual_richness_gap`.

### 2026-07-15 — UI/Visual Density Pass started: new color identity + SplitView master-detail pattern

- Agent: Claude Sonnet 5
- Phase: Checkpoint — UI/Visual Density Pass (see the roadmap checkpoint added earlier this session)
- Status: In Progress — proof-of-concept slice complete, full rollout across remaining pages not done
- What changed: Per the checkpoint scheduled right after Phase 2 closed, started the visual redesign the
  user asked to be genuinely original rather than a copy of SAP Fiori or Dynamics 365. Two concrete, durable
  decisions (recorded in memory as `project_visual_identity_decisions` for future sessions):
  1. **Color identity**: `Platform.UI/tokens/design-tokens.css`'s palette was — unintentionally — an exact
     copy of GitHub Primer's colors (`#0969da` etc.). Since blue is also Fiori's and Dynamics's default
     accent, replaced it with a deep **teal** accent (`#0d7377` light / `#2dd4bf` dark) and teal-tinted cool
     neutrals, keeping conventional green/red/amber for success/danger/warning (status colors need instant
     recognition; the creative budget went into the identity color, not the semantic vocabulary). This one
     token-file change ripples through every existing page automatically — verified live on an
     un-retrofitted page (Business Partners) to confirm nothing broke.
  2. **Interaction pattern**: new `Platform.UI.SplitView` component — the list pane stays visible while the
     detail pane slides in beside it (RTL-aware CSS `@keyframes` entrance), instead of replacing the list the
     way both Fiori and Dynamics 365 do on drill-down. Closer to a mail client's master-detail. The detail
     pane is rendered as a translucent "glass" panel (`backdrop-filter: blur()` + a soft inner top highlight),
     needing a subtle accent-tinted background wash behind it (`Apps.Shell/src/App.css`) to actually read as
     glass rather than a flat opaque card — genuinely different from either reference product's flat panels.
     Also added `.pi-dense-table`/`.pi-link` (compact rows, key-column drill-down links in the product's own
     accent color instead of literal blue, RTL-correct selected-row indicator) and a small `.pi-doc-chain`
     hint for chained procurement documents.
  3. Retrofitted two pages end-to-end as the proof: `PurchaseOrdersPage.tsx` (the densest/most-chained
     document) and `BusinessPartnersPage.tsx` (the largest existing page — 6 FastTabs, several sub-forms —
     proving the pattern survives real complexity, not just a toy page).
- Verified: frontend typecheck + Arabic-hardcoding guardrail both pass. Live Playwright pass: light mode,
  dark mode, and Arabic (RTL) all screenshotted on both retrofitted pages — confirmed the split view mirrors
  correctly under RTL (detail pane and its slide-in direction both flip to the correct logical side, the
  selected-row indicator bar flips from inset-start to inset-end), zero browser console errors throughout.
  Confirmed via a live screenshot that an un-retrofitted page (a plain FastTabs page) automatically inherited
  the new teal identity with nothing broken, proving the token-only part of this change is safe to leave
  applied everywhere immediately even before every page is converted to SplitView.
- Files touched: `src/Platform/Platform.UI/tokens/design-tokens.css` (full palette replacement + elevation/
  motion/glass tokens), `src/Platform/Platform.UI/components/SplitView.tsx` (new), `components/components.css`
  (SplitView layout/animation, `.pi-dense-table`/`.pi-link`), `index.ts` (SplitView export);
  `src/Apps/Apps.Shell/src/App.css` (content-area background wash for the glass blur, `.pi-doc-chain`);
  `src/Apps/Apps.Shell/src/pages/PurchaseOrdersPage.tsx`, `BusinessPartnersPage.tsx` (both converted to
  SplitView); `src/Apps/Apps.Shell/src/i18n/content.ts` (`po.selectHint`/`bp.selectHint` keys);
  `src/Platform/Platform.UI/README.md` (documented the new tokens/component, updated Deferred list).
- Next: retrofit the remaining pages (GL Accounts, Items, Cost Centers, Tax Codes, Journal Entries, AP
  Invoices, Vendor Prequalification, Purchase Requisitions, RFQs, GRNs) to SplitView the same way — each is a
  mechanical repeat of the pattern proven on these two pages, not a new design decision. Also flagged but not
  fixed in this slice: no BO anywhere has a real "Edit" Action Pane button yet (see Platform.UI/README.md's
  Deferred list) — worth closing while doing the mechanical retrofit pass, since SplitView's Action Pane is
  exactly where it belongs. Phase 3 (`Modules.ProjectManagement`'s WBS/Networks foundation) starts once the
  retrofit is done.

### 2026-07-15 — Goods Receipt Note + 3-Way Match built (Phase 2 slice 6) — Phase 2 exit criteria met

- Agent: Claude Sonnet 5
- Phase: Phase 2 — Procurement
- Status: Completed — **this closes Phase 2's exit criteria** ("full procure-to-pay cycle with configurable
  approval matrix")
- What changed: Built `GoodsReceiptNote`/`GrnLine` — the fourth procure-to-pay document, recording partial/
  staged receipts against an Approved PO, with cumulative-quantity-vs-ordered validation across every
  non-Rejected GRN for a line. Built `ThreeWayMatchService` — a computed-on-demand (never persisted) check
  comparing Ordered (`PurchaseOrder.Total`) vs Received (sum of Approved GRNs) vs Invoiced (an AP Invoice's
  `NetAmount`, via a new `Modules.Finance.Contracts.IAPInvoiceLookup`). The match deliberately lives entirely
  on the Procurement side — Finance is upstream of Procurement in the module dependency graph, so Finance
  can never depend back on Procurement; Procurement reads its own PO/GRN data directly and reaches into
  Finance only through the one new published lookup, same direction `IBudgetCheckService` already
  established. The match is at the document-amount level, not line-by-line, since `APInvoice` has no lines/
  PO reference to match per line — disclosed as deferred rather than reworking an already-shipped Phase 1
  entity's shape mid-Phase-2.
- Also fixed a real UX gap the user caught live: the create-form line-entry grids (Purchase Requisition,
  Purchase Order's direct mode) had no way to remove a wrongly-added line before clicking Create — only
  Submit/Approve/Reject exist after creation, so a mis-added line among many would have forced a full
  reject-and-redo. Added a "Remove" button per line in both grids (and built the new GRN create form with
  one from the start).
- One real bug, caught only by the live `curl` exercise (not by unit tests, which build their own
  self-contained workflow catalog per test): Gateway.Api's DI wiring registered
  `GoodsReceiptNoteSecurity`/`GoodsReceiptNoteWorkflow`'s roles and actor-role assignments but never added
  `GoodsReceiptNoteWorkflow.SubmitApprovalDefinition` to the actual `IWorkflowDefinitionCatalog`
  registration — a GRN could be Submitted (silently treated as "no approval configured") but never Approved.
  Fixed immediately, re-verified live and via the full test suite.
- Verified: 105 unit tests in Modules.Procurement.Tests (22 new) + 13 integration tests in
  Modules.Procurement.IntegrationTests (3 new) pass; full solution (17 test projects) builds and tests clean
  with zero regressions, architecture guardrail tests pass. Frontend typecheck + Arabic-hardcoding guardrail
  pass. Live `curl` exercise: partially received a PO (15 of 50 units), submitted/approved the GRN, created
  an AP invoice within the received value and confirmed the match reports Matched with exact figures, then
  created a second invoice exceeding the received value and confirmed Variance with a human-readable note;
  confirmed over-receipt is rejected both within one GRN and cumulatively across two GRNs. Live Playwright
  pass (screenshots, zero console errors) in both English and Arabic on `GoodsReceiptNotesPage.tsx` and
  `PurchaseOrdersPage.tsx`'s new 3-Way Match FastTab — full RTL mirroring confirmed.
- Files touched: `src/Modules/Modules.Finance/Contracts/IAPInvoiceLookup.cs` (new),
  `src/Modules/Modules.Finance/Infrastructure/EfAPInvoiceLookup.cs` (new);
  `src/Modules/Modules.Procurement/Domain/GoodsReceiptNote.cs`, `GrnLine.cs` (new);
  `Application/GoodsReceiptNoteDto.cs`, `IGoodsReceiptNoteRepository.cs`, `GoodsReceiptNoteSecurity.cs`,
  `GoodsReceiptNoteWorkflow.cs`, `GoodsReceiptNoteService.cs`, `ThreeWayMatchDto.cs`,
  `ThreeWayMatchService.cs` (new); `Infrastructure/ProcurementDbContext.cs` (GRN/GrnLine mapping),
  `EfGoodsReceiptNoteRepository.cs` (new), migration `20260714230934_AddGoodsReceiptNote`;
  `Api/GoodsReceiptNotesController.cs` (new), `PurchaseOrdersController.cs` (three-way-match endpoint);
  `src/Gateway/Gateway.Api/Program.cs` (DI/Security/SoD/Workflow registrations for GRN, IAPInvoiceLookup
  registration); frontend `src/Apps/Apps.Shell/src/api/goodsReceiptNoteApi.ts` (new),
  `src/Apps/Apps.Shell/src/api/purchaseOrderApi.ts` (checkThreeWayMatch), `pages/GoodsReceiptNotesPage.tsx`
  (new), `pages/PurchaseOrdersPage.tsx` (3-Way Match FastTab), `pages/PurchaseRequisitionsPage.tsx`
  (Remove-line button), `App.tsx` (new nav Area), `i18n/content.ts` (`grn.*`/`po.match*`/`po.tabThreeWayMatch`/
  `nav.goodsReceiptNotesArea`/`pr.actionRemoveLine`/`po.actionRemoveLine` keys); tests:
  `tests/UnitTests/Modules.Procurement.Tests/GoodsReceiptNoteTests.cs`, `GoodsReceiptNoteServiceTests.cs`,
  `ThreeWayMatchServiceTests.cs`, `FakeGoodsReceiptNoteRepository.cs`, `FakeAPInvoiceLookup.cs` (new);
  `tests/IntegrationTests/Modules.Procurement.IntegrationTests/GoodsReceiptNotePersistenceTests.cs` (new),
  `TestDatabase.cs` (added goods_receipt_notes truncate); `src/Modules/Modules.Procurement/README.md`.
- Next: Phase 2 is functionally complete against its own stated exit criteria. Per this session's earlier
  go-ahead: the UI/Visual Density Pass checkpoint is next (see the entry below) — build a shared dense
  List+Details component and retrofit every existing page — before any Phase 3 code starts. Phase 3's first
  slice, once that pass is done, is `Modules.ProjectManagement`'s WBS/Networks foundation (the generic
  cost backbone `Modules.Construction`'s Contracts/BOQ/Subcontracts depend on).

### 2026-07-15 — Purchase Order built (Phase 2 slice 5); deferred RFQ Playwright check closed out
- Agent: Claude Sonnet 5
- Phase: Phase 2 — Procurement
- Status: Completed
- What changed: Built `PurchaseOrder` — the third document in the procure-to-pay chain — "from an
  RFQ-selected quote or direct" per task #102's own wording. Domain: `PurchaseOrder` + child
  `PurchaseOrderLine` (carries the real negotiated `UnitPrice`, not an estimate, plus `CostCenterId` — unlike
  `RfqLine`, which drops Cost Center; a PO needs one back for the budget check). From-RFQ creation validates
  the RFQ is Approved, the vendor was invited and has quoted *every* line, then copies lines at the vendor's
  quoted price and traces each line's Cost Center back through `RfqLine.PurchaseRequisitionLineId` to the
  source PR's own line. Direct creation validates Item/Cost Center references the same way Purchase
  Requisition does. Also built **`Modules.Finance.Contracts`** (new project, mirrors
  `Modules.MasterData.Contracts`'s shape) publishing `IBudgetCheckService` — the exact synchronous
  cross-module contract call docs/architecture/01-architecture-foundation.md §3.2 names as its own worked
  example ("Procurement asks Finance's IBudgetCheckService before releasing a PO");
  `PurchaseOrderService.SubmitAsync` calls it once per distinct cost center before submitting for approval.
  The implementation, `PassThroughBudgetCheckService`, always allows for now — Budget Control itself is still
  deferred Finance depth, so there's no real budget data to check against yet, disclosed in that class's own
  doc comment rather than faking enforcement. Also closed out the previous session's explicitly-flagged open
  item: a live Playwright pass (EN+AR) on `RequestsForQuotationPage.tsx`, deferred when that slice was built.
- Verified: 83 unit tests in Modules.Procurement.Tests (18 new) + 10 integration tests in
  Modules.Procurement.IntegrationTests (3 new) all pass; full solution (15 test projects) builds and tests
  clean with zero regressions; architecture guardrail tests (module dependency direction) pass with the new
  Modules.Finance.Contracts package in the graph. Frontend typecheck + Arabic-hardcoding guardrail both pass.
  Live `curl` exercise: built the full PR→RFQ→PO chain end-to-end (created/approved a PR, created/approved an
  RFQ with a vendor quote, created a PO from that RFQ — confirmed it used the vendor's *quoted* price, not
  the PR's *estimated* price, and correctly traced the cost center), submitted it (budget check ran and
  passed) and approved it, created a second PO directly with no RFQ, confirmed supplying both an RFQ and
  direct lines is rejected with a 400. Live Playwright pass (screenshots, zero browser console errors) in
  both English and Arabic on both `PurchaseOrdersPage.tsx` (list, create form's From-RFQ/Direct source toggle,
  line-entry grid, details FastTabs) and `RequestsForQuotationPage.tsx` — full RTL mirroring confirmed on
  both pages including nav tree, table columns, and FastTabs.
- Files touched: `src/Modules/Modules.Finance/Contracts/Modules.Finance.Contracts.csproj`,
  `IBudgetCheckService.cs` (new project); `src/Modules/Modules.Finance/Infrastructure/
  PassThroughBudgetCheckService.cs` (new), `Modules.Finance.Infrastructure.csproj` (project reference);
  `src/Modules/Modules.Procurement/Domain/PurchaseOrder.cs`, `PurchaseOrderLine.cs` (new);
  `Application/PurchaseOrderDto.cs`, `IPurchaseOrderRepository.cs`, `PurchaseOrderSecurity.cs`,
  `PurchaseOrderWorkflow.cs`, `PurchaseOrderService.cs` (new), `Modules.Procurement.Application.csproj`
  (Finance.Contracts reference); `Infrastructure/ProcurementDbContext.cs` (PurchaseOrder/Line mapping),
  `EfPurchaseOrderRepository.cs` (new), migration `20260714205512_AddPurchaseOrder`;
  `Api/PurchaseOrdersController.cs` (new); `src/Gateway/Gateway.Api/Program.cs` (DI/Security/SoD/Workflow
  registrations for PO, IBudgetCheckService registration); frontend
  `src/Apps/Apps.Shell/src/api/purchaseOrderApi.ts`, `src/Apps/Apps.Shell/src/pages/PurchaseOrdersPage.tsx`,
  `App.tsx` (new nav Area under Procurement), `i18n/content.ts` (`po.*`/`nav.purchaseOrdersArea` keys); tests:
  `tests/UnitTests/Modules.Procurement.Tests/PurchaseOrderTests.cs`, `PurchaseOrderServiceTests.cs`,
  `FakePurchaseOrderRepository.cs`, `FakeBudgetCheckService.cs` (new), `Modules.Procurement.Tests.csproj`
  (Finance.Contracts reference); `tests/IntegrationTests/Modules.Procurement.IntegrationTests/
  PurchaseOrderPersistenceTests.cs` (new), `TestDatabase.cs` (added purchase_orders truncate);
  `erp-platform.sln` (new Modules.Finance.Contracts project entry); `src/Modules/Modules.Procurement/
  README.md`, `src/Modules/Modules.Finance/README.md`.
- Next: GRN (Goods Receipt Note) + 3-way match against AP Invoice — the last piece of Phase 2's exit
  criteria ("full procure-to-pay cycle with configurable approval matrix"). After that closes out, the
  UI/Visual Density Pass checkpoint (see the entry below) is next, before Phase 3 starts.

### 2026-07-14 — Decision: UI/Visual Density Pass scheduled as a checkpoint before Phase 3
- Agent: Claude Sonnet 5
- Phase: Architecture / Roadmap (no code changed)
- Status: Completed (decision + docs only)
- What changed: User wants every page to reach Dynamics 365 F&O-level density and navigation — dense
  sortable/filterable grids (like Dynamics's vendor list), key/ID fields rendered as blue hyperlinks that
  drill into that record's own full details page, comprehensive FastTab-based detail pages — this matters
  particularly for the future Construction/Project Management module (Phase 3), which needs SAP
  Fiori/Dynamics-grade density given this is a contracting-company ERP. `docs/architecture/02-business-object-
  model.md` §2/§3 already specified this exact target pattern (merged List+Details form, FastTabs, Action
  Pane, hyperlink drill-down) — it was written but never actually built that way; every page from Phase 0–2
  was hand-rolled per slice at a simpler functional level (no shared component, `tools/object-page-gen`
  never finished into a real generator). Rather than retrofitting density page-by-page (expensive, and Phase
  2 isn't done), added a **Checkpoint — UI/Visual Density Pass** section to
  `docs/architecture/06-roadmap.md`, placed right after Phase 2's exit criteria and before Phase 3: build one
  shared `Platform.UI` List+Details component (or finish `tools/object-page-gen`) once every recurring page
  shape has appeared (flat list, hierarchical list, multi-step workflow, line-item grid, cross-document
  drill-down — all present by the time PO/GRN/3-way match lands), then apply it to every existing page in one
  pass and get it for free on every Phase 3+ page. Explicitly told future sessions not to hand-roll partial
  density on new Phase 2 pages before this checkpoint — that would be the exact rework this is meant to avoid.
- Files touched: `docs/architecture/06-roadmap.md` (new Checkpoint section between Phase 2 and Phase 3),
  `PROGRESS.md` (this entry + Phase Status Summary row).
- Next: finish Phase 2 (Purchase Order → GRN → 3-way match + budget-check integration) exactly as already
  planned, still in the current simpler page style. Do the UI/Visual Density Pass only after that, then start
  Phase 3 already using the new dense component from day one.

### 2026-07-14 — Request for Quotation built (Phase 2 slice 4) — session paused here by user request
- Agent: Claude Sonnet 5
- Phase: Phase 2 — Procurement
- Status: Completed (this slice) — user explicitly asked to stop after this slice ("complete and deliver
  full vertical slice and hold... we have chances"), so Purchase Order/GRN (tasks #102/#103) are NOT started.
- What changed: Built `RequestForQuotation` — the second document in the procure-to-pay chain — as
  `Modules.Procurement`'s fourth vertical slice. Domain: `RequestForQuotation` + child `RfqLine` (copied from
  an Approved source `PurchaseRequisition`'s own lines: Item + Quantity, `PurchaseRequisitionLineId` kept for
  traceability), `RfqInvitedVendor` (a fixed vendor set, frozen once Submitted), `RfqVendorQuoteLine`
  (recorded per invited vendor per line, only after Submit, once per vendor+line pair). `Approve` just closes
  the quote-collection process — there's no "award" step on this entity; picking a winning quote is deferred
  to the future Purchase Order slice ("from RFQ-selected quote or direct", per the roadmap). Service depends
  on `IPurchaseRequisitionRepository` directly (same-module, no new Contracts package needed) to validate the
  referenced PR is Approved before copying its lines, and reuses
  `Modules.Finance.Application.APInvoiceService.PayableEligibleRoles`' exact commercial-relationship role set
  to decide which vendors are eligible to be invited to quote.
- Verified: 85 unit tests in Modules.Procurement.Tests (21 new for this slice) + 9 integration tests in
  Modules.Procurement.IntegrationTests (2 new) all pass; 379 tests pass solution-wide with zero regressions.
  Full backend + frontend build clean; both no-hardcoded-Arabic guardrails pass. Live `curl` exercise:
  created and approved a PR, created an RFQ against it (confirmed the line was copied correctly and the
  vendor invited), submitted, recorded a vendor quote, approved — confirmed the quote persisted through.
  Frontend built and typechecked clean; live Playwright pass deferred for this slice given the session's
  token-budget constraint at the time — the backend contract and full test coverage are proven, but the UI
  was not re-screenshotted in-browser before this checkpoint. **Next session/tool should do a quick
  Playwright pass on `RequestsForQuotationPage.tsx` (EN+AR) before building on top of it, to close that gap.**
- Files touched: `src/Modules/Modules.Procurement/Domain/RequestForQuotation.cs`, `RfqLine.cs`,
  `RfqInvitedVendor.cs`, `RfqVendorQuoteLine.cs` (new); `Application/RequestForQuotationDto.cs`,
  `IRequestForQuotationRepository.cs`, `RequestForQuotationSecurity.cs`, `RequestForQuotationWorkflow.cs`,
  `RequestForQuotationService.cs` (new); `Infrastructure/ProcurementDbContext.cs` (RFQ + 3 child collection
  mappings), `EfRequestForQuotationRepository.cs` (new), migration `20260714195518_AddRequestForQuotation`;
  `Api/RequestsForQuotationController.cs` (new); `src/Gateway/Gateway.Api/Program.cs` (DI/Security/SoD/
  Workflow registrations); frontend `src/Apps/Apps.Shell/src/api/requestForQuotationApi.ts`,
  `src/Apps/Apps.Shell/src/pages/RequestsForQuotationPage.tsx`, `App.tsx` (new nav Area under Procurement),
  `i18n/content.ts` (`rfq.*`/`nav.requestsForQuotationArea` keys); tests:
  `tests/UnitTests/Modules.Procurement.Tests/RequestForQuotationTests.cs`,
  `RequestForQuotationServiceTests.cs`, `FakeRequestForQuotationRepository.cs` (new);
  `tests/IntegrationTests/Modules.Procurement.IntegrationTests/RequestForQuotationPersistenceTests.cs` (new),
  `TestDatabase.cs` (added requests_for_quotation truncate); `src/Modules/Modules.Procurement/README.md`.
- Next: Purchase Order (task #102) — from an RFQ-selected quote or direct, plus the Finance budget-check
  integration via a new `Modules.Finance.Contracts.IBudgetCheckService` — then GRN + 3-way match (task
  #103), completing Phase 2's exit criteria. Do the deferred Playwright pass on this RFQ UI first.

### 2026-07-14 — Purchase Requisition built (Phase 2 slice 3)
- Agent: Claude Sonnet 5
- Phase: Phase 2 — Procurement
- Status: Completed
- What changed: Built `PurchaseRequisition` — the first document in the procure-to-pay chain (PR → RFQ →
  PO → GRN → 3-way match) — as `Modules.Procurement`'s second vertical slice. Domain: `PurchaseRequisition` +
  child `PurchaseRequisitionLine` (ItemId, CostCenterId, Quantity, EstimatedUnitPrice, optional
  LineDescription), computed `EstimatedTotal`, stops at Approved (no Post/Reverse — an internal request, not
  a financial document), lines frozen once submitted, same shape as `Modules.Finance.Domain.JournalEntry`.
  Added a new cross-module lookup this module needed for the first time beyond Vendor Prequalification's:
  `Modules.MasterData.Contracts.IItemLookup`/`ItemSummary` + `EfItemLookup`, registered in Gateway.Api's DI
  container — `PurchaseRequisitionService.CreateAsync` validates every line's Item (exists, Active) and Cost
  Center (exists, Active, Postable) through it before adding the line, the same "validate through Contracts,
  never through another module's Domain/Infrastructure directly" pattern every cross-module reference in
  this codebase uses. `PurchaseRequisitionSecurity`/`PurchaseRequisitionWorkflow` are a plain one-step
  Any-quorum Maintainer/Approver pair (not Vendor Prequalification's special 5-step case) — a real
  amount-conditioned approval matrix is deferred until this module has more than one approval path.
- Verified: 43 unit tests in Modules.Procurement.Tests (20 new for this slice) + 5 integration tests in
  Modules.Procurement.IntegrationTests (3 new) all pass; 356 tests pass solution-wide with zero regressions.
  Full backend + frontend build clean; both no-hardcoded-Arabic guardrails pass. Live `curl` exercise:
  created a requisition for 100 bags of cement against a real Item + Cost Center, drove it through
  Submit→Approve, confirmed a header/grouping cost center is correctly rejected with a clear 400. Live
  Playwright pass in both English and Arabic — the second real multi-row line-item entry grid in this
  application (after Journal Entry's), full RTL mirroring confirmed.
- Files touched: `src/Modules/Modules.MasterData/Contracts/IItemLookup.cs` (new),
  `src/Modules/Modules.MasterData/Infrastructure/EfItemLookup.cs` (new);
  `src/Modules/Modules.Procurement/Domain/PurchaseRequisition.cs`, `PurchaseRequisitionLine.cs` (new);
  `Application/PurchaseRequisitionDto.cs`, `IPurchaseRequisitionRepository.cs`,
  `PurchaseRequisitionSecurity.cs`, `PurchaseRequisitionWorkflow.cs`, `PurchaseRequisitionService.cs` (new);
  `Infrastructure/ProcurementDbContext.cs` (PurchaseRequisition/Line mapping),
  `EfPurchaseRequisitionRepository.cs` (new), migration `20260714193752_AddPurchaseRequisition`;
  `Api/PurchaseRequisitionsController.cs` (new); `src/Gateway/Gateway.Api/Program.cs` (IItemLookup + PR
  DI/Security/SoD/Workflow registrations); frontend
  `src/Apps/Apps.Shell/src/api/purchaseRequisitionApi.ts`,
  `src/Apps/Apps.Shell/src/pages/PurchaseRequisitionsPage.tsx`, `App.tsx` (new nav Area under Procurement),
  `i18n/content.ts` (`pr.*`/`nav.purchaseRequisitionsArea` keys); tests:
  `tests/UnitTests/Modules.Procurement.Tests/PurchaseRequisitionTests.cs`,
  `PurchaseRequisitionServiceTests.cs`, `FakePurchaseRequisitionRepository.cs`, `FakeLookups.cs` (new);
  `tests/IntegrationTests/Modules.Procurement.IntegrationTests/PurchaseRequisitionPersistenceTests.cs` (new),
  `TestDatabase.cs` (added purchase_requisitions truncate); `src/Modules/Modules.Procurement/README.md`,
  `src/Modules/Modules.MasterData/README.md` (Contracts section notes IItemLookup).
- Next: RFQ (Request for Quotation) — references an Approved PR + invited Vendors (via
  `IBusinessPartnerLookup`) with vendor quote lines — then Purchase Order (+ the Finance budget-check
  integration via a new `Modules.Finance.Contracts.IBudgetCheckService`), then GRN + 3-way match, completing
  Phase 2's exit criteria.

### 2026-07-14 — Modules.Procurement scaffolded; Vendor Prequalification built (first multi-step workflow)
- Agent: Claude Sonnet 5
- Phase: Phase 2 — Procurement
- Status: Completed
- What changed: Scaffolded `Modules.Procurement` (Domain/Application/Infrastructure/Api, own "procurement"
  Postgres schema in the same physical database as MasterData's/Finance's) and built its first vertical
  slice, Vendor Prequalification, per `docs/architecture/06-roadmap.md`'s Phase 2 design. Domain:
  `VendorPrequalification` (BusinessPartnerId + RoleType + optional Trade, stops at Approved like other
  Master-Data-ish BOs, `SetValidityPeriod` sets ValidFrom/ValidUntil exactly once at final approval from a
  configured validity period). Built and registered `VendorPrequalificationWorkflow` — a real 5-step
  (Commercial→Legal→Technical→HSE→Quality), Any-quorum, unconditioned approval matrix — **the first
  multi-step workflow this codebase has ever actually exercised** (every prior workflow used one step);
  confirmed feasible by reading `Platform.Workflow.WorkflowEngine`/`WorkflowInstance` before building, since
  each step just needs its own `RequiredRoleKey` and the engine already resolves "which step is current" +
  checks role eligibility per step. `VendorPrequalificationSecurity` registers one Maintainer duty/role plus
  five distinct reviewer duties/roles (one per domain) and five Maintainer-vs-reviewer SoD conflict rules.
  `CreateAsync` rejects Government Authority outright (the roadmap's explicit "not prequalified at all"
  exclusion) and validates the vendor exists, is Approved, and actually holds the requested role via
  `IBusinessPartnerLookup`. The validity period (default 24 months) is a real `Platform.Configuration` key
  (`Procurement.VendorPrequalification.ValidityMonths`), not hardcoded. This module also owns its own copy
  of `Platform.Attachments`' persistence (own schema, same reasoning as its NumberRange/WorkflowInstance
  copies) for supporting documents.
- Verified: 380 tests pass across the solution (23 new unit + 2 new integration tests for this slice,
  zero regressions elsewhere). Full backend + frontend build clean; both the backend architecture guardrail
  (`NoHardcodedTranslatableTextTests`) and the frontend script (`check-no-hardcoded-arabic.mjs`) pass. Live
  `curl` exercise: created a prequalification for an Approved Supplier and drove it through all 5 review
  steps to Approved with a computed validity window (2026-07-14 to 2028-07-14, 24 months), confirmed
  Government Authority and unheld-role requests are rejected with clear 400s, confirmed a mid-workflow
  rejection leaves no validity period set, and exercised the attachment upload/list/download/delete cycle.
  Live Playwright pass in both English and Arabic (nav tree correctly shows a new "Procurement" module,
  full RTL mirroring on the details page including FastTabs).
- Files touched: `src/Modules/Modules.Procurement/Domain/VendorPrequalification.cs`;
  `Application/VendorPrequalificationDto.cs`, `IVendorPrequalificationRepository.cs`,
  `VendorPrequalificationSecurity.cs`, `VendorPrequalificationWorkflow.cs`, `VendorPrequalificationService.cs`;
  `Infrastructure/ProcurementDbContext.cs`, `NumberRangeCounterEntity.cs`, `EfCoreNumberRangeService.cs`,
  `EfWorkflowInstanceRepository.cs`, `EfVendorPrequalificationRepository.cs`, `AttachmentContentRow.cs`,
  `EfAttachmentRepository.cs`, `DesignTimeDbContextFactory.cs`, migration `20260714191324_InitialCreate`;
  `Api/VendorPrequalificationsController.cs`; `src/Gateway/Gateway.Api/Program.cs` (DI/Security/SoD/Workflow/
  Configuration registrations, Gateway.Api.csproj project references); frontend
  `src/Apps/Apps.Shell/src/api/vendorPrequalificationApi.ts`,
  `src/Apps/Apps.Shell/src/pages/VendorPrequalificationsPage.tsx`, `App.tsx` (new "Procurement" nav module),
  `i18n/content.ts` (`vpq.*`/`nav.procurementModule`/`nav.vendorPrequalificationsArea` keys); tests:
  `tests/UnitTests/Modules.Procurement.Tests/*` (new project), `tests/IntegrationTests/
  Modules.Procurement.IntegrationTests/*` (new project); `src/Modules/Modules.Procurement/README.md`.
- Next: Purchase Requisition (PR) — the next Phase 2 vertical slice per the roadmap, followed by RFQ, PO
  (+ the Finance budget-check integration via a new `Modules.Finance.Contracts.IBudgetCheckService`), and
  GRN + 3-way match, which together complete Phase 2's exit criteria ("full procure-to-pay cycle with
  configurable approval matrix").

### 2026-07-14 — Phase 2 started: BusinessPartner.PartnerType replaced by multi-select BusinessRoles
- Agent: Claude Sonnet 5
- Phase: Phase 2 — Procurement
- Status: Completed
- What changed: Replaced `BusinessPartner.PartnerType` (Customer/Vendor/Both, a single enum) with
  `BusinessRoles` — a multi-select child collection (`BusinessRole`: `RoleType` + optional `Trade`) — per
  the design captured in `docs/architecture/06-roadmap.md` Phase 2 (2026-07-14, earlier this session).
  Built first, ahead of the rest of Phase 2, because Vendor Prequalification needs it to exist before it
  can be built on top of it — exactly the sequencing the roadmap entry called for. Ten role types: Client
  (replaces Customer), Supplier, Subcontractor, Consultant, JointVenturePartner, GovernmentAuthority,
  RentalCompany, Manufacturer, ManpowerSupplier, TestingLaboratory. Government Authority is mutually
  exclusive with every other role (no commercial relationship at all, per the roadmap); the same role can
  be held twice with different Trades (e.g. Subcontractor–Electrical and Subcontractor–Concrete on the
  same company), since Vendor Prequalification will qualify per Role+Trade combination, not once per Role.
  A real data migration converted every existing `partner_type` value into an equivalent role instead of
  silently dropping it (Customer/Both → Client, Vendor/Both → Supplier — a "Both" partner correctly ends
  up holding two roles) — confirmed live against the dev database's existing partners. Updated the one
  cross-module consumer, `Modules.Finance.Application.APInvoiceService`, to check for a payable-eligible
  role via `IBusinessPartnerLookup.BusinessRoles` instead of the old `PartnerType` string — the first real
  proof that a Contracts-package consumer survives a shape change on the publishing side.
- Trade/Specialty is free text with no server-side validation against the role-specific taxonomies the
  roadmap names (disclosed deferred — there's no admin config screen yet to manage a suggested-values
  list per role).
- Verified: 355 tests pass across 15 test projects (6 new Domain tests for role add/remove/mutual-exclusion/
  duplicate-trade rules), full solution builds clean, frontend typecheck + Arabic guardrail pass, live
  curl exercise (confirmed the migrated data, created a multi-role partner, added a Subcontractor role
  with a Trade, confirmed Government Authority's mutual exclusivity returns 409), live Playwright pass in
  both English and Arabic showing the new Business Roles FastTab (list + add + remove) and the reworked
  create form (role dropdown + conditional Trade field).
- Files touched: `src/Modules/Modules.MasterData/Domain/BusinessRoleType.cs`, `BusinessRole.cs`,
  `BusinessPartner.cs` (AddBusinessRole/RemoveBusinessRole, constructor takes an initial role), deleted
  `PartnerType.cs`; `Application/BusinessPartnerDto.cs`, `BusinessPartnerService.cs`
  (AddBusinessRoleAsync/RemoveBusinessRoleAsync, Submit requires ≥1 role);
  `Contracts/IBusinessPartnerLookup.cs` (BusinessRoles list replaces PartnerType string);
  `Infrastructure/EfBusinessPartnerLookup.cs`, `EfBusinessPartnerRepository.cs`, `MasterDataDbContext.cs`
  (business_partner_roles table mapping), `Migrations/20260714183315_ReplacePartnerTypeWithBusinessRoles*.cs`
  (hand-edited to migrate data, not just drop the column); `Api/BusinessPartnersController.cs`
  (business-roles endpoints); `src/Modules/Modules.Finance/Application/APInvoiceService.cs`
  (PayableEligibleRoles check); `src/Apps/Apps.Shell/src/api/businessPartnerApi.ts`,
  `src/Apps/Apps.Shell/src/pages/BusinessPartnersPage.tsx` (Business Roles FastTab, reworked create form),
  `src/Apps/Apps.Shell/src/pages/APInvoicesPage.tsx` (vendor filter), `src/Apps/Apps.Shell/src/i18n/content.ts`
  (bp.role*/bp.fieldBusinessRole/etc. keys, removed bp.partnerType* keys);
  `tests/UnitTests/Modules.MasterData.Tests/BusinessPartnerTests.cs`, `BusinessPartnerServiceTests.cs`;
  `tests/IntegrationTests/Modules.MasterData.IntegrationTests/BusinessPartnerPersistenceTests.cs`;
  `tests/UnitTests/Modules.Finance.Tests/APInvoiceServiceTests.cs`, `FakeLookups.cs`;
  `docs/architecture/06-roadmap.md`, `src/Modules/Modules.MasterData/README.md`.
- Next: Vendor Prequalification (the real Business Object this session's BusinessRoles work was building
  toward) and `Modules.Procurement` itself (PR→RFQ→PO→GRN→3-way match) both need a fresh go-ahead before
  starting — this was a large, self-contained rework, a natural checkpoint before going further into
  Phase 2.

### 2026-07-14 — AP Invoice — Phase 1 exit criteria for Finance Core met
- Agent: Claude Sonnet 5
- Phase: Phase 1 — Master Data + Finance Core
- Status: Completed
- What changed: Built AP Invoice — the other half of the Phase 1 exit criteria ("post/reverse a GL journal
  **and an AP invoice** end-to-end with full audit trail"). `APInvoice`: vendor reference validated as an
  actually-Approved Vendor/Both partner via `IBusinessPartnerLookup` (rejects unapproved or non-vendor
  partners), an explicitly-chosen Expense account + Payable account (deliberately not a configured "AP
  control account" default — there's no admin config screen yet to safely set one, and a guessed default
  would be worse than an explicit choice), and an optional Tax Code that **snapshots** its rate into
  `TaxRate` at creation so a later rate change never retroactively changes an already-created invoice.
  `Posting` an invoice generates a real, separate, linked G/L Journal Entry (Dr Expense, Dr VAT if any, Cr
  Payable — always balanced by construction since Gross ≡ Net + Tax); reversing an invoice reverses that
  linked entry too. Refactored `JournalEntryService.ReverseAsync`'s mirror-entry logic into a new shared
  `CreateSystemGeneratedAsync` method so AP Invoice's posting could reuse the exact same "construct →
  validate lines → number → drive lifecycle, skip human approval" sequence instead of duplicating it — the
  same reasoning this codebase has used everywhere else for extracting a second real consumer's shared
  logic. Wired end-to-end: Domain, Application (`APInvoiceService`, `APInvoiceSecurity`,
  `APInvoiceWorkflow`), Infrastructure (EF mapping in the same `finance` schema, migration applied to dev +
  test), Api (`api/v1/finance/ap-invoices`), frontend (`APInvoicesPage.tsx` — vendor/account/tax-code
  dropdowns, a live Net/Tax/Gross preview, own nav Area under Finance).
- Verified: 349 tests pass across 15 test projects (19 new Finance unit tests + 2 new Finance integration
  tests against real PostgreSQL), full solution builds clean, frontend typecheck + Arabic guardrail pass,
  and a full live exercise: created a vendor, confirmed an invoice against it was rejected while still
  Draft/unapproved, approved the vendor, created GL accounts (Expense/Payable/VAT Recoverable), created an
  invoice with 15% VAT (`FIN-AP-2026-000001`), drove it through Submit → Approve → Post, confirmed the
  generated journal entry (`FIN-JE-2026-000004` — continuing the same document-number sequence real
  user-created entries use) had exactly Dr Expense 1000 / Dr VAT 150 / Cr Payable 1150, reversed the
  invoice and confirmed the linked entry was reversed too, then created a second invoice with no tax code
  to confirm the two-line posting path. Live Playwright pass in both English and Arabic.
- **This closes out the Phase 1 exit criteria for Finance Core.** A company can now maintain its chart of
  accounts and vendors, and post/reverse a GL journal and an AP invoice end-to-end with full audit trail —
  exactly what docs/architecture/06-roadmap.md's Phase 1 exit criteria requires. AR/Cash-Bank, Document
  Splitting, Parallel Ledgers, Budget Control, and Results Analysis/CO-PA remain as later Finance depth
  (docs/architecture/07), not required to close Phase 1.
- Files touched: `src/Modules/Modules.Finance/Domain/APInvoice.cs`,
  `Application/APInvoiceDto.cs`, `APInvoiceService.cs`, `IAPInvoiceRepository.cs`, `APInvoiceSecurity.cs`,
  `APInvoiceWorkflow.cs`, `JournalEntryService.cs` (refactored `ReverseAsync` into shared
  `CreateSystemGeneratedAsync`), `Infrastructure/EfAPInvoiceRepository.cs`,
  `FinanceDbContext.cs` (APInvoice mapping), `Migrations/20260714165636_AddAPInvoice*.cs`,
  `Api/APInvoicesController.cs`, `src/Gateway/Gateway.Api/Program.cs` (DI + security + SoD + workflow +
  number range), `src/Apps/Apps.Shell/src/api/apInvoiceApi.ts`,
  `src/Apps/Apps.Shell/src/pages/APInvoicesPage.tsx`, `src/Apps/Apps.Shell/src/i18n/content.ts` (ap.* keys),
  `src/Apps/Apps.Shell/src/App.tsx` (AP Invoices nav Area + routing),
  `tests/UnitTests/Modules.Finance.Tests/APInvoiceTests.cs`, `APInvoiceServiceTests.cs`,
  `FakeAPInvoiceRepository.cs`, `FakeLookups.cs` (added Business Partner/Tax Code fakes),
  `tests/IntegrationTests/Modules.Finance.IntegrationTests/APInvoicePersistenceTests.cs`, `TestDatabase.cs`
  (truncate ap_invoices), `src/Modules/Modules.Finance/README.md` (AP Invoice "What's built" section,
  updated Deferred list).
- Next: Phase 1 is functionally complete against its own stated exit criteria. Remaining Finance depth
  (AR, Cash/Bank, Document Splitting, Parallel Ledgers, Budget Control, Results Analysis/CO-PA) and Phase 2
  (Procurement) both need a fresh go-ahead before starting — this closes out the user's "tax code and
  finance in one go" authorization for this session.

### 2026-07-14 — Modules.Finance started: Modules.MasterData.Contracts + GL Journal Entry
- Agent: Claude Sonnet 5
- Phase: Phase 1 — Master Data + Finance Core
- Status: Completed
- What changed: Started `Modules.Finance` — the second real business module, and genuinely the most
  architecturally significant piece built this session (double-entry validation, a cross-module Contracts
  boundary actually exercised for the first time, and the first real use anywhere in this codebase of the
  full Draft → Submit → Approve → Post → Reverse lifecycle — every Master Data slice stops at Approved).
  Three pieces, built and committed in order:
  1. **`Modules.MasterData.Contracts`** — a thin, dependency-free project publishing `IGLAccountLookup`/
     `IBusinessPartnerLookup`/`ITaxCodeLookup`/`ICostCenterLookup` + read-only summary DTOs, implemented as
     EF adapters projecting off the existing `MasterDataDbContext` (no new tables). This is the boundary
     docs/architecture/01 §3.2 always specified ("a module may depend on another module's published
     Contracts package only") but nothing had actually exercised until Finance needed to validate a G/L
     Account reference.
  2. **`Modules.Finance` scaffolding** — Domain/Application/Infrastructure/Api projects mirroring
     Modules.MasterData's exact layering, with its own "finance" Postgres schema (physically enforcing the
     module boundary the same way "masterdata" does) and its own copies of
     `EfCoreNumberRangeService`/`EfWorkflowInstanceRepository` (near-duplicates of MasterData's, backed by
     `FinanceDbContext` instead — a module cannot depend on another module's Infrastructure directly, so
     these two "look generic but are actually per-module" pieces of plumbing get their own table in each
     module's own schema; disclosed reasoning in `NumberRangeCounterEntity`'s doc comment).
  3. **GL Journal Entry** — `JournalEntry`/`JournalLine` (Domain), `JournalEntryService` (validates every
     line's G/L Account/Cost Center reference through the new Contracts interfaces before adding it),
     `JournalEntriesController` at `api/v1/finance/journal-entries`, `JournalEntriesPage.tsx` (the first
     real multi-row line-item entry grid in this application, with a live balanced/unbalanced indicator).
     `IsBalanced` (total debits = total credits) is enforced at both Submit and Post — not a configurable
     rule, the accounting identity itself. Reversal creates a brand-new mirror entry with every line's
     debit/credit swapped (undoing the ledger's actual balance, not just flipping a status flag), driven
     straight to Posted without a second human approval since its content is mechanically derived from an
     already-approved document (same reasoning as SAP's FB08 "reverse document") — see
     `JournalEntryService.ReverseAsync`'s doc comment.
- Verified: 328 tests pass across 15 test projects (24 new Finance unit tests + 4 new Finance integration
  tests against real PostgreSQL — entry+lines round-trip, RowVersion across 3 real transitions, cascade
  delete, a reversal's link to its original persisting), full solution builds clean, frontend typecheck +
  Arabic guardrail pass, and a live end-to-end exercise: created an unbalanced entry (allowed as Draft,
  rejected on Submit with the exact imbalance amounts), created a balanced entry and drove it through
  Submit → Approve → Post → Reverse via curl, confirmed the mirror entry had debit/credit correctly
  swapped and linked via `reversalOfEntryId`, then confirmed the same live in the browser in both English
  and Arabic (full RTL mirroring including the line-item grid's column order). New "Finance" nav module,
  not bolted onto Master Data.
- Files touched: `src/Modules/Modules.MasterData/Contracts/*` (new project), `Infrastructure/EfGLAccountLookup.cs`,
  `EfBusinessPartnerLookup.cs`, `EfTaxCodeLookup.cs`, `EfCostCenterLookup.cs` (new),
  `src/Modules/Modules.Finance/Domain/JournalEntry.cs`, `JournalLine.cs` (new project),
  `Application/JournalEntryDto.cs`, `JournalEntryService.cs`, `IJournalEntryRepository.cs`,
  `JournalEntrySecurity.cs`, `JournalEntryWorkflow.cs` (new project),
  `Infrastructure/FinanceDbContext.cs`, `EfJournalEntryRepository.cs`, `EfCoreNumberRangeService.cs`,
  `EfWorkflowInstanceRepository.cs`, `NumberRangeCounterEntity.cs`, `DesignTimeDbContextFactory.cs`,
  `Migrations/20260714162917_InitialCreate*.cs` (new project), `Api/JournalEntriesController.cs` (new
  project), `src/Gateway/Gateway.Api/Program.cs` (DI for both new modules), `erp-platform.sln`,
  `src/Apps/Apps.Shell/src/api/journalEntryApi.ts`, `src/Apps/Apps.Shell/src/pages/JournalEntriesPage.tsx`,
  `src/Apps/Apps.Shell/src/i18n/content.ts` (je.* keys), `src/Apps/Apps.Shell/src/App.tsx` (Finance nav
  module + routing), `tests/UnitTests/Modules.Finance.Tests/*` (new project),
  `tests/IntegrationTests/Modules.Finance.IntegrationTests/*` (new project),
  `src/Modules/Modules.Finance/README.md`, `src/Modules/Modules.MasterData/README.md` (Contracts section).
- Next: AP Invoice — the other half of the Phase 1 exit criteria ("post/reverse a GL journal **and an AP
  invoice** end-to-end with full audit trail"). Will reference a vendor via `IBusinessPartnerLookup` and a
  tax code via `ITaxCodeLookup` (snapshotting the rate at creation), and post a linked GL Journal Entry
  through the machinery just built. Continuing in this same session per the user's "tax code and finance in
  one go" instruction.

### 2026-07-14 — Tax Codes — Phase 1 Master Data complete
- Agent: Claude Sonnet 5
- Phase: Phase 1 — Master Data + Finance Core
- Status: Completed
- What changed: Built Tax Codes — the ZATCA VAT reference (e.g. "VAT15" at 15%, "ZERO", "EXEMPT") every
  future AP/AR document will carry, and the fifth and last Phase 1 Master Data piece. `TaxCode`: unique
  `TaxCodeCode`, bilingual `TaxCodeName`/`TaxCodeNameArabic`, `Rate` (decimal 0–100, validated both in the
  constructor and on update — the VAT percentage lives here as data, never hardcoded in any module's code),
  `TaxType` enum (Standard/ZeroRated/Exempt — the ZATCA taxonomy). Deliberately flat like Item, not
  GLAccount. Same BusinessObject lifecycle, Security (Maintainer vs. Approver + SoD), and Workflow wiring as
  every other master-data slice. Wired end-to-end: Domain, Application, Infrastructure (EF repo, unique
  index, migration applied to dev + test), Api (`api/v1/masterdata/tax-codes`), frontend (own nav Area,
  i18n keys EN+AR). Verified: 300 tests pass (120 unit incl. 20 new Tax Code tests + 23 integration incl. 3
  new), full solution builds clean, frontend typecheck + Arabic guardrail pass, live API exercise (standard
  + zero-rated codes, out-of-range rate correctly rejected with a 400), live Playwright browser pass in
  both English and Arabic (RTL layout mirroring, correct nav placement).
- One compiler-level thing worth noting: `ArgumentOutOfRangeException` derives from `ArgumentException`,
  so `TaxCodesController` catches only `ArgumentException` — an extra `catch (ArgumentOutOfRangeException)`
  after it is unreachable code and the compiler correctly refused to build it. Fixed immediately.
- **This completes Phase 1 Master Data** — Business Partner, Chart of Accounts, Items, Cost Centers, and
  Tax Codes are all built, tested, and live-verified. `Modules.Finance` (GL/AP/AR/Cash-Bank) is next, per
  the user's "tax code and finance in one go" instruction — continuing directly into Finance scaffolding
  and the first GL Journal Entry slice in this same session.
- Files touched: `src/Modules/Modules.MasterData/Domain/TaxCode.cs`, `Domain/TaxType.cs`,
  `Application/TaxCodeDto.cs`, `Application/TaxCodeService.cs`, `Application/ITaxCodeRepository.cs`,
  `Application/TaxCodeSecurity.cs`, `Application/TaxCodeWorkflow.cs`,
  `Infrastructure/EfTaxCodeRepository.cs`, `Infrastructure/MasterDataDbContext.cs` (TaxCode mapping),
  `Infrastructure/Migrations/20260714154624_AddTaxCode*.cs`, `Api/TaxCodesController.cs`,
  `src/Gateway/Gateway.Api/Program.cs` (DI + security + SoD + workflow + number range),
  `src/Apps/Apps.Shell/src/api/taxCodeApi.ts`, `src/Apps/Apps.Shell/src/pages/TaxCodesPage.tsx`,
  `src/Apps/Apps.Shell/src/i18n/content.ts` (tax.* keys), `src/Apps/Apps.Shell/src/App.tsx` (nav Area +
  routing), `tests/UnitTests/Modules.MasterData.Tests/TaxCodeTests.cs`, `TaxCodeServiceTests.cs`,
  `FakeTaxCodeRepository.cs`, `tests/IntegrationTests/Modules.MasterData.IntegrationTests/
  TaxCodePersistenceTests.cs`, `TestDatabase.cs` (truncate tax_codes),
  `src/Modules/Modules.MasterData/README.md` (added Tax Codes "What's built" section, fixed the stale
  Deferred line).
- Next: `Modules.MasterData.Contracts` (published lookup interfaces so Finance never reaches into
  MasterData's internals — docs/architecture/01 #3.2), then `Modules.Finance` scaffolding and a first real
  GL Journal Entry (Draft → Submit → Approve → Post → Reverse — the first real use of the Posted/Reversed
  lifecycle anywhere in this codebase), then AP Invoice referencing Business Partner + Tax Code via
  Contracts, matching the Phase 1 exit criteria wording exactly: "post/reverse a GL journal and an AP
  invoice end-to-end with full audit trail."

### 2026-07-14 — Cost Centers — Phase 1 Master Data
- Agent: Claude Sonnet 5
- Phase: Phase 1 — Master Data + Finance Core
- Status: Completed
- What changed: Built the Cost Centers master-data entity — the "who owns this cost/revenue
  organizationally" Controlling object every journal line can carry alongside the G/L Account
  (docs/architecture/07-project-accounting-and-financial-architecture.md #1, #4), and the fourth of the
  five Phase 1 Master Data pieces (only Tax codes remain). Mirrors `GLAccount`'s shape rather than `Item`'s:
  unique `CostCenterCode`, bilingual `CostCenterName`/`CostCenterNameArabic`, a self-referencing
  `ParentCostCenterId` hierarchy (e.g. "Head Office" → "Finance Department"), and `IsPostable`
  (header/grouping vs. leaf) — cost center reporting genuinely needs roll-ups, the same reason the chart of
  accounts needs one, unlike Item's deliberately flat catalog. Same BusinessObject lifecycle, Security
  (Maintainer vs. Approver duties + SoD conflict rule), and Workflow (one-step Any-quorum approval) wiring
  as every other master-data slice. Wired end-to-end: Domain entity, Application (DTOs, service, repository
  port, security, workflow config), Infrastructure (EF repo, DbContext mapping with self-ref FK + unique
  index, migration applied to dev + test), Api controller (CRUD + submit/approve/reject at
  `api/v1/masterdata/cost-centers`), frontend (API client, page with list/create/details + parent display,
  its own nav Area from the start this time, i18n keys EN+AR). Verified: 277 tests pass (100 unit incl. 18
  new Cost Center tests + 20 integration incl. 3 new Cost Center persistence/hierarchy/uniqueness tests
  against real Postgres), full solution builds clean, frontend typecheck + Arabic guardrail both pass, API
  exercised live via curl (header/non-postable Head Office + child Finance Department, duplicate-code
  rejection, list — all correct), and a live Playwright browser pass in both English and Arabic (parent
  cost center displays correctly, full RTL layout mirroring, own nav Area shows correctly).
- Nothing new broke — followed the exact `GLAccount` pattern, and the `{page === "cost-centers" ? ... }`
  render branch (the thing the Items slice forgot) was added correctly the first time.
- Files touched: `src/Modules/Modules.MasterData/Domain/CostCenter.cs`, `Application/CostCenterDto.cs`,
  `Application/CostCenterService.cs`, `Application/ICostCenterRepository.cs`,
  `Application/CostCenterSecurity.cs`, `Application/CostCenterWorkflow.cs`,
  `Infrastructure/EfCostCenterRepository.cs`, `Infrastructure/MasterDataDbContext.cs` (CostCenter mapping),
  `Infrastructure/Migrations/20260714152130_AddCostCenter*.cs`, `Api/CostCentersController.cs`,
  `src/Gateway/Gateway.Api/Program.cs` (DI + security + SoD + workflow + number range),
  `src/Apps/Apps.Shell/src/api/costCenterApi.ts`, `src/Apps/Apps.Shell/src/pages/CostCentersPage.tsx`,
  `src/Apps/Apps.Shell/src/i18n/content.ts` (cc.* keys), `src/Apps/Apps.Shell/src/App.tsx` (nav Area +
  routing), `tests/UnitTests/Modules.MasterData.Tests/CostCenterTests.cs`, `CostCenterServiceTests.cs`,
  `FakeCostCenterRepository.cs`, `tests/IntegrationTests/Modules.MasterData.IntegrationTests/
  CostCenterPersistenceTests.cs`, `TestDatabase.cs` (truncate cost_centers),
  `src/Modules/Modules.MasterData/README.md` (added Cost Centers + Home workspace "What's built" sections,
  fixed the stale Deferred line).
- Next: Phase 1's last Master Data slice is Tax codes, then Finance (GL/AP/AR/Cash-Bank). Needs a fresh
  go-ahead before starting — this closes out the "go ahead, in one go, proceed to another roadmap step too"
  authorization for this session.

### 2026-07-14 — Home workspace page replaces plain System Status as the default landing page
- Agent: Claude Sonnet 5
- Phase: Phase 1 — Master Data + Finance Core
- Status: Completed
- What changed: Added a `HomePage.tsx` — a workspace-style landing page (SAP Fiori Launchpad / Dynamics
  365 Workspace pattern: tiles per area, real counts, click-through to the list) — as the default screen
  instead of the diagnostic System Status page (which stays reachable under Platform Administration →
  System). Three tiles (Business Partners, Chart of Accounts, Items), each showing a live total and a
  "N pending approval" line when any records are in Submitted status, sourced by calling the three existing
  list endpoints directly (no new backend endpoint — reuses `listBusinessPartners`/`listGLAccounts`/
  `listItems`, same pattern already used by each list page for its own `top=200` fetch). Clicking a tile
  navigates to that area's list. Purely additive frontend work — no Domain/Application/Infrastructure
  changes, so it carries no cost to the Finance roadmap.
- Verified: frontend typecheck + Arabic guardrail both pass, live browser screenshots in English and
  Arabic (full RTL mirroring, tiles reordered correctly) confirm real counts (14 Business Partners, 1
  Chart of Accounts, 2 Items at the time), and a live test (submit an Item, reload) confirmed the
  "N pending approval" sub-line renders correctly.
- Files touched: `src/Apps/Apps.Shell/src/pages/HomePage.tsx` (new), `src/Apps/Apps.Shell/src/App.tsx`
  (new Home nav module as the first entry, default landing page changed from System Status to Home),
  `src/Apps/Apps.Shell/src/i18n/content.ts` (`nav.homeModule`/`nav.homeArea`/`nav.home`/`home.heading`/
  `home.totalLabel`/`home.pendingApprovalLabel`).
- Next: continuing in the same session to Cost Centers (next Master Data slice).

### 2026-07-14 — Navigation: split one mislabeled "Business Partners" area into three
- Agent: Claude Sonnet 5
- Phase: Phase 1 — Master Data + Finance Core
- Status: Completed
- What changed: Each new Master Data screen (Chart of Accounts, then Items) had been getting bolted onto
  the single nav Area created for Business Partners, so all three ended up living under an Area literally
  labeled "Business Partners" — user-visible confusion, not just internal untidiness. Split into three
  proper Areas under the Master Data module (Business Partners / Chart of Accounts / Items), matching the
  Dynamics 365 Module → Area → screen navigation pattern this app is modeled on
  (docs/architecture/02-business-object-model.md #3). Also renamed the Chart of Accounts and Items menu
  items to "All Accounts"/"All Items" (from "Chart of Accounts"/"Items") now that the Area itself carries
  that name, matching the existing "Business Partners" Area → "All Business Partners" item precedent.
- Files touched: `src/Apps/Apps.Shell/src/App.tsx`, `src/Apps/Apps.Shell/src/i18n/content.ts`
  (`nav.chartOfAccountsArea`, `nav.itemsArea` added; `nav.allGLAccounts`/`nav.allItems` reworded).
- Next: Cost Centers and Tax codes will each get their own Area the same way when built, rather than being
  bolted onto an existing one.

### 2026-07-14 — Items — Phase 1 Master Data
- Agent: Claude Sonnet 5
- Phase: Phase 1 — Master Data + Finance Core
- Status: Completed
- What changed: Built the Items master-data entity — the material/product/service master a Procurement PO
  line, a Construction BOQ line, or (later) an Inventory stock movement all reference (same role as SAP's
  Material Master / D365's Released Product), and the item side of the Phase 1 exit criteria's "the vendors
  and the items/materials those vendors supply." Followed the exact `GLAccount` pattern end-to-end (which
  itself follows Business Partner): `Item : BusinessObject` with unique `ItemCode`, bilingual
  `ItemName`/`ItemNameArabic`, `ItemType` enum (Stock/NonStock/Service), free-text `UnitOfMeasure`,
  `IsActive`; full Draft → Submit → Approve/Reject lifecycle with Security (Maintainer vs. Approver duties +
  SoD conflict rule) and Workflow (one-step Any-quorum approval) wired the same way as Business Partner and
  G/L Account. Deliberately flat, no parent hierarchy (unlike the chart of accounts, an item catalog's
  grouping is a reporting concern for later, not a Phase 1 structural need). Wired end-to-end: Domain
  entity + `ItemType` enum, Application (DTOs, service, repository port, security, workflow config),
  Infrastructure (EF repo, DbContext mapping with unique index on `item_code`, migration applied to dev +
  test), Api controller (CRUD + submit/approve/reject at `api/v1/masterdata/items`), frontend (API client,
  page with list/create/details, nav entry, i18n keys EN+AR). Verified: 254 tests pass (82 unit incl. 18 new
  Item tests + 17 integration incl. 3 new Item persistence/uniqueness tests against real Postgres), full
  solution builds clean, frontend typecheck + Arabic guardrail both pass, API exercised live via curl
  (create Stock + Service item, duplicate-code rejection, submit, approve, list — all correct), and a live
  Playwright browser pass in both English and Arabic (RTL input, full right-to-left layout mirroring).
- One real bug, caught only by looking at the live screenshot: the nav entry and hash routing for the Items
  page were wired into `App.tsx`, but the actual `{page === "items" ? <ItemsPage /> : ...}` render branch
  was initially left out of the ternary — clicking "Items" highlighted it as active in the nav but silently
  kept rendering System Status underneath. `tsc --noEmit` and the Arabic guardrail script both passed the
  whole time (neither checks that a page actually renders) — fixed immediately, re-verified with a fresh
  screenshot.
- Files touched: `src/Modules/Modules.MasterData/Domain/Item.cs`, `Domain/ItemType.cs`,
  `Application/ItemDto.cs`, `Application/ItemService.cs`, `Application/IItemRepository.cs`,
  `Application/ItemSecurity.cs`, `Application/ItemWorkflow.cs`, `Infrastructure/EfItemRepository.cs`,
  `Infrastructure/MasterDataDbContext.cs` (Item mapping), `Infrastructure/Migrations/20260714140759_AddItem*.cs`,
  `Api/ItemsController.cs`, `src/Gateway/Gateway.Api/Program.cs` (DI + security + SoD + workflow + number
  range), `src/Apps/Apps.Shell/src/api/itemApi.ts`, `src/Apps/Apps.Shell/src/pages/ItemsPage.tsx`,
  `src/Apps/Apps.Shell/src/i18n/content.ts` (item.* keys), `src/Apps/Apps.Shell/src/App.tsx` (nav + routing),
  `tests/UnitTests/Modules.MasterData.Tests/ItemTests.cs`, `ItemServiceTests.cs`, `FakeItemRepository.cs`,
  `tests/IntegrationTests/Modules.MasterData.IntegrationTests/ItemPersistenceTests.cs`, `TestDatabase.cs`
  (truncate items), `src/Modules/Modules.MasterData/README.md` (added Chart of Accounts + Items "What's
  built" sections — the Chart of Accounts one was missing entirely until now, backfilled while documenting
  Items).
- Next: Phase 1 continues with Cost Centers and Tax codes (remaining Master Data), then Finance
  (GL/AP/AR/Cash-Bank). Needs a fresh go-ahead before starting.

### 2026-07-14 — Chart of Accounts (G/L Account) — Phase 1 Master Data
- Agent: ZCode (builtin:zai-coding-plan/GLM-5.2)
- Phase: Phase 1 — Master Data + Finance Core
- Status: Completed
- What changed: Built the Chart of Accounts master-data entity (`GLAccount`) — the "GL Account" dimension
  every journal line carries (doc 07 §1) and the second of the two pieces the Phase 1 exit criteria names
  ("maintain its chart of accounts and vendors"). Mirrors the Business Partner slice's established patterns
  exactly — no re-architecting. `GLAccount : BusinessObject` with: unique `AccountCode` (business-facing
  chart position, distinct from the sequential `DocumentNumber` audit id), bilingual `AccountName` +
  `AccountNameArabic`, 5-type `AccountType` enum (Asset/Liability/Equity/Revenue/Expense), derived
  `NormalBalance` (Debit/Credit from type — single source of truth, not stored), self-referencing
  `ParentAccountId` hierarchy, `IsPostable` (header vs leaf), `IsActive` (deactivate not delete). Full
  BusinessObject lifecycle: Draft → Submit → Approve via workflow, audit on every change, SoD security split
  (Maintainer vs Approver), unique-code enforcement at service + DB level. Wired end-to-end: Domain entity +
  enum, Application (DTOs, service, repository port, security, workflow config), Infrastructure (EF repo,
  DbContext mapping with self-ref FK + unique index, migration applied to dev + test), API controller
  (CRUD + submit/approve/reject), frontend (API client, page with list/create/details, nav entry, i18n keys
  EN+AR). Verified: 216 tests pass (64 unit incl. 11 new GLAccount tests + 14 integration incl. 2 new
  persistence round-trip tests against real Postgres), full solution builds clean, frontend Arabic guardrail
  + npm build both pass, API exercised live via curl (create header non-postable + child, submit, approve,
  list — all correct).
- Files touched: `src/Modules/Modules.MasterData/Domain/GLAccount.cs`, `Domain/AccountType.cs`,
  `Application/GLAccountDto.cs`, `Application/GLAccountService.cs`, `Application/IGLAccountRepository.cs`,
  `Application/GLAccountSecurity.cs`, `Application/GLAccountWorkflow.cs`,
  `Infrastructure/EfGLAccountRepository.cs`, `Infrastructure/MasterDataDbContext.cs` (GLAccount mapping),
  `Infrastructure/Migrations/20260714134744_AddGLAccount*.cs`,
  `Api/GLAccountsController.cs`,
  `src/Gateway/Gateway.Api/Program.cs` (DI + security + SoD + workflow + number range),
  `src/Apps/Apps.Shell/src/api/glAccountApi.ts`,
  `src/Apps/Apps.Shell/src/pages/GLAccountsPage.tsx`,
  `src/Apps/Apps.Shell/src/i18n/content.ts` (gl.* keys),
  `src/Apps/Apps.Shell/src/App.tsx` (nav + routing),
  `tests/UnitTests/Modules.MasterData.Tests/GLAccountTests.cs`, `GLAccountServiceTests.cs`,
  `FakeGLAccountRepository.cs`,
  `tests/IntegrationTests/Modules.MasterData.IntegrationTests/GLAccountPersistenceTests.cs`,
  `TestDatabase.cs` (truncate gl_accounts)
- Deferred, disclosed not hidden: account balances (computed from journal lines — Finance's job), posting
  validation ("can you post to this account?" — Finance's concern), multi-company CoA (Phase 1 uses C001),
  standard Saudi CoA template import, contra-account normal-balance overrides (derive-from-type is the
  Phase 1 baseline). No attachments/notes wired for GLAccount yet (the platform services exist and would
  be added the same way as Business Partner if needed — not a Phase 1 exit-criteria requirement for the
  chart itself).
- Note: this entity's initial implementation used the class/table/route name `GlmAccount`/`glm_accounts`/
  `glm-accounts` (the generating tool's own model name, not a real domain term). Claude Sonnet 5 renamed it
  to `GLAccount`/`gl_accounts`/`gl-accounts` end-to-end (Domain/Application/Infrastructure/Api/tests/
  frontend/i18n) before this was ever committed, rebuilding the EF migration from scratch (dropped and
  re-applied to both dev and test databases) and re-verifying with the full test suite, a live API exercise,
  and a live browser screenshot — all still green. No functional change, naming only. Disclosed here rather
  than silently fixed, per this file's own append-only/no-hidden-corrections principle.
- Next: Phase 1 continues with Items, Cost Centers, and Tax codes (remaining Master Data), then Finance
  (GL/AP/AR/Cash-Bank). Needs a fresh go-ahead before starting.

### 2026-07-14 — Business Partner: bilingual (Arabic) name field added
- Agent: Claude Sonnet 5
- Phase: Phase 1
- Status: Completed
- What changed: Added an optional `NameArabic` field to `BusinessPartner` (`UpdateNameArabic`), set at
  creation only (`CreateBusinessPartnerRequest.NameArabic`) — same precedent as `TaxRegistrationNumber`, no
  update endpoint yet. Motivated by ZATCA e-invoicing, which requires the seller's Arabic legal name on a
  tax invoice; not yet enforced as a requirement before Approved status (disclosed in the module README's
  Deferred section — revisit once Finance/invoicing exists). Wired end-to-end: Domain property, EF mapping
  + migration (`name_arabic`, nullable `varchar(200)`), `BusinessPartnerDto`/`CreateBusinessPartnerRequest`,
  frontend API client, i18n key (`bp.fieldNameArabic`), and a new `dir="rtl"` input on the create form plus
  a matching row in the details view's General FastTab. Verified with a domain unit test, a service unit
  test, an extended integration test (round-trips literal Arabic text through a fresh DbContext against
  real PostgreSQL), a direct API `curl` exercise, and a live Playwright browser pass — screenshots confirmed
  the RTL input renders correctly on the create form, the value displays correctly in the English-language
  details view, and the whole page (including this field) correctly mirrors to a right-to-left layout when
  switched to Arabic (`<html dir="rtl">`). All 213 existing tests still pass; 2 new tests added. Nothing
  broke — this was the last item in the Audit → Workflow → Security → Attachments/Notes → bilingual-names
  sequence the user asked to complete this session.
- Files touched: `src/Modules/Modules.MasterData/Domain/BusinessPartner.cs`,
  `src/Modules/Modules.MasterData/Infrastructure/MasterDataDbContext.cs`,
  `src/Modules/Modules.MasterData/Infrastructure/Migrations/20260714093432_AddBusinessPartnerNameArabic*.cs`,
  `src/Modules/Modules.MasterData/Application/BusinessPartnerDto.cs`,
  `src/Modules/Modules.MasterData/Application/BusinessPartnerService.cs`,
  `src/Apps/Apps.Shell/src/api/businessPartnerApi.ts`, `src/Apps/Apps.Shell/src/i18n/content.ts`,
  `src/Apps/Apps.Shell/src/pages/BusinessPartnersPage.tsx`,
  `tests/UnitTests/Modules.MasterData.Tests/BusinessPartnerTests.cs`,
  `tests/UnitTests/Modules.MasterData.Tests/BusinessPartnerServiceTests.cs`,
  `tests/IntegrationTests/Modules.MasterData.IntegrationTests/BusinessPartnerPersistenceTests.cs`,
  `src/Modules/Modules.MasterData/README.md`
- Next: Phase 1 continues with Chart of Accounts, Items, Cost Centers, and Tax codes, then Finance —
  not yet started, needs a fresh go-ahead before beginning (this entry closes out the sequence the user
  explicitly authorized: Audit → Workflow → Security → Attachments/Notes → bilingual names).

### 2026-07-14 — New Platform.Notes capability, wired into Business Partner (last BusinessObject guarantee closed)
- Agent: Claude Sonnet 5
- Phase: Phase 1 — Master Data + Finance Core
- Status: Completed — this closes every gap in the original "what does BusinessObject really embody"
  challenge (Identity/Status/Lifecycle/Audit/Attachments/Notes/Workflow/Localization/ExtensionData/
  Concurrency/Permissions): Audit, Workflow, Security, Attachments, and now Notes are all real and wired
  into Business Partner. See `Platform.Core/README.md`'s guarantee table for the full picture.
- What changed:
  - **Built `Platform.Notes` from scratch** — new project, new test project (6 unit tests): `Note` (one
    free-text note, linked to a Business Object via the same flat `(BusinessObjectType, BusinessObjectId)`
    pair `Platform.Attachments`/`Platform.Workflow` already use), `INoteRepository` (storage-agnostic
    port), `INoteService`/`NoteService` (validates non-empty text and a 2000-character ceiling). Notes are
    deliberately **append-only/delete-only** — no `UpdateText` — matching the platform's existing
    "correct by reversal, not by silent edit" principle.
  - Implemented `EfNoteRepository` in `Modules.MasterData.Infrastructure` — a single `notes` table,
    indexed the same way `attachments` is, new migration applied to both dev and test databases.
  - Wired into `BusinessPartnerService`: `AddNoteAsync`/`ListNotesAsync`/`DeleteNoteAsync`, gated by the
    same Maintainer privilege as everything else. `DeleteNoteAsync` verifies the note actually belongs to
    the requested partner before removing it — the same ownership check Attachments already does. Added
    matching endpoints to `BusinessPartnersController` and a Notes FastTab to the frontend (a textarea +
    Add Note button, a table of existing notes with a Delete button on each).
  - **Verified live**: full solution builds with 0 errors, every test project passes (211 tests across the
    solution: 6 new in `Platform.Notes.Tests`, 5 new in `Modules.MasterData.Tests`, 2 new integration tests
    proving a note round-trips through real PostgreSQL via a fresh `DbContext`), then exercised the real
    running API end-to-end with `curl` (add, list, delete, confirmed empty after delete) and confirmed the
    full add/list/delete flow live in a real browser with no console errors.
  - Nothing new broke — both `Platform.Attachments` (previous entry) and `Platform.Notes` followed the
    exact `WorkflowInstance` persistence pattern already proven correct (flat BusinessObjectType/Id,
    private-constructor ORM materialization), so this slice was low-risk by construction.
- Files touched: `src/Platform/Platform.Notes/*` (new project: `Note`, `INoteRepository`, `INoteService`,
  `NoteService`, `README`), `tests/UnitTests/Platform.Notes.Tests/*` (new project),
  `src/Modules/Modules.MasterData/Infrastructure/{EfNoteRepository,MasterDataDbContext,
  Modules.MasterData.Infrastructure.csproj}`, `src/Modules/Modules.MasterData/Infrastructure/
  Migrations/*AddNotes*` (new), `src/Modules/Modules.MasterData/Application/
  {BusinessPartnerService,BusinessPartnerDto,Modules.MasterData.Application.csproj}`,
  `src/Modules/Modules.MasterData/Api/BusinessPartnersController.cs`, `src/Gateway/Gateway.Api/Program.cs`,
  `src/Apps/Apps.Shell/src/{api/businessPartnerApi.ts,pages/BusinessPartnersPage.tsx,i18n/content.ts,
  App.css}`, `tests/UnitTests/Modules.MasterData.Tests/{BusinessPartnerServiceTests,FakeNoteRepository,
  Modules.MasterData.Tests.csproj}`, `tests/IntegrationTests/Modules.MasterData.IntegrationTests/
  {NotePersistenceTests,TestDatabase}.cs`, `src/Modules/Modules.MasterData/README.md`,
  `src/Platform/Platform.Core/README.md`, `erp-platform.sln`
- Next: Add bilingual (Arabic + English) name fields to `BusinessPartner` (ZATCA/KSA legal requirement) —
  the last item in the user's originally chosen sequencing. Only after that does Phase 1 continue to
  Chart of Accounts/Items/Cost Centers/Tax codes and Finance.

### 2026-07-14 — New Platform.Attachments capability, wired into Business Partner
- Agent: Claude Sonnet 5
- Phase: Phase 1 — Master Data + Finance Core
- Status: Completed
- What changed:
  - **Built `Platform.Attachments` from scratch** — one of the guarantees `Platform.Core.BusinessObject`
    was supposed to carry (Identity/Status/Lifecycle/Audit/Attachments/Notes/Workflow/Localization/
    ExtensionData/Concurrency/Permissions) that had never been built at all, per the gap the user caught
    when pressing on what a Business Object really embodies. New project, new test project (9 unit tests):
    `AttachmentMetadata` (file metadata, deliberately without the file bytes as a property — listing
    attachments must never load every file's content just to show a filename/size),
    `IAttachmentRepository` (a storage-agnostic port, same "kernel defines it, a module with a real
    database implements it" pattern as `IWorkflowInstanceRepository`), and `IAttachmentService`/
    `AttachmentService` (the single entry point modules call — a 10 MB size ceiling and a content-type
    **allowlist**, not a denylist, rejecting anything that isn't PDF/PNG/JPEG/Word/Excel; no executable,
    script, or macro-capable format is ever accepted).
  - Implemented `EfAttachmentRepository` in `Modules.MasterData.Infrastructure` — file metadata in a new
    `attachments` table, bytes in a separate `attachment_contents` table (cascade-deleted with the
    metadata row), new migration applied to both dev and test databases.
  - Wired into `BusinessPartnerService`: `AddAttachmentAsync`/`ListAttachmentsAsync`/
    `DownloadAttachmentAsync`/`DeleteAttachmentAsync`, gated by the same Maintainer privilege as everything
    else (uploading a document is a maintenance action). `DownloadAttachmentAsync` double-checks the
    attachment's own business-object id matches the requested partner before returning it, so guessing an
    id can never fetch another partner's file. Added matching endpoints to `BusinessPartnersController`
    (`POST/GET/DELETE .../{id}/attachments...`, `multipart/form-data` upload, correct
    `Content-Type`/`Content-Disposition` on download).
  - **Also closed a small, separate, pre-existing gap while touching this controller**: added a `Reject`
    button to the frontend next to Approve — the backend endpoint has existed since the Workflow slice but
    was never exposed in the UI until now.
  - **Verified live**: full solution builds with 0 errors, every test project passes (198 tests across the
    solution: 9 new in `Platform.Attachments.Tests`, 5 new in `Modules.MasterData.Tests`, 2 new integration
    tests proving metadata + bytes round-trip through real PostgreSQL via a fresh `DbContext`), then
    exercised the real running API end-to-end with `curl` — upload, list, download (byte-for-byte match),
    delete, and confirmed the content-type allowlist actually rejects a disallowed upload (`400`). Also
    verified the full upload/list/download/delete flow live in a real browser with no console errors.
- Files touched: `src/Platform/Platform.Attachments/*` (new project: `AttachmentMetadata`,
  `IAttachmentRepository`, `IAttachmentService`, `AttachmentService`, `README`),
  `tests/UnitTests/Platform.Attachments.Tests/*` (new project), `src/Modules/Modules.MasterData/
  Infrastructure/{EfAttachmentRepository,AttachmentContentRow,MasterDataDbContext,
  Modules.MasterData.Infrastructure.csproj}`, `src/Modules/Modules.MasterData/Infrastructure/
  Migrations/*AddAttachments*` (new), `src/Modules/Modules.MasterData/Application/
  {BusinessPartnerService,BusinessPartnerDto,Modules.MasterData.Application.csproj}`,
  `src/Modules/Modules.MasterData/Api/{BusinessPartnersController,Modules.MasterData.Api.csproj}`,
  `src/Gateway/Gateway.Api/Program.cs`, `src/Apps/Apps.Shell/src/{api/businessPartnerApi.ts,
  pages/BusinessPartnersPage.tsx,i18n/content.ts}`, `tests/UnitTests/Modules.MasterData.Tests/
  {BusinessPartnerServiceTests,FakeAttachmentRepository,Modules.MasterData.Tests.csproj}`,
  `tests/IntegrationTests/Modules.MasterData.IntegrationTests/{AttachmentPersistenceTests,TestDatabase}.cs`,
  `src/Modules/Modules.MasterData/README.md`, `src/Platform/Platform.Core/README.md`,
  `erp-platform.sln`
- Next: Build Notes into `Platform.Core` (the last of the never-built BusinessObject guarantees), then add
  bilingual (Arabic + English) name fields to `BusinessPartner` (ZATCA/KSA legal requirement). Only after
  those does Phase 1 continue to Chart of Accounts/Items/Cost Centers/Tax codes and Finance.

### 2026-07-14 — Business Partner: Platform.Security wired in (third and last of the Audit/Workflow/Security gap-fix pass)
- Agent: Claude Sonnet 5
- Phase: Phase 1 — Master Data + Finance Core
- Status: Completed — closes out the full Audit → Workflow → Security gap-fix pass the user requested
- What changed:
  - **Wired `Platform.Security` into `BusinessPartnerService`/`BusinessPartnersController`** — the last of
    the three kernel services (Audit, Workflow, Security) that existed and were tested in isolation since
    Phase 0 but were never actually consumed by the first real business module. `BusinessPartnerSecurity`
    (new, in `Modules.MasterData.Application`) registers a deliberately split Duty pair — Maintainer
    (create/add address/add contact/submit) and Approver (approve/reject) — plus a real SoD conflict rule
    between them: the exact "Create Vendor vs. Approve Vendor Payment" example this module's own domain
    comments already referenced without ever enforcing it. Every public `BusinessPartnerService` method now
    calls `IAuthorizationService.Authorize(...)` first and throws `UnauthorizedAccessException` on denial.
  - Added `PlatformApiController.ForbiddenError()` + `ApiErrorEnvelope.Forbidden()` (403) — the first
    module needed this response shape, so it's added to the shared base rather than reinvented locally.
  - `BusinessPartnersController` now uses two distinct hardcoded actors — `"system/ui"` (Maintainer) for
    create/edit/submit, `"system/approver"` (Approver) for approve/reject — instead of one `"system/ui"`
    for everything. Not cosmetic: this is what makes the SoD split real even without per-user login (the
    same actor can never both create and approve).
  - Added `Platform.Security.IActorRoleAssignmentStore` (+ `InMemoryActorRoleAssignmentStore`) — a real,
    if temporary, actor-to-Role resolver, replacing the prior workflow-eligibility shim that granted the
    approver Role to whichever actor string was passed unconditionally. An actor with no assignment now
    resolves to zero Roles (denied by default, not granted by default) — the same Role
    (`MasterData.ApproveBusinessPartner`) still serves double duty for both Workflow eligibility and
    Security's privilege-grant resolution, so one Role means "can approve" everywhere.
  - Registered `Platform.Security.Sod`'s `ISodEngine`/`ISodExceptionLog` in `Gateway.Api`'s DI container for
    the first time (previously built and tested in `Platform.Security.Tests` but never actually wired into
    the running application). Proved the registered conflict rule is caught by the real `SodEngine` via a
    unit test — disclosed that it isn't checked anywhere in the live request path yet, since there is no
    role-*assignment* admin surface in the application to check it against (only role *definitions* exist).
  - **Verified live**: full solution builds with 0 errors, every test project passes (32 unit tests in
    Modules.MasterData.Tests including 4 new Security-specific ones, plus a new `Forbidden` envelope test
    in Platform.Api.Tests, 8 integration tests against real PostgreSQL — 115 tests total across the
    solution), then exercised the real running API end-to-end (create → submit → approve, both now
    passing through real authorization checks) and confirmed via `/api/v1/system/status` that the audit
    chain remained valid. Also verified the existing frontend Submit/Approve buttons live in a real browser
    with no console errors — no frontend changes were needed, the API contract didn't change shape.
- Files touched: `src/Platform/Platform.Security/{IActorRoleAssignmentStore,InMemoryActorRoleAssignmentStore,
  README}`, `src/Platform/Platform.Api/{ApiErrorEnvelope,PlatformApiController,README}`,
  `src/Modules/Modules.MasterData/Application/{BusinessPartnerSecurity,BusinessPartnerService,
  Modules.MasterData.Application.csproj}`, `src/Modules/Modules.MasterData/Api/BusinessPartnersController.cs`,
  `src/Gateway/Gateway.Api/Program.cs`, `tests/UnitTests/Modules.MasterData.Tests/BusinessPartnerServiceTests.cs`,
  `tests/UnitTests/Platform.Api.Tests/ApiErrorEnvelopeTests.cs`, `src/Modules/Modules.MasterData/README.md`
- Next: This closes the Audit/Workflow/Security gap-fix pass the user explicitly requested after catching
  that `BusinessObject`'s full guarantee set (Identity/Status/Lifecycle/Audit/Attachments/Notes/Workflow/
  Localization/ExtensionData/Concurrency/Permissions) wasn't actually all wired into Business Partner. Per
  the user's chosen sequencing, remaining: build Attachments into `Platform.Core` (never built at all —
  next up, with Vendor Prequalification's future supporting documents as a natural real use case, see
  `docs/architecture/06-roadmap.md` Phase 2), then Notes, then bilingual (Arabic + English) name fields on
  `BusinessPartner`. Only after those does Phase 1 continue to Chart of Accounts/Items/Cost Centers/Tax
  codes and Finance.

### 2026-07-14 — Branding: the application is named HadionERP, by hAdisHere, created by aHmAr
- Agent: Claude Sonnet 5
- Phase: Architecture Baseline (cross-cutting; touches every layer, not a specific module phase)
- Status: Completed
- What changed: The generic placeholder name ("ERP Platform") is replaced everywhere with the real product
  identity — **HadionERP**, by **hAdisHere**, created by **aHmAr**:
  - Frontend: browser tab title, the shell header (title + a small muted "by hAdisHere" tagline next to
    it — added an optional `tagline` prop to `Platform.UI`'s `ShellBar`), and a new app-wide footer
    ("HadionERP by hAdisHere — Created by aHmAr"). All new text goes through the existing i18n system
    (`content.ts`) in both English and Arabic, same as everything else in this app — nothing hardcoded,
    verified by the existing hardcoded-Arabic guardrail script.
  - Backend: `SystemController`'s `application` field (what the System Status page displays), the
    application-started event payload, and the localization-check welcome message all say "HadionERP" now
    instead of "ERP Platform".
  - Top-level docs: `CLAUDE.md`, `AGENTS.md`, `ARCHITECTURE.md` (including its old "Codename: (TBD)"
    placeholder), `HOW-TO-RUN.md`, and this file's own header all now name the product explicitly, so any
    future session (human or AI) sees the real name immediately rather than a generic description.
  - Verified live: full solution builds, all test projects pass (10 projects, including the architecture
    and hardcoded-Arabic guardrails), and confirmed in a real running browser — tab title, English and
    Arabic shell — with no console errors, plus confirmed `/api/v1/system/status` returns `"HadionERP"`.
- Files touched: `src/Apps/Apps.Shell/index.html`, `src/Apps/Apps.Shell/src/i18n/content.ts`,
  `src/Apps/Apps.Shell/src/App.tsx`, `src/Apps/Apps.Shell/src/App.css`,
  `src/Platform/Platform.UI/components/{ShellBar.tsx,components.css}`,
  `src/Gateway/Gateway.Api/Controllers/SystemController.cs`,
  `src/Gateway/Gateway.Api/Localization/GatewayApiLocalizationDefaults.cs`,
  `src/Gateway/Gateway.Api/Program.cs`, `CLAUDE.md`, `AGENTS.md`, `ARCHITECTURE.md`, `HOW-TO-RUN.md`,
  `PROGRESS.md`
- Next: No functional follow-up required. A real logo/favicon (currently still the generic placeholder
  SVG) is a separate, later design task if wanted. The current Phase 1 queue is unaffected: `Platform.Security`
  wiring into Business Partner remains next, then Attachments/Notes, then bilingual name fields.

### 2026-07-14 — Design captured: Vendor Prequalification + Business Roles (documentation only, no code)
- Agent: Claude Sonnet 5
- Phase: Architecture Baseline / Phase 2 — Procurement (design captured ahead of the phase starting)
- Status: Completed (documentation only — no code changes this entry)
- What changed: The user described how large Saudi construction/EPC owners (Aramco-style vendor
  registration, MOMRAH contractor classification, ISO/HSE prequalification) actually prequalify vendors —
  by role and trade/specialty, scored across commercial/legal/technical/HSE/quality criteria, time-bound
  with re-qualification — and gave an example Business Roles list (Supplier, Subcontractor, Consultant,
  Client, Joint Venture, Government Authority, Rental Company, Manufacturer). Discussed and captured the
  design in `docs/architecture/06-roadmap.md` (Phase 2) rather than implementing now, per explicit user
  decision on both open questions:
  - **Module ownership**: a future `Modules.Procurement` owns the actual `VendorPrequalification` Business
    Object and its workflow, NOT `Modules.MasterData` — prequalification is a procurement process against
    a master-data party, not master data itself (matches SAP Ariba SLP / Dynamics Vendor Onboarding).
  - **Timing**: document now, build later — `BusinessPartner.PartnerType` (single enum) becoming a
    multi-select `BusinessRoles` collection is noted under Phase 1 in the roadmap and in
    `Modules.MasterData/README.md`'s Deferred list, but not implemented this entry, since Attachments
    (needed to hold prequalification's supporting documents) isn't built yet either and Phase 2 hasn't
    started.
  - Design nuances worked through and recorded: "Client" should be one role, not two (it's the
    construction-industry label for what SAP calls Customer/Debtor — the same AR-invoiced counterparty);
    "Government Authority" is never prequalified (no commercial relationship, exists only so
    permit/license correspondence has somewhere to attach) — prequalification logic must be conditional on
    role, reusing `Platform.Workflow`'s existing `AttributeConstraints` step-condition mechanism, not a
    blanket process; "Joint Venture" as a simple role can't capture the real partnership (who, which
    project, ownership split) — deliberately left as a simple role for now, real JV modeling deferred to a
    future Project/Contract concern; each role-family (especially Manpower Supplier, Testing Laboratory,
    Rental Company, Manufacturer) needs its own prequalification criteria template, not one generic
    checklist, since their real-world qualification requirements are genuinely different (GOSI/Iqama/WPS
    vs. ISO/IEC 17025 lab accreditation vs. equipment calibration records vs. factory audit).
- Files touched: `docs/architecture/06-roadmap.md`, `src/Modules/Modules.MasterData/README.md`
- Next: No immediate follow-up required by this entry. When Phase 2 (`Modules.Procurement`) actually
  starts, revisit the roadmap's Phase 2 section for the full captured design before scaffolding the module.
  The current queue is unaffected: `Platform.Security` wiring into Business Partner remains next, then
  Attachments/Notes into `Platform.Core`, then bilingual name fields.

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
