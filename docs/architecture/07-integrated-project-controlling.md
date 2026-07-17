# 07 — Integrated Project Controlling
### How every department connects around a construction project (SAP PS/MM/CO/AM/HCM-style)

**Audience:** architects, senior developers. **Read after:** 01-overview.md, 02-business-object-model.md,
03-module-boundaries.md. **Purpose:** this is the document that ties every department together. Individual
module docs (`docs/module/*.md`) describe what one module does in isolation; this document describes how
money and resources actually flow *between* them on a real project — which is the part a first-pass module
build almost always gets wrong, because each module looks fine on its own and the gaps only show up when
you try to answer "what did this project actually cost us this month."

---

## 1. The one idea everything below depends on: the WBS Element is the cost backbone

In SAP, **every cost and every revenue on a project passes through a WBS Element** — not through
Finance directly, not through Procurement directly. A Purchase Order doesn't "cost the company money" on
its own; it's *assigned to* a WBS Element, and that assignment is what makes it a project cost. This
project's `Modules.ProjectManagement` already owns the WBS Element for this exact reason (per your
README). Every section below is really answering one question: **"what touches a WBS Element, and how?"**

```
                              WBS Element (owns planned + actual cost/revenue)
                                          │
        ┌──────────────┬──────────────┬──┴───────────┬──────────────┬──────────────┐
        │              │              │               │              │              │
   Materials       Labor Cost    Equipment Cost   Subcontract    Overhead/     Revenue
   (Warehouse/         (Timesheet)   (Timesheet)      Cost         Indirect       (IPC —
    Procurement)                                                   Allocation    Construction module)
```

Nothing in this document introduces a new "master ledger." Every cost type below ends up as a **Cost
Line** against a WBS Element, and Finance's Universal Journal (already in your architecture, doc 07 of the
old set) is what actually books the money. This doc is about how each department *produces* those cost
lines correctly.

---

## 2. Materials & Warehouse — linking inventory to projects (SAP MM + PS integration)

### The gap in a naive design
A generic ERP treats "buy material" and "use material" as one event (PO → GRN → done). Construction
doesn't work that way: material is usually **received into a warehouse or site store first**, then
**issued to a specific WBS element** later, sometimes weeks apart, sometimes partially, sometimes returned
or transferred between sites.

### The real flow
```
Purchase Order (Procurement, already exists)
   → Goods Receipt (GRN, already exists) — material enters a WAREHOUSE, not a project yet
        → Goods Issue — material leaves the warehouse, consumed AGAINST a specific WBS Element
             → this is what actually creates the project's material cost line
   → Goods Return (site → warehouse) — unused material returned, reverses part of the cost
   → Stock Transfer (warehouse → warehouse, or site → site) — no cost impact, just relocation
```

### What's currently missing
- **`Warehouse`** as a master data entity (site stores are warehouses too — a project can have its own
  temporary site warehouse in addition to a central yard)
- **`StockItem`** / **`InventoryBalance`** — quantity on hand per material per warehouse, valued (moving
  average or FIFO — SAP defaults to moving average for most industries; pick one and document it, don't
  leave it implicit)
