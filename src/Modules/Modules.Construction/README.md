# Modules.Construction

The construction-industry commercial layer built on top of `Modules.ProjectManagement`'s WBS elements:
Customer Contracts, BOQ (mapped onto WBS elements), Subcontracts (procurement documents assigned to WBS
elements), Site Progress/Measurement, Variation Orders (adjust a WBS element's planned cost/revenue, which
feeds the next Results Analysis run in Finance), and Retention terms.

This module depends on ProjectManagement (consumes its `Modules.ProjectManagement.Contracts.IProjectLookup`)
and does not define its own project-cost structure. See
`docs/architecture/07-project-accounting-and-financial-architecture.md` §4.

## What's built (Phase 3 slice: Customer Contract + BOQ)

- **Domain**: `Contract : BusinessObject` — `ProjectId` (validated Approved via `IProjectLookup` at Create),
  `ContractType` (lookup-validated against a new `ContractType` Lookup type — LumpSum/UnitPrice/CostPlus,
  EN+AR — same `ILookupCatalog` pattern `PaymentService` uses for `PaymentMethod`), `PaymentTerms` (free
  text — see Deferred below), `AdvancePaymentPercentage?`, `DefectsLiabilityPeriodMonths?`. Owns a child
  `BoqLine` collection (`Code`/`Description`/`DescriptionArabic?`/`UnitOfMeasure`/`Quantity`/`Rate`/
  `WbsElementId`, `Amount` computed as `Quantity * Rate`). `Contract.ContractValue` is computed as the sum
  of BOQ line amounts, never entered by hand (mirrors `Modules.Procurement.Domain.PurchaseOrder.Total`).
  Stops at Approved like every commercial/organizational BO so far — no Post/Reverse; a Contract is not
  itself a journal-posting document (IPC/AR billing against it is a later slice). Not enforced as
  one-per-project: real contracts get amendments, and a hard 1:1 relationship would need reworking the
  first time a genuine amendment/addendum contract shows up.
