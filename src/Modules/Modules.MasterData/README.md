# Modules.MasterData

Business Partners (customers/vendors), Chart of Accounts, Items, Cost Centers, Tax codes, Number range
definitions (docs/architecture/01-architecture-foundation.md #3.1). **The first business module built**,
and the first real, persisted (PostgreSQL-backed) data in the application — everything before this was
platform-kernel infrastructure using in-memory storage, which was fine for kernel demo/status data but not
for real business records that must survive a restart.

## What's built (Phase 1, slice 1: Business Partner)

- **Domain**: `BusinessPartner` (extends `Platform.Core.BusinessObject`) — Name, PartnerType
  (Customer/Vendor/Both), tax registration number, contact/address fields. Uses the standard BO lifecycle
  (Draft → Submit → Approve) since new-partner onboarding is a real fraud/compliance control point
  (docs/architecture/03-platform-services.md #2.2's Segregation of Duties example); contact-detail edits
  are NOT gated by lifecycle status (a deliberate difference from transactional documents — correcting a
  vendor's phone number isn't a "reversal").
- **Application**: `BusinessPartnerService` (orchestration only — business rules live on the Domain
  object), `IBusinessPartnerRepository` (the persistence port).
- **Infrastructure**: `MasterDataDbContext` (EF Core, Postgres, its own `masterdata` schema — physically
  enforcing the module-boundary rule at the database level), `EfBusinessPartnerRepository`,
  `EfCoreNumberRangeService` (a real, atomic `INSERT ... ON CONFLICT DO UPDATE ... RETURNING`
  implementation — not a naive read-then-write, which would let concurrent requests hand out duplicate
  document numbers).
- **Api**: `BusinessPartnersController` (inherits `Platform.Api.PlatformApiController`) at
  `api/v1/masterdata/business-partners`.
- **Frontend**: `src/Apps/Apps.Shell/src/pages/BusinessPartnersPage.tsx` — the first real business screen
  (List + create/details), using `Platform.UI`'s `ActionPane`/`FastTabs`. Not using a shared "List+Details
  form template" yet — that's deferred until a second business object needs the same shape (see
  `Platform.UI/README.md`), so the common pattern gets extracted from real usage, not guessed at.

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

## Deferred (disclosed, not hidden)

- Chart of Accounts, Items, Cost Centers, Tax codes — the rest of Master Data (next slices of Phase 1).
- Real authentication/company-context: `BusinessPartnersController` currently hardcodes
  `actor = "system/ui"` and `companyId = "C001"` since no real SSO or company-selection UI exists yet —
  see `Program.cs`.
- Docker/Testcontainers for integration tests — not available on this development machine, so
  `tests/IntegrationTests/Modules.MasterData.IntegrationTests` runs against a real, separate
  `erp_platform_test` database instead (connection string via `ERP_MASTERDATA_TEST_CONNECTION` env var,
  never committed). Revisit when Docker is available.
- A shared "List+Details form" Platform.UI template, a real npm package for Platform.UI, and a proper
  client-side router are all still deferred for the same reason as before: wait for a second real
  consumer before extracting/generalizing.