- **`GoodsIssue`** (Business Object, Draft→Posted, no Approve step needed for most companies — it's a
  execution-level document, not a commercial approval) — `WarehouseId`, `WbsElementId`, lines with
  `MaterialId`/`Quantity`/`ValuedCost` (pulled from the warehouse's current valuation, not re-entered)
- **`GoodsReturn`** — mirror of Goods Issue, reverses the WBS element's cost
- **Reservation** (optional but very common in practice) — a site engineer reserves material against a
  future WBS element need before it's actually issued, so procurement/warehouse planning can see upcoming
  demand — SAP calls this a Reservation; worth flagging as a "nice to have, not blocking" item

### Why this matters for costing accuracy
Without Goods Issue as its own step, a company either (a) costs material to the project the moment it's
purchased — wrong, because you might over-buy and the excess never gets used on *that* project — or (b)
never gets project-level material cost at all and just tracks total spend. Neither survives an audit.

---

## 3. Labor Cost & Timesheets (SAP CATS-equivalent)

### The core document: Timesheet
- `Timesheet` (Business Object): `EmployeeId`, `PeriodStart/End` (usually weekly), `Status` (Draft →
  Submitted → Approved by site supervisor → Posted to costing)
- `TimesheetLine`: `Date`, `WbsElementId` (which project/activity the hours were spent on — a single
  employee can split one day across multiple WBS elements), `HoursWorked`, `HoursType` (Regular/Overtime/
  Holiday/Sick — each usually has a different cost rate)

### How hours become cost
```
Timesheet (hours per WBS element)
   × Employee's Cost Rate (NOT their pay rate — see below)
   = Labor Cost Line against that WBS Element
```

**Critical distinction real systems get wrong:** the rate used to cost a project is often **not** the
same as what the employee is actually paid. Companies commonly use a **standard/burdened labor rate**
(base pay + statutory on-costs like GOSI + overhead loading) for project costing, separate from the actual
Payroll calculation. This is exactly why SAP keeps Cost Center Accounting's "activity rate" distinct from
HCM's payroll rate. **Data model implication:** `LaborCostRate` (per Employee or per Job Grade — job grade
is more scalable for a large workforce) is its own small master-data entity, referenced by the timesheet
costing engine, not derived from Payroll directly.

### Approval chain (why it's not a simple Draft→Approve)
Site supervisor approves *hours* (did this person actually work these hours on this project) — separate
concern from HR/Payroll approving *pay*. A `Timesheet` can be Approved for costing purposes and still feed
into a completely separate Payroll run that has its own approval.

---

## 4. Equipment Cost (SAP PM/PS integration — internal fleet)

