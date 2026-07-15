# Modules.MasterData

Business Partners (customers/vendors), Chart of Accounts, Items, Cost Centers, Tax codes, Number range
definitions (docs/architecture/01-architecture-foundation.md #3.1). **The first business module built**,
and the first real, persisted (PostgreSQL-backed) data in the application — everything before this was
platform-kernel infrastructure using in-memory storage, which was fine for kernel demo/status data but not
for real business records that must survive a restart.

## What's built (Phase 1, slice 1: Business Partner)

- **Domain**: `BusinessPartner` (extends `Platform.Core.BusinessObject`) — Name, tax registration number,
  an optional `NameArabic` (`UpdateNameArabic`), and three child collections: `Addresses`, `Contacts`, and
  `BusinessRoles` (see the "Phase 2: BusinessRoles" section below — this replaced the original single
  `PartnerType` enum once Phase 2 was actually reached). `NameArabic` exists because ZATCA e-invoicing
  requires the seller's Arabic legal name on a tax invoice — it's optional at the Domain level today (not
  every partner will be invoiced yet) but see Deferred for what isn't enforced.
  Uses the standard BO lifecycle (Draft → Submit → Approve) since new-partner onboarding is a real
  fraud/compliance control point (docs/architecture/03-platform-services.md #2.2's Segregation of Duties
  example); adding/removing an address or contact is NOT gated by lifecycle status (a deliberate
  difference from transactional documents — correcting a vendor's address isn't a "reversal").
  - `BusinessPartnerAddress` (child entity, `internal` constructor — only creatable via
    `BusinessPartner.AddAddress`): `AddressType` (HeadOffice/Billing/Shipping/SiteOffice), Country, City,
    AddressLine. Multiple addresses of the same type are allowed on purpose (e.g. several active
    SiteOffice addresses for different projects) — a real construction company has exactly this shape,
    not one address per company.
  - `BusinessPartnerContact` (child entity, same pattern via `AddContact`): Name, JobTitle, Email, Phone.
    Replaces what was originally a single flat email/phone pair on `BusinessPartner` itself — a real
    company has several contact people (Procurement Manager, Accountant, CEO, Site Engineer), each with
    their own phone/email, not one shared pair for the whole company.
- **Application**: `BusinessPartnerService` (orchestration only — business rules live on the Domain
  object), `IBusinessPartnerRepository` (the persistence port). Calls `Platform.Audit`'s `IAuditRecorder`
  at every audit-relevant point — `RecordCreate` on create, `RecordFieldUpdate` on each address/contact
  added (`FieldName` "Addresses"/"Contacts", `NewValueJson` the serialized child, `OldValueJson` null since
  there's nothing to diff against for an append), `RecordStatusTransition` on Submit/Approve/Reject. This is
  consuming the platform service, not reimplementing it (CLAUDE.md's "audit ... as platform services
  consumed from `src/Platform/*`") — the actual capture/hash-chaining logic lives entirely in
  `Platform.Audit`; this layer only decides *when* to call it.
  - **`Platform.Workflow` is wired the same way.** `BusinessPartnerWorkflow.SubmitApprovalDefinition`
    (one Any-quorum step, role `MasterData.ApproveBusinessPartner`) is this module's own registered
    approval matrix. `SubmitAsync` starts a workflow instance instead of approving directly;
    `ApproveAsync`/`RejectAsync` now *decide* the pending instance (`IWorkflowEngine.Decide`) and only
    apply `BusinessPartner`'s own Approved/Rejected transition once the workflow itself reaches that final
    state — a submitted partner genuinely waits on a real approval gate now, not an unconditional call.
    Added a `RejectAsync`/`POST .../{id}/reject` that didn't exist before this (Reject was reachable on the
    Domain object since Phase 1 but had no service method or endpoint — an existing gap this work also
    closed, not a new feature invented for its own sake, since a workflow's Reject decision needs
    somewhere to land).
  - **`Platform.Security` is wired in too** — `BusinessPartnerSecurity` registers a real, deliberately
    split pair of Duties: `MasterData.BusinessPartner.Maintainer` (create, add address/contact, submit)
    and `MasterData.BusinessPartner.Approver` (approve/reject), plus the SoD conflict rule between them —
    the classic "Create Vendor vs. Approve Vendor Payment" example from
    docs/architecture/03-platform-services.md #2.2, which this module's own domain comments already
    referenced without ever actually enforcing. Every public `BusinessPartnerService` method now calls
    `IAuthorizationService.Authorize(...)` first and throws `UnauthorizedAccessException` on denial
    (mapped to a 403 by `BusinessPartnersController`) — replacing what used to be an unconditional grant.
    `BusinessPartnersController` now uses two distinct hardcoded actors, `"system/ui"` (Maintainer) for
    create/edit/submit and `"system/approver"` (Approver) for approve/reject, instead of one `"system/ui"`
    for everything — not cosmetic: it's what makes the SoD split real even without per-user login. The
    Approver Role key (`MasterData.ApproveBusinessPartner`) is deliberately the same one
    `BusinessPartnerWorkflow`'s step already used, so one Role means "can approve" to both the workflow
    engine's eligibility check and Security's privilege-grant resolution.
  - **`Platform.Attachments` is wired in too** — one of the guarantees `Platform.Core.BusinessObject` was
    supposed to carry but had never been built at all, per the gap the user caught.
    `AddAttachmentAsync`/`ListAttachmentsAsync`/`DownloadAttachmentAsync`/`DeleteAttachmentAsync` call
    `Platform.Attachments.IAttachmentService` — upload/delete are gated by the same Maintainer privilege
    as everything else (uploading a document is a maintenance action, not a separate permission).
    `DownloadAttachmentAsync` double-checks the attachment's own `BusinessObjectId` matches the requested
    partner before returning it, so an id guessed for one partner can never fetch another partner's file.
    First real use case: Business Partner onboarding documents (CR copy, GOSI certificate, ISO
    certificates, bank letter) — exactly the supporting documents Vendor Prequalification's future design
    already anticipated (see `docs/architecture/06-roadmap.md` Phase 2).
  - **`Platform.Notes` is wired in too** — the last of the guarantees that had never been built at all.
    `AddNoteAsync`/`ListNotesAsync`/`DeleteNoteAsync` call `Platform.Notes.INoteService`, gated by the same
    Maintainer privilege. Notes are append-only/delete-only (no edit — see `Platform.Notes`'s own doc
    comment on `Note` for why), and `DeleteNoteAsync` verifies the note actually belongs to the requested
    partner before removing it, the same ownership check `DownloadAttachmentAsync`/`DeleteAttachmentAsync`
    already do.
- **Infrastructure**: `MasterDataDbContext` (EF Core, Postgres, its own `masterdata` schema — physically
  enforcing the module-boundary rule at the database level), `EfBusinessPartnerRepository`,
  `EfCoreNumberRangeService` (a real, atomic `INSERT ... ON CONFLICT DO UPDATE ... RETURNING`
  implementation — not a naive read-then-write, which would let concurrent requests hand out duplicate
  document numbers). Addresses/Contacts are mapped as owned child tables
  (`business_partner_addresses`/`business_partner_contacts`) via their private backing fields, cascade
  deleted with the parent.
  - **`EfWorkflowInstanceRepository`** implements `Platform.Workflow.IWorkflowInstanceRepository` — the
    first real persistence for `WorkflowInstance` anywhere in the codebase (previously only proven
    in-memory by `Platform.Workflow.Tests`; a running approval needs to survive between separate HTTP
    requests). Mapped into its own `workflow_instances` table with `ApplicableSteps`/history/approver-set
    fields stored as `jsonb` (same choice as `ExtensionFields`), each with an explicit `ValueComparer` —
    see bug 7 below for why that matters here specifically.
  - **`EfAttachmentRepository`** implements `Platform.Attachments.IAttachmentRepository` — file metadata in
    an `attachments` table, bytes in a separate `attachment_contents` table (cascade-deleted with the
    metadata row) so that listing attachments never has to load every file's full content just to show a
    filename and size (see `AttachmentMetadata`'s own doc comment in `Platform.Attachments`).
  - **`EfNoteRepository`** implements `Platform.Notes.INoteRepository` — a single `notes` table, indexed
    on `(business_object_type, business_object_id)` the same way `attachments` is.
- **Api**: `BusinessPartnersController` (inherits `Platform.Api.PlatformApiController`) at
  `api/v1/masterdata/business-partners`, with `POST .../{id}/addresses` and `POST .../{id}/contacts` to
  append a child (there is no update/remove endpoint yet — see Deferred), `POST .../{id}/reject` alongside
  the existing `.../submit`/`.../approve`, `POST .../{id}/attachments` (multipart file upload),
  `GET .../{id}/attachments` (list metadata), `GET .../{id}/attachments/{attachmentId}/content` (download,
  correct `Content-Type`/`Content-Disposition`), `DELETE .../{id}/attachments/{attachmentId}`, and
  `POST/GET/DELETE .../{id}/notes[/{noteId}]`.
- **Frontend**: `src/Apps/Apps.Shell/src/pages/BusinessPartnersPage.tsx` — the first real business screen
  (List + create/details), using `Platform.UI`'s `ActionPane`/`FastTabs`. The details view has
  Addresses/Contacts/Attachments/Notes FastTabs, each showing the existing rows plus an inline add form (a
  file picker + Upload button for Attachments, a textarea + Add Note button for Notes); a Reject button
  now sits alongside Approve on a Submitted partner (the backend endpoint existed since the Workflow slice
  but was never exposed in the UI until now). Not using a shared "List+Details form template" yet —
  that's deferred until a second business object needs the same shape (see `Platform.UI/README.md`), so
  the common pattern gets extracted from real usage, not guessed at.

## What's built (Phase 1, slice 2: Chart of Accounts / G/L Account)

- **Domain**: `GLAccount` — the "GL Account" dimension every journal line carries
  (docs/architecture/07-project-accounting-and-financial-architecture.md #1), and the second of the two
  Phase 1 exit-criteria master-data pieces ("maintain its chart of accounts and vendors"). Unique
  `AccountCode`, bilingual `AccountName`/`AccountNameArabic`, 5-type `AccountType` enum
  (Asset/Liability/Equity/Revenue/Expense), derived `NormalBalance` (not stored — single source of truth is
  the type), self-referencing `ParentAccountId` hierarchy for roll-ups (e.g. "Current Assets" → "Cash"),
  `IsPostable` (header vs. leaf — journal lines can only touch leaf accounts), `IsActive` (deactivate, not
  delete). Same BusinessObject lifecycle, Security/SoD split (Maintainer vs. Approver), and Workflow wiring
  as Business Partner. Originally built by ZCode (GLM-5.2) as `GlmAccount`/`glm_accounts`/`glm-accounts` —
  its own model name, not a real domain term — then renamed end-to-end to `GLAccount`/`gl_accounts`/
  `gl-accounts` by Claude Sonnet 5 before it was ever committed (class/file/table/route/i18n-keys, migration
  regenerated from scratch), fully re-verified afterward. No attachments/notes wired for GLAccount (not a
  Phase 1 exit-criteria requirement for the chart itself — would be added the same way as Business Partner
  if a real need shows up).
- **Api**: `GLAccountsController` at `api/v1/masterdata/gl-accounts` — CRUD + submit/approve/reject, same
  pattern as `BusinessPartnersController`.
- **Frontend**: `GLAccountsPage.tsx` — list/create/details, parent-account display, bilingual name field.

## What's built (Phase 1, slice 3: Items)

- **Domain**: `Item` — the material/product/service master record a Procurement PO line, a Construction
  BOQ line, or (later) an Inventory stock movement all reference, same role as SAP's Material Master or
  D365's Released Product; the item side of "the vendors and the items/materials those vendors supply."
  Unique `ItemCode`, bilingual `ItemName`/`ItemNameArabic`, `ItemType` enum (Stock/NonStock/Service — Stock
  is warehouse-tracked, NonStock is expensed on receipt, Service has no physical quantity at all, e.g.
  subcontract labor or equipment-rental hours), `UnitOfMeasure` (free-text for Phase 1 — see Deferred),
  `IsActive`. Same BusinessObject lifecycle, Security/SoD split, and Workflow wiring as Business Partner and
  G/L Account — adding a miscoded/duplicate item to the master pollutes every PO/BOQ line that references it
  afterward, the same control-point reasoning as the other two master-data slices. Deliberately flat, no
  parent hierarchy (unlike G/L Account's chart, an item catalog's grouping is a reporting concern, not a
  structural one — deferred until a real Item Group need shows up).
- **Api**: `ItemsController` at `api/v1/masterdata/items` — CRUD + submit/approve/reject, same pattern as
  `GLAccountsController`.
- **Frontend**: `ItemsPage.tsx` — list/create/details, bilingual name field, item-type dropdown.
- Nothing new broke building this — followed the exact `GLAccount` pattern already proven correct. One real
  bug caught during live browser verification (not by any automated test): the nav entry and hash routing
  for the Items page were wired in `App.tsx`, but the actual `{page === "items" ? <ItemsPage /> : ...}`
  render branch was initially left out — clicking "Items" in the nav highlighted it as active but silently
  kept showing the System Status page underneath. Caught by looking at the live screenshot, not by
  `tsc --noEmit` (both branches type-check fine on their own) or the guardrail script — a reminder that
  "the build passes" and "the feature works" are genuinely different claims.

## What's built (Phase 1, slice 4: Cost Centers)

- **Domain**: `CostCenter` — the "who owns this cost/revenue organizationally" Controlling object every
  journal line can carry alongside the G/L Account
  (docs/architecture/07-project-accounting-and-financial-architecture.md #1, #4), and the fourth Phase 1
  Master Data piece. Mirrors `GLAccount`'s shape, not `Item`'s: unique `CostCenterCode`, bilingual
  `CostCenterName`/`CostCenterNameArabic`, a self-referencing `ParentCostCenterId` hierarchy (e.g. "Head
  Office" → "Finance Department"), and `IsPostable` (header vs. leaf) — cost center reporting genuinely
  needs roll-ups, the same reason the chart of accounts needs one, unlike Item's flat catalog. Same
  BusinessObject lifecycle, Security/SoD split, and Workflow wiring as the other three master-data slices.
- **Api**: `CostCentersController` at `api/v1/masterdata/cost-centers` — CRUD + submit/approve/reject, same
  pattern as `GLAccountsController`.
- **Frontend**: `CostCentersPage.tsx` — list/create/details, parent-cost-center display, bilingual name
  field. Got its own nav Area from the start this time (see the earlier nav-restructure entry in
  PROGRESS.md for why that matters).
- A `HomePage.tsx` workspace (tiles per Master Data area, live totals + pending-approval counts, click
  through to the list — the SAP Fiori Launchpad / Dynamics 365 Workspace pattern) now replaces the plain
  System Status page as the application's default landing screen; System Status stays reachable under
  Platform Administration for diagnostics. Purely additive frontend work, reusing the existing list
  endpoints — no new backend endpoint, no Domain/Application/Infrastructure changes.
- Nothing new broke building this either — followed the exact `GLAccount`/`Item` pattern, and this time the
  `{page === "cost-centers" ? <CostCentersPage /> : ...}` render branch was added correctly the first time
  (the Items slice's bug was fresh enough to check for deliberately).

## What's built (Phase 1, slice 5: Tax Codes — Phase 1 Master Data complete)

- **Domain**: `TaxCode` — the ZATCA VAT reference every future AP/AR document will carry (e.g. "VAT15" at
  15%, "ZERO" at 0%, "EXEMPT"), and the fifth and last Phase 1 Master Data piece. Unique `TaxCodeCode`,
  bilingual `TaxCodeName`/`TaxCodeNameArabic`, `Rate` (decimal, 0–100, validated in the constructor and on
  update — this is the one place a VAT percentage is allowed to live; nowhere in code is 15% ever written
  literally, per CLAUDE.md's "don't hard-code business rules... that should be configuration"), `TaxType`
  enum (Standard/ZeroRated/Exempt — the ZATCA taxonomy; ZeroRated and Exempt both charge 0% but are
  reported differently on a VAT return), `IsActive`. Deliberately flat like `Item`, not `GLAccount` — a tax
  code list doesn't need roll-ups. Same BusinessObject lifecycle, Security/SoD split, and Workflow wiring
  as every other master-data slice — a wrong VAT rate/type affects every document that references it
  afterward.
- **Api**: `TaxCodesController` at `api/v1/masterdata/tax-codes` — CRUD + submit/approve/reject, same
  pattern as the other four master-data controllers.
- **Frontend**: `TaxCodesPage.tsx` — list/create/details, bilingual name field, rate input, tax-type
  dropdown, its own nav Area.
- Nothing new broke — followed the exact `Item`/`CostCenter` pattern. One new wrinkle handled correctly the
  first time: `ArgumentOutOfRangeException` derives from `ArgumentException`, so `TaxCodesController`
  catches only `ArgumentException` (catching both would have been an unreachable-code compiler error, which
  the build caught immediately).
- **This closes out Phase 1 Master Data** — Business Partner, Chart of Accounts, Items, Cost Centers, and
  Tax Codes are all built. `Modules.Finance` (GL/AP/AR/Cash-Bank) is next.

## `Contracts` — the published surface other modules depend on

`src/Modules/Modules.MasterData/Contracts` is a thin, dependency-free project exposing read-only lookup
interfaces (`IGLAccountLookup`, `IBusinessPartnerLookup`, `ITaxCodeLookup`, `ICostCenterLookup`,
`IItemLookup`) + summary DTOs — the only thing another module (`Modules.Finance`, `Modules.Procurement`) may
depend on to reference this module's data, per docs/architecture/01-architecture-foundation.md §3.2 ("a
module may depend on another module's published Contracts package only, never its Domain/Infrastructure/
Application internals directly"). Implemented as thin EF adapters (`EfGLAccountLookup` etc.) in this
module's own Infrastructure project, projecting straight off `MasterDataDbContext` — no new tables, no
behavior change to any existing endpoint. First real consumer: `Modules.Finance`'s `JournalEntryService`,
which validates every journal line's G/L Account/Cost Center reference through these interfaces before ever
adding the line. `IItemLookup` (added 2026-07-14) is consumed by `Modules.Procurement`'s
`PurchaseRequisitionService` to validate an Item reference before adding a requisition line.

## Real bugs found and fixed while building this (disclosed, not hidden)

1. **`Platform.Core.BusinessObject` had no parameterless constructor.** Its only constructor always
   generated a fresh `Id` and set `Status = Draft` — correct for creating a new object, but EF Core
   rehydrating an *existing* row through that constructor would have corrupted every loaded record. Fixed
   by adding a parameterless constructor reserved for ORM materialization (EF Core sets every property,
   including get-only ones, via their backing field afterward).
2. **`LifecycleEngine` allowed `Submitted → Approve` directly (a shortcut for BOs with no configured
   workflow) but not the symmetric `Submitted → Reject`.** Found because `BusinessPartner.Reject()` failed
   for any partner that hadn't gone through a workflow's `InApproval` step. Fixed by adding the missing
   transition, with a Platform.Core.Tests case proving it.
3. **The number range counter table had no explicit column names**, so EF's default Npgsql convention
   produced PascalCase columns (`RangeKey`) while the raw SQL in `EfCoreNumberRangeService` assumed
   snake_case (`range_key`) — a silent mismatch that only an integration test against a real database
   caught (a pure unit test with a fake repository never would have).
4. **Optimistic concurrency wasn't actually enforced.** `BusinessObject.RowVersion` only increments inside
   `Transition()` (status changes) — a plain field edit like `UpdateContactDetails` never touches it, so
   two concurrent edits both saw the same row_version and neither was rejected. Fixed by using Postgres's
   native `xmin` system column (via `UseXminAsConcurrencyToken()`) as the actual EF Core concurrency
   token instead, since it tracks every write regardless of which property changed.

All four were caught by actually running tests against a real database, not by unit tests with fakes —
the intended lesson for this module going forward.

5. **`TestDatabase.ResetAsync()`'s `TRUNCATE TABLE masterdata.business_partners` broke** once
   `business_partner_addresses`/`business_partner_contacts` held a foreign key into it (`cannot truncate a
   table referenced in a foreign key constraint`) — fixed by adding `CASCADE`, which also clears the child
   tables in the same statement.
6. **The integration test suite was flaky under parallel execution** — `BusinessPartnerPersistenceTests`
   and `EfCoreNumberRangeServiceTests` both call `TestDatabase.ResetAsync()` (a real `TRUNCATE`) against
   the one shared `erp_platform_test` database, and xUnit runs different test *classes* in the same
   assembly in parallel by default. One class's reset could wipe rows another class's test had just
   inserted mid-run, surfacing as `System.InvalidOperationException: Sequence contains no elements` from a
   `FirstAsync`/`SingleAsync` that should have found its row. Fixed with
   `[assembly: CollectionBehavior(DisableTestParallelization = true)]` in `TestDatabase.cs` — the tests
   weren't flaky, the isolation assumption was wrong for a suite sharing one physical database instead of
   one container per test.
7. **A mutated `WorkflowInstance` almost wouldn't have persisted its own decision.** `Decide()` mutates its
   history/approved-by-step collections *in place* (the same list/dictionary instance, not a replacement)
   — EF Core's default reference-equality comparer for value-converted reference types can't detect that
   as a change, so a load → `Decide()` → `SaveChanges()` unit of work could silently write nothing back.
   Fixed by giving each jsonb-converted property an explicit `ValueComparer` that compares by serialized
   value, not reference — and specifically proved this with an integration test that decides an instance
   in one `DbContext`, then reloads through a completely fresh one to confirm the decision actually landed
   (the general "prove persistence across a fresh DbContext" pattern this module already uses, applied to
   the one scenario — mutation, not just insert — that could have hidden this bug from a less deliberate
   test).

Nothing new broke while wiring `Platform.Security` — no bug found in that pass beyond the compile-time
`Microsoft.AspNetCore.Http` missing `using` for `StatusCodes` in the new `ForbiddenError` helper
(`Platform.Api.PlatformApiController`), caught immediately by the build. Nothing new broke while building
`Platform.Attachments`/`Platform.Notes` either — both followed the exact `WorkflowInstance` persistence
pattern (flat `BusinessObjectType`/`BusinessObjectId`, private-constructor ORM materialization) already
proven correct, and both were verified against real PostgreSQL from the start.

## What's built (Lookup Data — admin-configurable picklist engine)

- **Domain**: `LookupType` (Code/Name/NameArabic/IsSystemDefined) and `LookupValue`
  (LookupTypeCode/Code/Name/NameArabic/IsActive/SortOrder) — deliberately NOT `BusinessObject`s (no Draft/
  Submit/Approve lifecycle): real SAP domain-value maintenance (SM30) and Dynamics 365's Option Set editor
  are both immediate-effect, gated by a single authorization role, not a two-person maintain/approve
  workflow — see `LookupType`'s own doc comment. Built in response to the explicit instruction not to
  hard-code lookup/classification data (CLAUDE.md, and the roadmap's own "customers/vendors — these words"
  example) — the words "Client"/"Supplier"/"HeadOffice"/etc. used to be C# enums (`BusinessRoleType`,
  `AddressType`) or unvalidated free text (`Country`, `UnitOfMeasure`); they are now admin-editable data.
- **`LookupService`**: one `MasterData.Lookup.Administer` privilege (not a Maintainer/Approver split — see
  above). Full CRUD on both levels: `CreateTypeAsync`/`UpdateTypeAsync`/`DeleteTypeAsync` (an administrator
  can define a brand-new picklist category from scratch, e.g. a future "Incoterms" list, the same way SAP's
  generic table maintenance isn't limited to pre-defined tables) and
  `CreateValueAsync`/`UpdateValueAsync`/`SetActiveAsync`/`DeleteValueAsync` for values within a type.
  `DeleteValueAsync` refuses to delete a value any real record still references
  (`ILookupRepository.IsValueInUseAsync` — a small per-type switch over the columns that actually consume
  it) — the caller is told to deactivate instead, same "correct by reversal, not deletion" principle as
  everywhere else in this platform, but only where deletion would actually corrupt existing data; an unused
  value deletes cleanly. `DeleteTypeAsync` refuses to delete a `IsSystemDefined` type (this platform's own
  code depends on the *category* existing) or a type that still has values.
- **Seven system-defined types seeded at every startup** (`LookupSeeder`, idempotent — only inserts what's
  missing, never touches an administrator's own edits): `Country` (~74 real countries, EN+AR, GCC/MENA-
  complete plus major global trading partners — an admin can add any missing one in seconds), `BusinessRoleType`
  (the 10 values that used to be the `BusinessRoleType` enum), `AddressType` (the 4 values that used to be
  the `AddressType` enum), `UnitOfMeasure` (15 construction-relevant units), and Trade split into **three
  role-scoped types, not one flat list** — `SubcontractorTrade` (15: Electrical/Concrete/Steel Structure/...),
  `SupplierTrade` (10: Steel/Cement/MEP Materials/...), `ConsultantTrade` (8: Structural/Architectural/MEP
  Design/...) — matching docs/architecture/06-roadmap.md's own Phase 2 design verbatim, which already
  described these as three separate real-world taxonomies (a Subcontractor's trades are not a Supplier's or a
  Consultant's). The first version of this checkpoint shipped one merged `Trade` type instead — corrected the
  same day after the user caught the mismatch against the roadmap's own already-written design.
- **Retrofitted onto real fields**: `BusinessRole.RoleType` and `BusinessPartnerAddress.AddressType` changed
  from C# enums to plain `string` Domain properties (the EF column was already `character varying` via
  `.HasConversion<string>()`, so this needed no data migration, just a model change) — validated at the
  `BusinessPartnerService` layer against the Lookup engine instead of `Enum.TryParse`.
  `BusinessPartnerService.AddAddressAsync`'s `Country` (previously free text with zero validation) and
  `ItemService`'s `UnitOfMeasure` (previously free text, disclosed as a Phase 1 gap) are now validated the
  same way. `BusinessRole.Trade` stays deliberately unenforced server-side (the roadmap's own explicit
  design — see the Deferred item below); the frontend's Trade field (`BusinessPartnersPage.tsx`) is a plain
  HTML `<input>` backed by an HTML `<datalist>` of role-scoped suggestions (switching between the
  `SubcontractorTrade`/`SupplierTrade`/`ConsultantTrade` list based on the currently-selected role) — real
  suggestions, still freely overridable text underneath.
- **Api**: `LookupsController` at `api/v1/masterdata/lookup-types` — type CRUD plus a nested
  `/lookup-types/{typeCode}/values` resource for value CRUD + activate/deactivate.
- **Frontend — a real Admin Panel, not a single page**: `LookupDataPage.tsx`, one reusable component given
  its own distinct nav entry per type (Countries/Business Role Types/Address Types/Units of
  Measure/Trades — each its own heading and URL, same "shared shape, separate nav items" precedent as
  GLAccounts/Items/CostCenters/TaxCodes) plus an "All Lookup Types" hub (value counts, System/Custom kind,
  a form to create an entirely new lookup type). Each type's page is a real inline-editable table-maintenance
  grid (SAP-style): every row shows Code/Name/Name (Arabic)/Sort Order/Status side by side with Edit/
  Deactivate-Activate/Delete buttons, plus an always-visible add-row at the bottom — Name and Name (Arabic)
  are always both present on screen together so a bilingual entry never has to guess at the other language.
  `BusinessPartnersPage.tsx`'s Business Role Type/Address Type/Country dropdowns and `ItemsPage.tsx`'s Unit
  of Measure dropdown all now fetch their options live from this engine instead of a hardcoded array/switch
  statement — an administrator's own addition through the admin panel is immediately selectable on the next
  page load, with no code change.
- Verified end-to-end: 12 new unit tests (`LookupServiceTests` — value/type CRUD, duplicate-code rejection,
  unknown-type rejection, authorization denial, deactivate-without-delete, in-use delete protection,
  system-defined-type delete protection, custom-type creation and deletion) + 3 new integration tests
  against real PostgreSQL (type+values round-trip identically including Arabic names, per-type code
  uniqueness enforced at the DB level, deactivation persists and is reversible). 138 unit + 26 integration
  tests pass in this module alone, zero regressions solution-wide. Live `curl` exercise: created/renamed/
  deactivated/reactivated/deleted a custom Country value; confirmed deleting an in-use `BusinessRoleType`
  value 409s and deleting the system-defined `Country` type itself 409s; confirmed creating a Business
  Partner with a bogus role, an address with a bogus country, and an Item with a bogus unit of measure all
  400 with a clear message, while real seeded values succeed. Live Playwright pass (screenshots, zero
  console errors) on the hub, the Countries grid (including a live inline add), and Business Role Types, in
  both English and Arabic — full RTL mirroring confirmed, including the page heading itself switching to
  the lookup type's own `NameArabic` (a real bug caught by this pass: the heading originally only ever read
  `type.name`, and a direct nav entry into one type's grid never populated the `types` list the heading
  needed at all — both fixed, re-verified live).

## Deferred (disclosed, not hidden)

- Phase 1 Master Data is now complete (Business Partner, Chart of Accounts, Items, Cost Centers, Tax
  Codes) — `Modules.Finance` (GL/AP/AR/Cash-Bank) is next.
- G/L Account and Cost Center both have no parent-hierarchy validation against cycles (`AssignParent`
  accepts any GUID for either); Item's `UnitOfMeasure` is now validated against the Lookup Data engine (see
  above) but still has no conversion factors between units (e.g. bags → tons) and no Item Group/category
  hierarchy — revisit once a real second-unit or roll-up-reporting need shows up, same "wait for a real
  need" reasoning as everywhere else in this module.
- Removing or editing an existing Address/Contact from the API/UI — only add exists today (`AddAddress`/
  `AddContact` on the Domain object and their matching endpoints); `RemoveAddress`/`RemoveContact` exist on
  `BusinessPartner` but aren't wired to the Application/Api/UI layers yet.
- `NameArabic` can only be set at creation (`CreateBusinessPartnerRequest.NameArabic`) — same precedent as
  `TaxRegistrationNumber`, there's no dedicated update endpoint yet. Nothing requires it to be non-null
  before a partner reaches Approved status, even though ZATCA effectively requires it before that partner
  can ever be invoiced — revisit once Finance/invoicing exists to make that requirement real instead of
  guessed at.
- `Platform.Audit`, `Platform.Workflow`, `Platform.Security`, `Platform.Attachments`, and `Platform.Notes`
  are now ALL wired into this module (see above) — the gap the user originally caught (kernel/platform
  services built and tested in isolation, never actually consumed by the first real business module, and
  Attachments/Notes never built at all) is fully closed for Business Partner. Row-level and field-level
  security (`Platform.Security.RowLevel`/`FieldLevel`) are NOT wired — no row is scoped by
  company/branch/project yet, and no field (there's nothing salary/IBAN-sensitive on Business Partner
  today) is masked. Revisit when a module has data that actually needs either.
- Editing or removing an existing attachment's metadata (renaming a file) isn't exposed — only
  add/list/download/delete exist. Real blob storage (not Postgres `bytea`) and virus scanning are
  `Platform.Attachments`'s own deferred items — see its README.
- Notes have no rich text, mentions, or attachments of their own (a note that needs a document should
  reference an actual `Platform.Attachments` upload instead) — see `Platform.Notes`'s own README.
- Segregation of Duties (`BusinessPartnerSecurity.MaintainerApproverConflict`) is registered and tested
  against the real `SodEngine`, but not checked anywhere in the live request path — `ISodEngine.FindConflicts`
  is meant to run when a role *assignment* is being made ("would giving this user both Duties create a
  conflict"), and there is no role-assignment admin surface anywhere in this application yet (only role
  *definitions* exist). The rule is real and will start mattering the moment role assignment exists;
  disclosing this rather than faking an assignment-time check with nothing real to check it against.
- Real authentication/company-context: `BusinessPartnersController` currently hardcodes two demo actors,
  `"system/ui"` (Maintainer) and `"system/approver"` (Approver), and `companyId = "C001"`, since no real
  SSO or company-selection UI exists yet — see `Program.cs`. `IActorRoleAssignmentStore`'s in-memory
  implementation is the stand-in this replaces; every audit entry for Business Partner is currently
  attributed to one of these two literals, not a real logged-in user — revisit once real auth exists.
- Docker/Testcontainers for integration tests — not available on this development machine, so
  `tests/IntegrationTests/Modules.MasterData.IntegrationTests` runs against a real, separate
  `erp_platform_test` database instead (connection string via `ERP_MASTERDATA_TEST_CONNECTION` env var,
  never committed). Revisit when Docker is available.
- A shared "List+Details form" Platform.UI template, a real npm package for Platform.UI, and a proper
  client-side router are all still deferred for the same reason as before: wait for a second real
  consumer before extracting/generalizing.
- Trade/Specialty (on `BusinessRole`) now has three real, admin-managed, role-scoped lookup types
  (`SubcontractorTrade`/`SupplierTrade`/`ConsultantTrade`, manageable through the Lookup Data admin panel,
  with the frontend already showing only the relevant list for the currently-selected role) — but per the
  roadmap's own explicit design it deliberately stays a *suggestion*, not a server-side-enforced value:
  `BusinessPartnerService` still accepts any string for `Trade` regardless of which list it came from or
  whether it came from the list at all. Revisit if a real need for hard enforcement shows up.
- The Lookup Data admin panel has no rename-type UI (`LookupService.UpdateTypeAsync` exists and is tested,
  but `LookupDataPage.tsx` only exposes create/delete for types, not edit) — a minor, mechanical gap, not a
  design gap. No bulk import/export (e.g. paste a CSV of 50 countries at once) — every value is added one
  row at a time through the inline grid. No cross-module `Contracts` publication of lookup values yet (only
  `Modules.MasterData.Application`/`Api` consume `ILookupRepository` directly) — add
  `Modules.MasterData.Contracts.ILookupCatalog` the same way `IBusinessPartnerLookup`/`IItemLookup` exist
  once a module outside MasterData actually needs to read a lookup type (e.g. Procurement wanting the same
  `Trade` list) — not built speculatively ahead of a real consumer, per this project's own stated
  engineering preference.
- Vendor Prequalification itself (`VendorPrequalification` or similar BO, role-specific review steps,
  configurable validity period) is not built yet — `BusinessRoles` was step one of Phase 2's design
  (docs/architecture/06-roadmap.md), deliberately built first since Prequalification needs it to exist.

## Phase 2: BusinessRoles replaces PartnerType

`BusinessPartner.PartnerType` (Customer/Vendor/Both, a single enum) has been replaced by
`BusinessRoles` — a multi-select child collection (`BusinessRole`: `RoleType` +
optional `Trade`) — per the design captured in `docs/architecture/06-roadmap.md` Phase 2 (2026-07-14)
alongside the full Vendor Prequalification design that motivated it. A real construction-industry partner
commonly holds several roles at once (a company can be both a Supplier and a Subcontractor), which a
single enum could never express. Ten role types: Client (replaces Customer — the construction-industry
label for what SAP calls Customer/Debtor, deliberately not modeled as a separate role), Supplier,
Subcontractor, Consultant, JointVenturePartner, GovernmentAuthority, RentalCompany, Manufacturer,
ManpowerSupplier, TestingLaboratory.

- **Government Authority is mutually exclusive with every other role** — "no commercial relationship, no
  AP/AR posting, no scorecard" per the roadmap — enforced in `BusinessPartner.AddBusinessRole`.
- **The same role can be held twice with different Trades** — e.g. Subcontractor–Electrical and
  Subcontractor–Concrete on the same company — because Vendor Prequalification (a future BO) qualifies a
  partner per Role+Trade combination, not once per Role.
- A data migration (`20260714183315_ReplacePartnerTypeWithBusinessRoles`) converted every existing
  `partner_type` value into an equivalent role rather than silently dropping it: Customer/Both → Client,
  Vendor/Both → Supplier (a "Both" partner correctly ends up holding two roles).
- `Modules.Finance.Application.APInvoiceService` was updated to check for a payable-eligible role
  (Supplier/Subcontractor/Consultant/RentalCompany/Manufacturer/ManpowerSupplier/TestingLaboratory) via
  `IBusinessPartnerLookup.BusinessRoles` instead of the old `PartnerType` string — the first real proof
  that a Contracts-package consumer survives a shape change on the publishing side without needing to
  know about internal renames beyond the DTO's own published shape.