- **`Modules.ProjectManagement.Contracts.IProjectLookup`** (new, built as a prerequisite for this slice —
  the module's own README had referenced it before it existed): `ProjectSummary(Id, DocumentNumber,
  ProjectName, ProjectNameArabic, CustomerId, Status, WbsElements)` where each `WbsElementSummary` carries
  the three Controlling-object flags. One call gives Construction everything it needs (project status + the
  full WBS list) to validate a Contract's `ProjectId` and each BOQ line's `WbsElementId` — implemented as
  `EfProjectLookup` in `Modules.ProjectManagement.Infrastructure`.
- **`ContractService.CreateAsync`**: one-shot creation (header + all BOQ lines in a single request, no
  tempId/parent-resolution scheme needed since BOQ lines reference *existing* WBS elements by real `Guid`,
  unlike `Project`'s WBS hierarchy). Validates: the Project exists and is Approved; each BOQ line's
  `WbsElementId` belongs to that Project's own WBS elements (rejects a WBS element from a different
  project); `ContractType` and each line's `UnitOfMeasure` are known, active Lookup values. Not restricting
  BOQ lines to a specific WBS flag (billing vs. account-assignment) yet — deferred to Site Progress/
  Measurement, the next slice, where that distinction becomes load-bearing.
- **`ContractSecurity`/`ContractWorkflow`**: same one-step Any-quorum Maintainer/Approver shape as every
  other module's first-cut BO (`Construction.Contract.Maintain`/`.Approve` privilege keys, one SoD conflict
  rule).
- **Infrastructure**: `ConstructionDbContext` — this module's own **"construction" Postgres schema**, same
  physical database as every other module. Owns its own `NumberRangeCounterEntity`/
  `EfCoreNumberRangeService`/`EfWorkflowInstanceRepository`, same "a module cannot depend on another
  module's Infrastructure directly" reasoning as every other module. Number range key `CON-CONTRACT`,
  format `CON-CONTR-2026-000001`.
- **Api**: `ContractsController` at `api/v1/construction/contracts` — Create/Get/List +
  `submit`/`approve`/`reject`.
- **Frontend**: `ContractsPage.tsx` — list/create/details using the established `SplitView` pattern. Create
  form: header fields (Project select, Contract Type select sourced from the Lookup engine, Payment Terms,
  Advance Payment %, Defects Liability Period) plus a BOQ add-row grid whose WBS-element dropdown populates
  from the selected Project's own WBS elements (fetched via the existing
  `GET /api/v1/projectmanagement/projects/{id}` list already loaded client-side). Details view has General
  and BOQ Lines FastTabs. New "Construction" nav module with a "Contracts" area, alongside "Project
  Management."
- Verified end-to-end: 20 new unit tests (`ContractTests` domain-level — lifecycle, validation, computed
  Amount/ContractValue; `ContractServiceTests` — WBS-must-belong-to-project rejection, Project-must-be-
  Approved rejection, unknown ContractType/UnitOfMeasure rejection, Draft-only line add, full lifecycle,
  SoD/security denial) + 3 new integration tests against real PostgreSQL (`ContractPersistenceTests` —
  round-trip with child BOQ lines including Arabic description, cascade delete, RowVersion increments
  across real transitions). 23/23 test projects pass solution-wide, zero regressions. Live `curl` cycle:
  created and approved a Project with two WBS elements, confirmed a BOQ line referencing a WBS element from
  a *different* project is rejected with 400, created a real Contract with two BOQ lines (confirmed
  `ContractValue` computed correctly: 100×50 + 40×30 = 6,200), drove it through Submit→Approve. Live
  Playwright pass (screenshots, zero console errors) on `ContractsPage.tsx`'s list, create form, and detail
  view (General + BOQ Lines FastTabs), in both English and Arabic — full RTL mirroring confirmed including
  the nav module/area and the FastTabs.

## What's built (Phase 3 slice: Subcontracts)

- **Domain**: `Subcontract : BusinessObject` — `ProjectId` (validated Approved via `IProjectLookup` at
  Create, same pattern as `Contract`), an optional `ContractId` for back-to-back traceability to the
  Customer Contract (validated to belong to the same Project and be Approved, when supplied — never
  required, since a subcontract can exist for scope not itemized in any single main contract),
  `SubcontractorId` (a Business Partner validated Approved and holding the **`Subcontractor`** role
  specifically via `Modules.MasterData.Contracts.IBusinessPartnerLookup` — narrower than
  `PurchaseOrderService.VendorEligibleRoles`, since a Subcontract is semantically for an actual
  subcontractor, not any vendor-family role), `RetentionPercentage?`/`MobilizationAdvancePercentage?`
  (0-100) and `DefectsLiabilityPeriodMonths?` (>=0) as commercial terms. Owns a child `SubcontractLine`
  collection (same shape as `BoqLine` — Code/Description/DescriptionArabic?/UnitOfMeasure/Quantity/Rate/
  WbsElementId/computed Amount) and a child `BackCharge` collection (Description/Amount/DateIncurred) —
  back charges are **only addable once the Subcontract is Approved** (a live-execution event, not a
  Draft-time line item), enforced in the domain via `Subcontract.AddBackCharge`. `SubcontractValue`
  (sum of line amounts), `TotalBackCharges` (sum of back charges), and `NetPayableValue`
  (`SubcontractValue - TotalBackCharges`) are all computed, never entered by hand. Stops at Approved like
  `Contract` — no Post/Reverse, no budget check at Submit (same precedent as `Contract`: neither document
  consumes budget or posts to Finance until Payment/IPC exists against it).
- **`SubcontractService`**: takes `IContractRepository` directly (same module, no cross-module lookup
  needed) for the optional `ContractId` validation. `CreateAsync` validates: Project Approved; each line's
  WBS element belongs to that Project; each line's UnitOfMeasure is a known active Lookup value; the
  Subcontractor exists, is Approved, and holds the Subcontractor role; if `ContractId` is supplied, that
  Contract belongs to the same Project and is Approved. `AddBackChargeAsync` is Maintainer-authorized,
  rejects if the Subcontract isn't Approved, audited via `IAuditRecorder.RecordFieldUpdate` (same pattern
  `BusinessPartnerService` uses for adding an Address/Contact post-creation).
- **`SubcontractSecurity`/`SubcontractWorkflow`**: same one-step Any-quorum Maintainer/Approver shape as
  every other module's first-cut BO (`Construction.Subcontract.Maintain`/`.Approve` privilege keys, one
  SoD conflict rule).
- **Infrastructure**: `EfSubcontractRepository`, three new tables in the `construction` schema
  (`subcontracts`/`subcontract_lines`/`back_charges`, both children cascade-deleted with their parent).
  Number range key `CON-SUBCONTRACT`, format `CON-SUBCON-2026-000001`.
- **Api**: `SubcontractsController` at `api/v1/construction/subcontracts` — Create/Get/List +
  `submit`/`approve`/`reject` + `POST {id}/back-charges`.
- **Frontend**: `SubcontractsPage.tsx` — list/create/details using the established `SplitView` pattern.
  Create form: Project select, optional Contract select (populated from that Project's own Approved
  Contracts), Subcontractor select (Business Partners filtered to Approved + Subcontractor role, same
  filter idiom as `PurchaseOrdersPage.tsx`'s vendor-eligible-role filter), Retention %/Mobilization
  Advance %/Defects Liability Period inputs, a line add-row grid identical to `ContractsPage.tsx`'s BOQ
  grid. Detail view: General/Lines/Back Charges FastTabs — the Back Charges tab's add-row form is only
  enabled once the Subcontract is Approved, and shows `SubcontractValue`/`TotalBackCharges`/
  `NetPayableValue`. New "Subcontracts" nav area alongside "Contracts" under the "Construction" module.
- Verified end-to-end: 30 new unit tests (`SubcontractTests` domain-level — retention/mobilization %
  range validation, defects liability >= 0, `AddLine`/`AddBackCharge` lifecycle gating and computed-value
  correctness; `SubcontractServiceTests` — WBS-must-belong-to-project, Project-must-be-Approved,
  Subcontractor-must-be-Approved/-hold-the-role, optional-Contract-must-be-same-project/-Approved, unknown
  UnitOfMeasure, back-charge rejected before Approved / accepted after, SoD/security denial) + 3 new
  integration tests against real PostgreSQL (`SubcontractPersistenceTests` — round-trip with lines and
  back charges, cascade delete of both child collections, RowVersion increments across real transitions).
  23 test projects pass solution-wide, zero regressions. Live `curl` cycle: created and approved a Project
  with WBS elements and an Approved Subcontractor Business Partner, confirmed a line referencing an
  unknown WBS element and a non-Subcontractor vendor are both correctly rejected with 400, created a real
  Subcontract with two lines (confirmed `SubcontractValue` computed correctly: 100×50 + 20×200 = 9,000),
  confirmed a back-charge is rejected with 409 before Approval, drove Submit→Approve, then added a
  back-charge and confirmed `NetPayableValue` dropped from 9,000 to 8,500. Live Playwright pass
  (screenshots, zero console errors) on `SubcontractsPage.tsx`'s list, a full UI-driven create→submit→
  approve→add-back-charge cycle, and the detail view's three FastTabs, in both English and Arabic — full
  RTL mirroring confirmed including the new "Subcontracts" nav area.
- **Known operational gap surfaced during verification, same as the AP Payment Recording and Contract+BOQ
  sessions before this one**: the bootstrap `admin` user's roles aren't retroactively synced when new
  roles get registered (`Construction.Subcontract.Maintainer`/`Construction.ApproveSubcontract` here) —
  worked around manually via `POST /api/v1/identity/users/{id}/roles` with an SoD override reason (the
  bootstrap admin already holds every other module's Maintainer+Approver pair, so granting a fourth
  triggers the same-actor conflict rule by design). Not fixed here, same disclosed gap as before.

## Deferred (disclosed, not hidden)

- **Site Progress/Measurement, Variation Orders, Retention (as a real withholding mechanic), IPC (Interim
  Payment Certificate)** — the roadmap's other named pieces of this module. Site Progress/Variation Orders
  reference a BOQ line or WBS element the way Subcontracts do; IPC billing needs Finance's still-open AR
  gap closed first. Not started.
- **Retention/Mobilization Advance are commercial terms only, not yet mechanically enforced**:
  `Subcontract.RetentionPercentage`/`MobilizationAdvancePercentage` are stored and shown but nothing
  actually withholds a percentage from a Payment yet — `Modules.Finance.Payment` has no concept of a
  Subcontract or retention at all. Same "stored, not yet wired" state as `Contract.AdvancePaymentPercentage`
  before it. Real retention withholding is described in
  `docs/architecture/07-project-accounting-and-financial-architecture.md` §4 as "a Finance AR/AP
  sub-process against the same WBS/contract billing element" — needs AR/IPC to exist first.
- **Back charges don't reduce an actual Payment amount yet** — `NetPayableValue` is purely informational on
  the Subcontract document itself; there's no linkage from a `BackCharge` to any AP Payment allocation.
- **No per-line traceability back to a specific parent `Contract.BoqLine`** — a real back-to-back
  subcontract often scopes a specific BOQ item, but `SubcontractLine` only optionally traces back to the
  Contract as a whole (via the header's `ContractId`), not to an individual BOQ line. Deferred, same
  judgment call as `BoqLine`'s own "no WBS flag enforcement yet."
- **Accounts Receivable / IPC billing** — this Contract has no way to actually bill a customer yet; that's
  `Modules.Finance`'s still-open AR gap (`HadionERP_Missing_Features_Audit_V1.1.md` §3), elevated into this
  same roadmap phase per the 2026-07-16 sequencing decision (see `docs/architecture/06-roadmap.md`).
- **No BOQ flag enforcement**: a BOQ line can be mapped onto *any* of the Project's WBS elements regardless
  of `IsBillingElement`/`IsAccountAssignmentElement` — real-world BOQs typically map onto leaf,
  account-assignment nodes specifically. Deferred to Site Progress/Measurement, where the distinction
  actually needs enforcing (nothing posts against a BOQ line yet, so it's not load-bearing today).
- **No Contract-per-Project uniqueness**: multiple Contracts can be created against the same Project (real
  contracts get amendments/addenda) — deliberate, not an oversight, but also not yet a modeled "amendment
  of" relationship between two Contracts.
- **`PaymentTerms` is free text**, not the real Payment-Terms field on the Business Partner master
  (`ARCHITECTURE-AUDIT.md` §15, still open) — a Contract can't yet default its terms from the vendor/
  customer master because that master-data field doesn't exist yet.
- **Minor known RTL cosmetic issue**: the free-text `PaymentTerms` value (e.g. "30 days net") renders
  bidi-reversed in the Arabic UI since it isn't wrapped in `<bdi dir="ltr">` like the numeric/code fields
  elsewhere on the page — cosmetic only, not a data-integrity issue (the stored value is correct), worth
  fixing in the next UI pass rather than blocking this slice on it.
- Real authentication: `ContractsController` resolves the real logged-in user via
  `PlatformApiController.CurrentActor`, same as every other module's controller — no hardcoded actor here.
