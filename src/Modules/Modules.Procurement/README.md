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

## What's built (Phase 2, slice 4: Request for Quotation)

- **Domain**: `RequestForQuotation` — the second document in the procure-to-pay chain. References an Approved
  `PurchaseRequisition`; at creation, copies that PR's lines (Item + Quantity only, via child `RfqLine`,
  keeping `PurchaseRequisitionLineId` purely for traceability) and invites a fixed set of vendors (child
  `RfqInvitedVendor`) — both frozen once `Submit` sends the RFQ out. `RecordVendorQuote` records each invited
  vendor's unit price per line (child `RfqVendorQuoteLine`) only once Submitted, only for a vendor that was
  actually invited and a line that actually belongs to this RFQ, once per (vendor, line) pair. `Approve`
  means the quote-collection process is closed — there's no separate "award" step here; picking the winning
  quote is deferred to Purchase Order creation (task #102), which is stated to work "from RFQ-selected quote
  or direct."
- **Intra-module dependency, no new Contracts package**: `RequestForQuotationService` depends on
  `IPurchaseRequisitionRepository` directly (both live in `Modules.Procurement.Application`) to load and
  validate the source PR is Approved before copying its lines — same "same-module dependency doesn't need a
  Contracts package" reasoning as `Modules.Finance`'s `APInvoiceService` reusing `JournalEntryService`
  directly.
- **Vendor eligibility**: reuses the same commercial-relationship role set
  `Modules.Finance.Application.APInvoiceService.PayableEligibleRoles` uses — only a vendor holding a
  Supplier/Subcontractor/Consultant/RentalCompany/Manufacturer/ManpowerSupplier/TestingLaboratory role, and
  already Approved, can be invited to quote.
- **`RequestForQuotationSecurity`/`RequestForQuotationWorkflow`**: same one-step Any-quorum
  Maintainer/Approver shape as Purchase Requisition, a distinct Duty/Role pair from it (running an RFQ vs.
  approving one is different real-world authority than raising/approving a PR).
- **Api**: `RequestsForQuotationController` at `api/v1/procurement/requests-for-quotation` —
  Create/Get/List + `submit`/`approve`/`reject` + `POST {id}/vendor-quotes` to record one vendor's quote.
- **Frontend**: `RequestsForQuotationPage.tsx` — list/create/details. Create form has a PR dropdown (filtered
  to Approved requisitions), a vendor checkbox multi-select (filtered to Approved, quote-eligible vendors),
  description and response-deadline fields. Details view has four FastTabs: General, Lines (copied from the
  PR), Invited Vendors, and Vendor Quotes (a table of recorded quotes plus a mini quote-entry form, shown
  only once Submitted). Own nav Area under Procurement, alongside Vendor Prequalification and Purchase
  Requisition.
- Verified end-to-end: 21 new unit tests (line/vendor freeze-after-submit rules, quote-recording validation
  incl. uninvited-vendor/wrong-line/duplicate/non-positive-price rejection, PR-must-be-Approved validation,
  vendor eligibility validation, the full Draft→Submit→(quote)→Approve/Reject lifecycle) + 2 new integration
  tests against real PostgreSQL (RFQ with lines/invited vendors/quotes round-trips identically, cascade
  delete across all three child collections). Live `curl` exercise: created and approved a PR, created an
  RFQ against it (confirmed the line was copied with the correct Item/Quantity and the vendor was invited),
  submitted it, recorded a vendor quote, and approved — confirmed the quote persisted through to the
  Approved response.

## What's built (Phase 2, slice 5: Purchase Order)

- **Domain**: `PurchaseOrder` — the third document in the procure-to-pay chain, built "from an RFQ-selected
  quote or direct" per task #102's own wording. Child `PurchaseOrderLine` carries the real negotiated
  `UnitPrice` (not an estimate) plus a `CostCenterId` — unlike `RfqLine`, which drops Cost Center, a PO needs
  one back for the budget check below. `RequestForQuotationId`/`RfqLineId` are set only on the from-RFQ path,
  kept purely for traceability, same role `RfqLine.PurchaseRequisitionLineId` plays back to the PR. Stops at
  Approved — GRN (task #103, not built yet) is the actual receipt/financial event; a PO is a commitment, not
  a posting.
- **First real use of `Modules.Finance.Contracts.IBudgetCheckService`** — the exact synchronous cross-module
  contract call docs/architecture/01-architecture-foundation.md §3.2 names as its own worked example
  ("Procurement asks Finance's IBudgetCheckService before releasing a PO"). New `Modules.Finance.Contracts`
  project (mirrors `Modules.MasterData.Contracts`'s shape) publishes `IBudgetCheckService`/`BudgetCheckResult`;
  `PurchaseOrderService.SubmitAsync` calls it once per distinct cost center on the PO (summing that cost
  center's lines) before the PO is ever submitted for approval — a denied cost center blocks the whole
  submit. The real implementation, `Modules.Finance.Infrastructure.PassThroughBudgetCheckService`, always
  allows for now: Budget Control itself is still deferred Finance depth (PROGRESS.md), so there is no real
  budget data to check against yet — disclosed in that class's own doc comment rather than faking
  enforcement against numbers that don't exist. Swapping in a real check later only touches that one class's
  body, not the interface or any caller.
- **From-RFQ creation**: given an Approved RFQ and a vendor, builds the PO's lines from that RFQ's own lines
  at the price the vendor quoted — the vendor must be one of the RFQ's invited vendors and must have quoted
  *every* line (a partial award, splitting different vendors across lines, is a future PO-splitting concern,
  not built here — the RFQ's own "no award step" design, disclosed in its README section above, is why this
  had to be solved at PO-creation time). Each line's Cost Center is traced back through
  `RfqLine.PurchaseRequisitionLineId` to the source Purchase Requisition's own line — the field RFQ kept
  purely for traceability now earns its keep.
- **Direct creation**: no RFQ behind it — vendor and lines (Item/CostCenter/Quantity/UnitPrice) are supplied
  directly, validated through `Modules.MasterData.Contracts.IItemLookup`/`ICostCenterLookup` exactly like
  Purchase Requisition's own lines.
- **`PurchaseOrderSecurity`/`PurchaseOrderWorkflow`**: same one-step Any-quorum Maintainer/Approver shape as
  Purchase Requisition and RFQ, a distinct Duty/Role pair from both.
- **Api**: `PurchaseOrdersController` at `api/v1/procurement/purchase-orders` — Create/Get/List +
  `submit`/`approve`/`reject`.
- **Frontend**: `PurchaseOrdersPage.tsx` — list/create/details. Create form has a Source radio (From an
  RFQ-selected quote / Direct), then either an RFQ + vendor-eligible-for-that-RFQ dropdown pair, or a vendor
  dropdown plus the same Item/CostCenter/Quantity/UnitPrice line-entry grid Purchase Requisition uses. Details
  view has two FastTabs: General (vendor, source RFQ, total, status) and Lines. Own nav Area under
  Procurement, alongside Vendor Prequalification/Purchase Requisition/RFQ.
- Verified end-to-end: 18 new unit tests (direct-creation validation, from-RFQ line/price/cost-center
  copying, RFQ-not-Approved/vendor-not-invited/vendor-hasn't-quoted-every-line rejection, both-shapes-supplied
  rejection, budget-check-denies-blocks-submit, the full Draft→Submit→Approve/Reject lifecycle) + 3 new
  integration tests against real PostgreSQL (PO with lines round-trips identically including the RFQ
  traceability fields, a direct PO persists a null RFQ id, cascade delete). Live `curl` exercise: built the
  full PR→RFQ→PO chain (created and approved a PR, created and approved an RFQ against it with a vendor quote,
  created a PO from that RFQ — confirmed it picked up the *quoted* price 18.50, not the PR's *estimated*
  price 20, and correctly traced the cost center back through the PR), submitted (budget check ran and
  passed) and approved it, created a second PO directly with no RFQ, and confirmed the both-a-RFQ-and-lines
  request is rejected with a 400. Live Playwright pass in both English and Arabic on this page and (closing
  the prior session's deferred item) `RequestsForQuotationPage.tsx` — full RTL mirroring confirmed on both,
  zero browser console errors.

## What's built (Phase 2, slice 6: Goods Receipt Note + 3-Way Match — Phase 2 exit criteria complete)

- **Domain**: `GoodsReceiptNote` — the fourth document in the procure-to-pay chain, recording that goods
  against an Approved PO were physically received. Child `GrnLine` copies Item and `UnitPrice` from the
  referenced PO line (frozen at receipt time, same "freeze the financial fact" reasoning as `APInvoice.TaxRate`).
  Multiple GRNs can exist against one PO — construction deliveries are normally staged/partial —
  `GoodsReceiptNoteService.CreateAsync` sums every non-Rejected GRN's received quantity per PO line before
  admitting a new one, so cumulative receipts across any number of GRNs can never exceed what was ordered.
  Stops at Approved like every procurement document so far; it does **not** post a G/L entry itself
  (inventory/GR-IR clearing accounting is out of scope for this phase, disclosed below) — it exists purely so
  the 3-way match has a real "received" figure to compare against.
- **3-Way Match (`ThreeWayMatchService`)**: computed on demand, nothing persisted — same "computed, not
  stored" treatment as `PurchaseOrder.Total`. Compares **Ordered** (`PurchaseOrder.Total`), **Received** (sum
  of every *Approved* GRN's value against that PO), and **Invoiced** (an AP Invoice's `NetAmount`, looked up
  via a new `Modules.Finance.Contracts.IAPInvoiceLookup`) — flags a match only if the vendor agrees and the
  invoiced amount is within both the received value and the ordered total. Deliberately lives entirely on the
  Procurement side of the module boundary: Finance is upstream of Procurement in the dependency graph
  (docs/architecture/01-architecture-foundation.md §3.2), so Procurement reads its own PO/GRN data directly
  and reaches into Finance only through the one published, read-only `IAPInvoiceLookup` contract — the same
  direction `IBudgetCheckService` already established. The match is at the document-amount level, not
  line-by-line — `APInvoice` is header/amount-only by design (no lines, no PO reference), and reworking that
  shape into a full line-item document is a bigger change than this slice justifies; disclosed below rather
  than attempted.
- **`GoodsReceiptNoteSecurity`/`GoodsReceiptNoteWorkflow`**: same one-step Any-quorum Maintainer/Approver
  shape as every other document, a distinct Duty/Role pair (confirming a delivery is typically a site/
  warehouse authority, different from raising or approving the PO itself).
- **Api**: `GoodsReceiptNotesController` at `api/v1/procurement/goods-receipt-notes` — Create/Get/List +
  `submit`/`approve`/`reject`. The match check is `GET api/v1/procurement/purchase-orders/{id}/three-way-match
  ?apInvoiceId={id}` on `PurchaseOrdersController` (it's a query about a PO, not its own resource).
- **Frontend**: `GoodsReceiptNotesPage.tsx` — list/create/details, a PO dropdown (Approved POs only) that
  populates a per-line dropdown of that PO's own lines. `PurchaseOrdersPage.tsx` gained a "3-Way Match"
  FastTab: pick an AP Invoice, "Check Match," see Ordered/Received/Invoiced and a Matched/Variance result
  with human-readable variance notes.
- **This closes Phase 2's exit criteria** — "full procure-to-pay cycle with configurable approval matrix"
  (docs/architecture/06-roadmap.md): PR → RFQ → PO → GRN are all built, tested, and live-verified, and the
  3-way match against AP proves the loop closes.
- Verified end-to-end: 22 new unit tests (GRN line/quantity validation incl. cumulative-across-multiple-GRNs
  and rejected-GRNs-don't-count, the full Draft→Submit→Approve/Reject lifecycle; 3-way match's
  matched/vendor-mismatch/exceeds-received/exceeds-ordered/unapproved-GRNs-excluded cases) + 3 new
  integration tests against real PostgreSQL (GRN with lines round-trips identically, cascade delete,
  `ListByPurchaseOrderAsync` finds every GRN regardless of status). 105 unit + 13 integration tests pass in
  this module alone, zero regressions solution-wide (17 test projects). Live `curl` exercise: partially
  received a PO (15 of 50 units), submitted/approved the GRN, created an AP invoice within the received
  value and confirmed the match reports Matched, then created a second invoice exceeding the received value
  and confirmed the match reports Variance with the exact figures and a human-readable note; also confirmed
  a GRN attempting to over-receive beyond the PO's ordered quantity is rejected with a 400 (both in one GRN
  and cumulatively across two GRNs). One real bug caught only by this live exercise: the Gateway.Api wiring
  registered `GoodsReceiptNoteSecurity`/`GoodsReceiptNoteWorkflow`'s roles and actor-role assignments but the
  workflow *definition itself* was never added to the `IWorkflowDefinitionCatalog` registration — a GRN could
  be Submitted (no validation catches a missing definition, `WorkflowEngine.Start` just returns null and
  treats it as "no approval configured") but never Approved (`GetActiveAsync` correctly found no active
  instance). Fixed immediately, re-verified live and via the full test suite — the unit tests didn't catch
  this because `GoodsReceiptNoteServiceTests` builds its own self-contained workflow catalog per test, which
  correctly included the definition; only Gateway.Api's actual composition root had the gap. Live Playwright
  pass (screenshots, zero console errors) in both English and Arabic on `GoodsReceiptNotesPage.tsx` and
  `PurchaseOrdersPage.tsx`'s new 3-Way Match tab — full RTL mirroring confirmed.

## Deferred (disclosed, not hidden)

- Real Budget Control enforcement — `IBudgetCheckService` is wired and called at the right point (PO submit)
  but its only implementation always allows, since Budget Control itself doesn't exist in Finance yet.
- GRN doesn't post a G/L entry (inventory/GR-IR clearing accounting) — out of scope for this phase; the 3-way
  match uses GRN's `ReceivedValue` as a computed figure, not a posted balance.
- The 3-way match is document-amount-level, not line-by-line — `APInvoice` has no lines/PO reference to match
  against per line; a real line-by-line match needs that shape change, deferred as a bigger future rework.
- No "release" distinction between a matched invoice and actually posting/paying it — the match is advisory
  (an AP clerk checks it before posting), not an enforced gate on `APInvoiceService.PostAsync` itself.
- No PO amendment/change-order flow, no partial-award PO splitting across vendors for one RFQ.
- No "award" action on the RFQ itself (e.g. marking one vendor's quote as selected per line) — Approving an
  RFQ only closes the quote-collection process; picking which recorded quote to use now happens at Purchase
  Order creation time instead (see the Purchase Order section above), not on the RFQ.
- No duplicate-detection across RFQs (e.g. the same PR line being quoted on two different open RFQs) —
  genuinely useful, genuinely deferred, not required by this slice.
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
