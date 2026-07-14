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
  object), `IBusinessPartnerRepository` (the persistence port).
- **Infrastructure**: `MasterDataDbContext` (EF Core, Postgres, its own `masterdata` schema — physically
  enforcing the module-boundary rule at the database level), `EfBusinessPartnerRepository`,
  `EfCoreNumberRangeService` (a real, atomic `INSERT ... ON CONFLICT DO UPDATE ... RETURNING`
  implementation — not a naive read-then-write, which would let concurrent requests hand out duplicate
  document numbers). Addresses/Contacts are mapped as owned child tables
  (`business_partner_addresses`/`business_partner_contacts`) via their private backing fields, cascade
  deleted with the parent.
- **Api**: `BusinessPartnersController` (inherits `Platform.Api.PlatformApiController`) at
  `api/v1/masterdata/business-partners`, with `POST .../{id}/addresses` and `POST .../{id}/contacts` to
  append a child (there is no update/remove endpoint yet — see Deferred).
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

## Deferred (disclosed, not hidden)

- Chart of Accounts, Items, Cost Centers, Tax codes — the rest of Master Data (next slices of Phase 1).
- Removing or editing an existing Address/Contact from the API/UI — only add exists today (`AddAddress`/
  `AddContact` on the Domain object and their matching endpoints); `RemoveAddress`/`RemoveContact` exist on
  `BusinessPartner` but aren't wired to the Application/Api/UI layers yet.
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
