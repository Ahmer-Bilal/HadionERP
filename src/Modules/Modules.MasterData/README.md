# Modules.MasterData

Business Partners (customers/vendors), Chart of Accounts, Items, Cost Centers, Tax codes, Number range
definitions (docs/architecture/01-architecture-foundation.md #3.1). **The first business module built**,
and the first real, persisted (PostgreSQL-backed) data in the application — everything before this was
platform-kernel infrastructure using in-memory storage, which was fine for kernel demo/status data but not
for real business records that must survive a restart.

## What's built (Phase 1, slice 1: Business Partner)

- **Domain**: `BusinessPartner` (extends `Platform.Core.BusinessObject`) — Name, PartnerType
  (Customer/Vendor/Both), tax registration number, and two child collections, `Addresses` and `Contacts`.
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
- **Api**: `BusinessPartnersController` (inherits `Platform.Api.PlatformApiController`) at
  `api/v1/masterdata/business-partners`, with `POST .../{id}/addresses` and `POST .../{id}/contacts` to
  append a child (there is no update/remove endpoint yet — see Deferred), and `POST .../{id}/reject`
  alongside the existing `.../submit`/`.../approve`.
- **Frontend**: `src/Apps/Apps.Shell/src/pages/BusinessPartnersPage.tsx` — the first real business screen
  (List + create/details), using `Platform.UI`'s `ActionPane`/`FastTabs`. The details view has separate
  Addresses/Contacts FastTabs, each showing the existing rows plus an inline add form. Not using a shared
  "List+Details form template" yet — that's deferred until a second business object needs the same shape
  (see `Platform.UI/README.md`), so the common pattern gets extracted from real usage, not guessed at.

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
(`Platform.Api.PlatformApiController`), caught immediately by the build.

## Deferred (disclosed, not hidden)

- Chart of Accounts, Items, Cost Centers, Tax codes — the rest of Master Data (next slices of Phase 1).
- Removing or editing an existing Address/Contact from the API/UI — only add exists today (`AddAddress`/
  `AddContact` on the Domain object and their matching endpoints); `RemoveAddress`/`RemoveContact` exist on
  `BusinessPartner` but aren't wired to the Application/Api/UI layers yet.
- `Platform.Audit`, `Platform.Workflow`, and `Platform.Security` are now all wired into this module (see
  above) — the gap the user originally caught (kernel services built and tested in isolation, never
  actually consumed by the first real business module) is closed for Business Partner. Row-level and
  field-level security (`Platform.Security.RowLevel`/`FieldLevel`) are NOT wired — no row is scoped by
  company/branch/project yet, and no field (there's nothing salary/IBAN-sensitive on Business Partner
  today) is masked. Revisit when a module has data that actually needs either.
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
- **`PartnerType` (Customer/Vendor/Both) is planned to become a multi-select `BusinessRoles` collection**
  (Client/Supplier/Subcontractor/Consultant/Joint Venture Partner/Government Authority/Rental
  Company/Manufacturer/Manpower Supplier/Testing Laboratory, each with its own configurable Trade/Specialty
  sub-classification) — a real construction-industry partner commonly holds several roles at once. Design
  captured in `docs/architecture/06-roadmap.md` Phase 2 (2026-07-14) alongside the full Vendor
  Prequalification design that motivated it; deliberately documented now and built once Phase 2
  (`Modules.Procurement`) actually starts, rather than reworking Business Partner's shape twice.
