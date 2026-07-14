# Modules.Procurement

Phase 2's module: full procure-to-pay (PR → RFQ → PO → GRN → 3-way match against AP), gated by Vendor
Prequalification and a real budget-check integration with Modules.Finance. See
`docs/architecture/06-roadmap.md`'s Phase 2 section for the full design; this README tracks what's actually
built today.

## What's built (Phase 2, slice 1: Vendor Prequalification)

- **Domain**: `VendorPrequalification` — a vendor's qualification to act in a specific `RoleType`+`Trade`
  combination (e.g. Subcontractor–Electrical is a wholly separate certificate from Subcontractor–Concrete on
  the same company, matching `Modules.MasterData.Domain.BusinessPartner.BusinessRoles`' own "same role type
  twice with a different trade" design). `RoleType` is a plain string, not a shared enum reference — this
  module depends only on `Modules.MasterData.Contracts`, never MasterData's own Domain, per
  docs/architecture/01-architecture-foundation.md §3.2. Stops at Approved, like every Master Data-ish BO so
  far (GLAccount/Item/CostCenter/TaxCode) — a prequalification is a certification record, not a financial
  document, so there's no Post/Reverse. `SetValidityPeriod`/`ValidFrom`/`ValidUntil` are set exactly once,
  the moment the final review step approves, from a configured validity period (see below) — never
  re-derived later, so a future change to that configured period never retroactively shifts an
  already-approved certificate's expiry.
- **The first real multi-step workflow in this codebase**: `VendorPrequalificationWorkflow` registers a
  5-step, Any-quorum, unconditioned approval matrix (Commercial → Legal → Technical → HSE → Quality) — every
  prior workflow in this application (BusinessPartner, GLAccount, Item, CostCenter, TaxCode, JournalEntry,
  APInvoice) used exactly one step. Confirmed feasible without any new platform capability by reading
  `Platform.Workflow.WorkflowEngine.Start`/`WorkflowInstance` before building this: each step just needs its
  own `RequiredRoleKey`, and `WorkflowEngine.Decide` already resolves "which step is current" and checks the
  acting principal holds that step's specific role — the service layer only needs a shared, coarser
  `Review` privilege gate before attempting a decision at all.
- **`VendorPrequalificationSecurity`**: one Maintainer duty/role (create/submit) plus five distinct reviewer
  duties/roles, one per workflow step — Commercial/Legal/Technical/HSE/Quality are separate real-world
  departments, not one approver wearing five hats. Five SoD conflict rules (Maintainer vs. each reviewer
  duty), mirroring the "same person shouldn't both create and approve" rule every other module registers,
  just five times over.
- **Government Authority exclusion**: `VendorPrequalificationService.CreateAsync` rejects
  `RoleType == "GovernmentAuthority"` outright — the roadmap's explicit "not prequalified at all, there is no
  commercial relationship to qualify" design. Also validates the vendor exists, is itself Approved, and
  actually holds the requested `BusinessRole` (via `IBusinessPartnerLookup`) before allowing the
  prequalification to be created.
- **Configured validity period, not hardcoded**: `VendorPrequalificationService.ValidityMonthsConfigurationKey`
  (`"Procurement.VendorPrequalification.ValidityMonths"`, default 24 months) is a real
  `Platform.Configuration` key, overridable at Tenant/Company level — CLAUDE.md's "don't hard-code business
  rules... that should be configuration" applies here exactly as much as to a tax rate or approval
  threshold.
- **Attachments wired in**: this module owns its own copy of `Platform.Attachments`' persistence
  (`EfAttachmentRepository`/`AttachmentContentRow`, bound to `ProcurementDbContext`) for supporting documents
  (ISO certificates, CR copies, bank letters) — same "each module owns its own schema, so shares no tables
  with another module's Infrastructure" reasoning as its `EfCoreNumberRangeService`/
  `EfWorkflowInstanceRepository` copies.
- **Infrastructure**: `ProcurementDbContext` — this module's own **"procurement" Postgres schema**, in the
  same physical database as MasterData's/Finance's (schema-per-module is the boundary, not
  database-per-module). Owns its own `NumberRangeCounterEntity`/`EfCoreNumberRangeService`/
  `EfWorkflowInstanceRepository`/attachments plumbing, near-duplicates of Modules.Finance's own classes for
  the same "a module cannot depend on another module's Infrastructure directly" reason.
- **Api**: `VendorPrequalificationsController` at `api/v1/procurement/vendor-prequalifications` —
  Create/Get/List + `submit`/`approve`/`reject` (the same two endpoints service every one of the five review
  steps — `WorkflowEngine` resolves which step is actually current) + the standard
  attachments sub-resource (`POST/GET .../attachments`, `GET .../attachments/{id}/content`,
  `DELETE .../attachments/{id}`).
- **Frontend**: `VendorPrequalificationsPage.tsx` — list/create/details, vendor dropdown filtered to
  Approved partners, a role dropdown excluding Government Authority, a Trade input shown only for
  Trade-eligible roles (Supplier/Subcontractor/Consultant), an Attachments FastTab. Own nav module
  ("Procurement"), not bolted onto Finance or Master Data.
- Verified end-to-end: 23 new unit tests (create validation incl. Government Authority exclusion/unapproved
  vendor/role-mismatch, the full 5-step approval path, mid-workflow rejection, per-step role eligibility
  enforcement via a reviewer who only holds one of the five roles, attachment round-trip) + 2 new integration
  tests against real PostgreSQL. Live `curl` exercise: created a prequalification for an Approved Supplier,
  drove it through all 5 review steps to Approved with a computed `validFrom`/`validUntil` (24 months
  apart), confirmed Government Authority and unheld-role requests are rejected with clear messages, confirmed
  a mid-workflow rejection leaves no validity period set, and exercised the attachment upload/list/
  download/delete cycle. Live Playwright pass in both English and Arabic (full RTL mirroring including the
  nav tree and FastTabs).

## What's built (Phase 2, slice 2: Purchase Requisition)

- **Domain**: `PurchaseRequisition` — the first document in the procure-to-pay chain, a cost-center owner's
  request to buy specific Items in specific quantities at an estimated (not yet negotiated) price. Child
  collection `PurchaseRequisitionLine` (ItemId, CostCenterId, Quantity, EstimatedUnitPrice, optional
  LineDescription), same "0..n child collection, only exists through its parent, frozen once submitted"
  pattern as `Modules.Finance.Domain.JournalEntry.Lines`. `EstimatedTotal` is computed, never stored — a
  preview for the requester/approver, not a number the eventual RFQ/PO is bound by. Stops at Approved (no
  Post/Reverse — a requisition is an internal request, not a financial document).
- **New Contracts-package lookup**: `Modules.MasterData.Contracts.IItemLookup`/`ItemSummary` (+
  `EfItemLookup`) — the first cross-module lookup this module needed beyond what Vendor Prequalification
  already used (`IBusinessPartnerLookup`); `CreateAsync` validates every line's Item (must exist, be Active)
  and Cost Center (must exist, be Active, be Postable) via `IItemLookup`/`ICostCenterLookup` before the line
  is ever added, mirroring `JournalEntryService.ValidateLineReferencesAsync`'s exact pattern.
- **`PurchaseRequisitionSecurity`/`PurchaseRequisitionWorkflow`**: one Maintainer duty/role + one Any-quorum
  Approver step, the same single-step shape as every module's first-cut workflow (JournalEntry, APInvoice) —
  a real amount-conditioned approval matrix (the roadmap's own named example) is a natural later refinement
  once this module has more than one approval path to choose between.
- **Api**: `PurchaseRequisitionsController` at `api/v1/procurement/purchase-requisitions` — Create/Get/List +
  `submit`/`approve`/`reject`.
- **Frontend**: `PurchaseRequisitionsPage.tsx` — list/create/details, the second real multi-row line-item
  entry grid in this application (Item/Cost Center dropdowns per row, Quantity/Est. Unit Price/Line
  Description, an "Add Line" button, a live estimated-total preview), same shape as
  `JournalEntriesPage.tsx`'s grid. Own nav Area under Procurement, alongside Vendor Prequalification.
- Verified end-to-end: 20 new unit tests (line-addition validation, item/cost-center reference validation
  incl. inactive/non-postable rejection, the full Draft→Submit→Approve/Reject lifecycle, SoD/security denial
  cases) + 3 new integration tests against real PostgreSQL (requisition+lines round-trip, RowVersion
  increments across two real transitions, cascade delete). Live `curl` exercise: created a requisition for
  100 bags of cement against a real Item+Cost Center, drove it through Submit→Approve, confirmed a
  header/grouping cost center is correctly rejected. Live Playwright pass in both English and Arabic (full
  RTL mirroring including the line-item grid's column order).

## Deferred (disclosed, not hidden)

- RFQ, Purchase Order, GRN, 3-way match, and the Finance budget-check integration
  (`Modules.Finance.Contracts.IBudgetCheckService`) — the rest of Phase 2, genuinely next, not built yet.
- No amount-conditioned approval matrix for Purchase Requisition — one Any-quorum step for every
  requisition regardless of estimated total, same Phase-1-style simplification every module's first workflow
  cut uses.
- No per-step review criteria/checklist content (e.g. what the Technical reviewer is actually checking) —
  this slice proves the workflow mechanics; real review checklists are a configuration/content concern for
  later.
- `IBusinessPartnerLookup`'s `BusinessRoles` is role-type-only (no per-role Trade) — `CreateAsync` validates
  the vendor holds the requested `RoleType` but cannot cross-check the specific Trade against the vendor's
  own `BusinessRole.Trade` yet. Revisit once `BusinessPartnerSummary` carries trades too.
- No re-prequalification / renewal workflow — once a certificate's `ValidUntil` passes, nothing currently
  flags it or starts a new review; `IsValidAsOf` is available for a future PR/PO slice to check against, but
  no automated expiry notice exists yet.
- Real authentication: `VendorPrequalificationsController` hardcodes `"system/ui"`/`"system/approver"`, same
  deferred item as every other module's controller (the demo `"system/approver"` actor holds all five
  reviewer roles at once, which is why one demo user can drive all 5 steps in a row).