### Two totally different cost mechanisms, don't conflate them
1. **Owned equipment** (company's own excavators, cranes, generators) — costed to a project via an
   **internal usage rate** (e.g. "$450/day" — this rate is meant to recover the equipment's depreciation +
   maintenance + fuel over its life, it is *not* the equipment's actual cost that day)
2. **Rented equipment** — this is just a normal Procurement/AP flow (a vendor invoice), no different from
   any subcontracted service, and should NOT be modeled as equipment costing at all — it's already covered
   by Procurement/AP.

### Data model (owned equipment only)
- `Equipment` (master data, links to Asset Management §6 below — an owned excavator IS a Fixed Asset)
- `EquipmentUsageLog`: `EquipmentId`, `Date`, `WbsElementId`, `HoursUsed` or `DaysUsed`, `InternalRate`
  (pulled from a rate table, not typed manually — consistency matters here for month-end reporting)
- Same Draft→Approved→Posted lifecycle as Timesheet; often literally recorded on the same site log by the
  same site engineer, so consider whether `Timesheet` and `EquipmentUsageLog` should share a common
  "Site Daily Log" parent document in your UI even if they're separate domain entities underneath —
  worth a UX decision, not just a data model one.

---

## 5. HR in Construction Companies — what's structurally different from a typical office HR module

Construction HR has three things a generic HR module usually doesn't:

### a) Mobilization / Demobilization cycles
Site labor is frequently hired *for a specific project* and demobilized when it ends — HR needs to track
**which project an employee is currently mobilized to**, not just "which department." This should be a
first-class relationship (`EmployeeProjectAssignment`: `EmployeeId`, `WbsElementId` or `ProjectId`,
`MobilizationDate`, `DemobilizationDate`), because it drives both the Timesheet's default WBS element and
site-access/safety-induction tracking.

### b) Labor accommodation & camp management
Common in GCC/large EPC projects — the company houses site labor. Not always needed, but worth flagging
as a deferred/optional module rather than silently missing: `LaborCamp`, `AccommodationAssignment`.

### c) Statutory/compliance layers specific to your jurisdiction
For Saudi specifically (since your Platform.Localization already targets KSA):
- **GOSI** (social insurance contributions) — already noted as deferred in your Localization work
- **WPS** (Wage Protection System — mandatory salary file submission to the bank/government) — already
  noted as deferred, belongs in Payroll
- **Iqama/work-permit expiry tracking** for expatriate labor — very commonly needed, not yet in scope
  anywhere in your current docs; worth deciding if it's Phase 4 HR or a later compliance checkpoint

**None of this is code work right now** — just flagging it so `docs/module/hr.md` and `docs/module/
payroll.md` don't get written as if this were a generic Western-office HR system.

---

## 6. Asset Management (SAP AM-equivalent) — equipment, vehicles, and site assets as Fixed Assets

### The core gap
Right now equipment/vehicles have no home anywhere in the module list. In SAP, these are **Fixed Assets**
with their own module (Asset Accounting), which sits under Finance but has its own document types:

- `FixedAsset` (master data): `AssetClass` (Equipment/Vehicle/Building/Furniture...), `AcquisitionCost`,
  `AcquisitionDate`, `DepreciationMethod` (Straight-line is the common default), `UsefulLifeMonths`,
  `CurrentBookValue` (computed, never manually entered)
- `DepreciationRun` — a periodic (usually monthly) batch process that posts depreciation expense to
  Finance for every active asset — this is a Finance-owned scheduled job, not a manual document
- `AssetTransfer` — moving an asset between cost centers/projects/locations
- `AssetDisposal` — sale or scrapping, closes the asset's book value

### Where it connects to Construction
`Equipment` (§4) should reference a `FixedAssetId` — the equipment's internal usage rate (§4) is
*informed by* but not identical to its depreciation (a rate table is usually set once a year based on
budgeted utilization, while depreciation runs monthly on a fixed schedule regardless of usage) — don't
try to derive one from the other automatically, they answer different questions.

---

## 7. How Finance actually connects to Projects (Results Analysis / WIP — the piece that makes project accounting real)

This is the part your architecture docs already flagged (doc 07 in the old set mentions "Results Analysis
+ Settlement to CO-PA") but it's worth spelling out plainly, since it's the single most misunderstood part
of project-based accounting for people coming from a generic ERP background.

### The problem it solves
A project runs for 18 months. Costs (materials, labor, subcontracts) happen continuously. Revenue (IPCs)
is billed periodically but rarely matches costs month-to-month. If you just book cost-when-incurred and
revenue-when-billed, your monthly P&L is meaningless — some months show huge losses (heavy spend, no IPC
yet), others show huge profits (a big IPC lands, little spend that month). Real companies need
**percentage-of-completion revenue recognition**.

### The mechanism (simplified but accurate)
```
1. Each month, calculate: % Complete = Actual Cost to Date / Total Budgeted Cost (cost-based method —
   most common; alternative is Physical % Complete from Site Progress §2 of the earlier construction spec)

2. Revenue Earned to Date = % Complete × Total Contract Value

3. Compare to Revenue Billed to Date (sum of certified IPCs)

4. If Revenue Earned > Revenue Billed  → "Unbilled Revenue" / WIP asset (you've earned it, haven't billed it yet)
   If Revenue Billed > Revenue Earned  → "Billed in Excess" / deferred revenue liability (you've billed
   ahead of work done — also very common with advance payments)

5. This Results Analysis run posts an adjusting journal entry monthly so the P&L reflects
   REAL progress, not billing timing
```

### Data model
- `ResultsAnalysisRun` (per WBS Element, per period): `TotalBudgetedCost`, `ActualCostToDate`,
  `PercentComplete`, `RevenueEarnedToDate`, `RevenueBilledToDate` (pulled from Construction's IPCs),
  `WipAdjustmentAmount`, `Status` (Calculated → Posted)
- This is a **Finance-owned, scheduled, cross-module read** — it reads WBS actual costs (from Materials/
  Labor/Equipment/Subcontracts, §2–4 above) and reads Construction's IPC data (from the earlier spec),
  and produces a Finance-only posting. Neither Construction nor ProjectManagement should try to calculate
  this themselves — it's Finance's job precisely because it's a *financial* judgment (which method,
  which budget baseline) applied to operational data owned elsewhere.

---

## 8. Procurement & Subcontractor Contracts — where the two currently-separate flows must agree

Your `Modules.Procurement` (materials/services from vendors) and `Modules.Construction`'s Subcontracts
(labor+material scope from subcontractors) are structurally similar but serve different purposes, and
real companies keep them **separate document types** for one important reason: a **Subcontract carries
retention, IPC billing, and often a performance bond** — a regular Purchase Order does not. Don't try to
unify them into one BO type even though the temptation exists (see §6c in `construction-commercial-
processes-spec.md` for the full Subcontract billing cycle). What *should* be shared:

- Vendor/Subcontractor master data (`BusinessPartner` — one entity, a subcontractor is just a
  BusinessPartner with a `Subcontractor` role flag, same as your existing PurchaseOrder vendor)
- Prequalification (if you have Vendor Prequal for Procurement already, per your PROGRESS.md, the same
  prequal process should gate who's eligible to be awarded a Subcontract — don't build a second
  prequalification flow)

---

## 9. Variation Orders — the full lifecycle across departments (recap + department owners)

Building on the earlier VO section, here's who owns each step:

| Step | Owning department | Document |
|---|---|---|
| Client instructs/requests change | Project Management (site) | `SiteInstruction` (optional, informal trigger) |
| Contractor prices the change | Construction (commercial/QS team) | `VariationOrder` (Draft) |
| Client approves | Construction | `VariationOrder` (Approved) — updates Contract BOQ + WBS planned cost/revenue |
| Subcontract scope affected | Construction | Back-to-back `VariationOrder` on the relevant Subcontract |
| Budget impact reviewed | Finance | Budget Control check against the WBS element's revised planned cost |
| Legal review (large VOs / disputes) | Legal (see §10) | Attached legal opinion / correspondence, no new document type needed unless disputed |

---

## 10. Legal Department — how it connects (without becoming its own transactional module prematurely)

Construction-specific legal involvement is real but mostly **advisory/document-attachment**, not a
transaction-heavy module like Finance or Procurement. Recommend NOT building a full "Legal module" in
early phases — instead:

- **Contract-level legal metadata**: `Contract`/`Subcontract` gets optional fields — `GoverningLaw`,
  `DisputeResolutionMechanism` (Arbitration/Litigation/DAB — Dispute Adjudication Board, common in FIDIC),
  `PerformanceBondRequired`/`PerformanceBondReference`
- **Dispute tracking**, only once you actually need it: a lightweight `Dispute` entity linked to a Claim
  (§6b of the construction spec) or a Contract — `Status` (Raised → Under Negotiation → DAB/Arbitration →
  Resolved), with document attachments (legal correspondence, DAB decisions) rather than trying to model
  legal proceedings as structured data
- **Labor law connection to HR**: this is really about HR/Payroll being *compliant by construction* rather
  than Legal being a transactional module — e.g. end-of-service gratuity calculation (mandatory under
  Saudi labor law) belongs in Payroll's calculation engine, not a separate Legal system; a Legal module's
  real job is tracking *disputes*, not encoding every labor law rule as its own module — those rules
  belong embedded in HR/Payroll where the calculations actually happen.

**Recommendation:** treat "Legal" as a thin cross-cutting layer (a few fields + a Dispute/Claim linkage),
not a 10th business module, unless your company's actual legal team tells you otherwise once the system is
in use.

---

## 11. Summary table — department ↔ WBS Element touchpoints

| Department | What it produces against a WBS Element | New entities needed (not yet built) |
|---|---|---|
| Materials/Warehouse | Material cost (Goods Issue) | `Warehouse`, `StockItem`, `GoodsIssue`, `GoodsReturn` |
| HR | Employee-project assignment | `EmployeeProjectAssignment` |
| Labor | Labor cost (Timesheet) | `Timesheet`, `LaborCostRate` |
| Equipment | Equipment cost (Usage Log) | `Equipment`, `EquipmentUsageLog` |
| Asset Mgmt | Depreciation (indirect, via equipment) | `FixedAsset`, `DepreciationRun` |
| Construction | Subcontract cost, IPC revenue, VOs, Claims | (already scoped in construction-commercial-processes-spec.md) |
| Finance | Results Analysis / WIP adjustment | `ResultsAnalysisRun` |
| Procurement | Material/service PO cost (already exists) | — |
| Legal | Dispute tracking, contract legal terms | `Dispute` (lightweight), fields on Contract/Subcontract |

This table is the master checklist for `ROADMAP.md` — every row here is either
already scoped (Construction) or a genuinely new phase-of-work that doesn't exist in your roadmap yet.
