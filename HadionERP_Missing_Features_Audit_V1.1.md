# HadionERP — What's Missing: Core Data, Platform Capabilities & Construction-ERP Gaps

**Date**: 2026-07-16
**Scope**: Consolidated gap analysis vs. SAP S/4HANA, Microsoft Dynamics 365 F&O, and construction-specific
ERP suites (SAP RE-FX/PS-CO, Viewpoint Vista, CMiC, Procore + Sage/QuickBooks-style job costing, Oracle
Aconex/Primavera). Built on top of the repo's own `ARCHITECTURE-AUDIT.md` (2026-07-15), verified against
current source, and extended with construction-industry-specific findings not covered there.

**Note on the existing audit**: `ARCHITECTURE-AUDIT.md` is thorough and still mostly accurate — this document
does not repeat its full evidence trail, only its conclusions, corrected where the codebase has since moved
(one item, §16 below, was silently closed since the audit was written but the audit file itself was never
marked resolved).

---

## 0. Correction to the existing audit

`ARCHITECTURE-AUDIT.md` Part 2 §16 ("AP Payment Recording & Cash/Bank Management") is listed as **Blocking /
open** and called "the single biggest finding in this audit." This is now **stale**: `Modules.Finance.Domain`
already contains `Payment.cs`, `BankAccount.cs`, and `PaymentAllocation.cs`, wired through `PaymentService`
to actually post Debit-Payable/Credit-Bank journal entries — the Payment class's own doc comment states it
"closes `ARCHITECTURE-AUDIT.md` Part 2 §16." **Recommendation**: add a `✅ RESOLVED` note to that section the
same way §1 and §3 already have, so the audit file stays trustworthy as a living reference. Treat §16 as
closed in all planning going forward.

---

## 1. Snapshot — what actually exists today

| Module | State |
|---|---|
| `Modules.Identity` | Real (JWT auth, users, roles, SoD enforcement on assignment) |
| `Modules.MasterData` | Real (Business Partner, GL Account, Item, Cost Center, Tax Code, Lookup engine) |
| `Modules.Finance` | Real (GL Journal Entry, AP Invoice, Payment, Bank Account) — no AR side yet |
| `Modules.Procurement` | Real (Vendor Prequal → PR → RFQ → PO → GRN, 3-way match) |
| `Modules.ProjectManagement` | Partial (Project + WBS Element only — no scheduling, no resource/equipment allocation, no cost roll-up into Finance yet) |
| `Modules.Construction` | **Empty — README only.** No Contracts, BOQ, Subcontracts, Site Progress, Variation Orders, or Retention code exists |
| `Modules.HR` | **Empty — README only** |
| `Modules.Payroll` | **Empty — README only** |
| `Modules.Reporting` / `Platform.Reporting` | **Empty — README only** |
| `Platform.Integration` (ZATCA/GOSI/WPS/SADAD/e-sign adapters) | **Empty — README only** |
| `Platform.Extensibility` | **Empty — README only** |

Everything below explains what real SAP/Dynamics/construction-ERP deployments carry that these gaps
represent, so each empty module is scoped against reality rather than its one-line README.

---

## 2. Platform-level gaps (carried over from `ARCHITECTURE-AUDIT.md` Part 1, status as of today)

| # | Gap | Status | Why it matters |
|---|---|---|---|
| 1 | Authentication & Identity | ✅ Resolved | — |
| 2 | Multi-Company / Legal Entity structure | Missing | No `Company` entity; `"C001"` hardcoded ~15 places. Needed the moment a second legal entity, JV, or subsidiary exists |
| 3 | Segregation of Duties enforcement | ✅ Resolved | — |
| 4 | Row-Level & Field-Level Security | Structural (dead code) | Nothing scopes a record to a company/branch/project yet; no field is masked (salary, IBAN, national ID) |
| 5 | Amount-conditioned approval matrices | Depth gap | Every workflow is a fixed role-chain regardless of document value — a 5,000 SAR PO and a 5,000,000 SAR PO take the same one-step path |
| 6 | Delegation & Escalation | Structural / Missing | Delegation is wired but nothing ever registers one; Escalation code is fully orphaned |
| 7 | Notifications & Output Management | Missing | Zero email/SMTP, zero PDF/print-output generation anywhere — can't email a PO or print an approval notice |
| 8 | ZATCA e-invoicing | Structural (unwired) | QR/XML builder code exists but is never called from `APInvoice` |
| 9 | Hijri calendar | Structural (unwired) | Service exists; every date field in the UI is plain Gregorian `<input type="date">` |
| 10 | Multi-currency | Missing | No `Currency`/exchange-rate concept anywhere |
| 11 | Fiscal Year / Period management | Missing | No period-open/close, no year-end concept — a Journal Entry can post to any date forever |
| 12 | Reporting/Analytics/Dashboards | Missing (correctly scoped for Phase 5) | No dashboard, no BI layer, no saved report anywhere yet |
| 13 | Extensibility & integration adapters | Missing (correctly scoped for Phase 6) | Plugin/extension runtime not started |
| 14 | Attachment storage & virus scan | Depth | Attachments work but store content in-DB with no object storage or malware scanning |

---

## 3. Core data-model gaps (carried over from Part 2, status as of today)

| # | Gap | Status |
|---|---|---|
| 15 | Business Partner missing fields: **Payment Terms, default Payment Method, Bank/IBAN, Credit Limit, Reconciliation Account, Withholding Tax flag, "one-time vendor" flag** | Still open — `BusinessPartner` only has Name/NameArabic/TaxRegistrationNumber + Addresses/Contacts/BusinessRoles |
| 16 | AP Payment Recording & Cash/Bank Management | ✅ **Resolved** (see §0 above — this document corrects the stale status) |
| 17 | GL Document Type concept | Missing, low urgency |
| 18 | Profit Center (separate from Cost Center) & Internal Order | Missing — Internal Order likely subsumed by WBS `IsAccountAssignmentElement`, confirm intentionally rather than build twice |
| 19 | Withholding Tax & Tax Jurisdiction Code | Missing, low-mid priority for KSA |
| 20 | Fixed Assets (entire module) | Missing — **high relevance**: cranes, trucks, generators, formwork, site equipment are real capital assets for this business |
| 21 | Inventory / Warehouse Management (entire module) | Missing — `Item.cs`'s own doc comment presupposes an Inventory module that doesn't exist; GRN never increments any stock balance |
| 22 | Plant/Equipment Maintenance | Missing — distinct from Fixed Assets (depreciation) vs. Maintenance (service schedule, breakdown tracking) |
| 23 | Real Estate / Site-Land Management | Open question — depends on whether the business owns/leases land as a concern separate from Projects |

**Also missing but not previously flagged, standard on any Business Partner/AR module:**
- No **Accounts Receivable / Customer Invoice** object at all — `Modules.Finance` today is AP-only. There is
  no way to bill a customer, record a customer receipt, or age a receivable.
- No **Aging** (AP or AR) — no "30/60/90 days overdue" concept exists anywhere; a real ERP needs this for
  both cash-flow management and audit.
- No **Budget** enforcement — `PassThroughBudgetCheckService` in `Modules.Finance/Infrastructure` is a
  literal pass-through stub; nothing ever actually blocks a transaction for exceeding budget, despite
  `IBudgetCheckService` being called in the PO/PR flow.

---

## 4. Construction-industry-specific gaps (new — not previously documented anywhere in the repo)

This section evaluates HadionERP against what a **construction-specialized ERP** (SAP RE-FX + PS-CO,
Viewpoint Vista, CMiC, Procore/Sage 300 CRE, Oracle Aconex) provides beyond generic Finance/Procurement/HR.
`Modules.Construction`'s own README already *names* several of these (Contracts, BOQ, Subcontracts, Site
Progress, Variation Orders, Retention) — none of them have any code yet. This section adds the items the
README doesn't mention at all.

### 4.1 Named in the README, zero code (build these first — Phase 3 exit criteria)
- **Customer Contracts** — the commercial contract a Project is executed against (contract value, contract
  type — Lump Sum / Unit Price / Cost Plus, payment terms, advance payment %, defects liability period).
- **Bill of Quantities (BOQ)** — the line-by-line quantity/rate breakdown mapped onto WBS elements; this is
  the backbone construction ERPs price, bill, and measure progress against. Nothing like it exists yet —
  `WbsElement` only carries Code/Name/hierarchy flags, no quantity or rate fields.
- **Subcontracts** — procurement documents assigned to a WBS element, distinct from a standard PO (needs
  retention %, mobilization advance, back-charges, subcontractor performance tracking).
- **Site Progress / Measurement** — recording physical % complete or measured quantity against a BOQ line,
  which is what should drive both Progress Billing (below) and Results Analysis/WIP in Finance.
- **Variation Orders** — change orders that adjust a WBS element's planned cost/revenue; needs its own
  approval workflow separate from the base contract's.
- **Retention** — the withheld percentage (typically 5-10%) on both certified payments to the company (from
  the customer) and payments to subcontractors, released after a defects-liability period.

### 4.2 Not named anywhere — genuine blind spots for a construction ERP
- **Progress / Certified Payment Billing (Interim Payment Certificates)** — the actual invoice-to-customer
  cycle for construction is not a simple AR invoice: it's a **Payment Application** referencing measured BOQ
  progress, subject to the customer's/consultant's certification before it becomes billable. Since there is
  no AR module at all yet (§3 above) and no BOQ/measurement concept, this entire chain is currently
  impossible to represent.
- **Work-in-Progress (WIP) / Percentage-of-Completion accounting** — construction revenue recognition (IFRS
  15 / ASC 606 percentage-of-completion, or SAP's Results Analysis) computes recognized revenue from
  cost-incurred-to-date vs. estimated-total-cost per project, distinct from invoiced amounts. This is
  referenced conceptually in `docs/architecture/07-project-accounting-and-financial-architecture.md` but has
  zero implementation; it depends on real WBS cost postings, which don't exist yet either.
- **Cost-to-Complete / Estimate-at-Completion (EAC) forecasting** — the core project-controls discipline of
  comparing Budgeted Cost vs. Actual Cost vs. Forecast-to-complete per WBS element/cost code. No entity or
  report anywhere supports this.
- **Job Costing / Cost Codes** — most construction ERPs use a dedicated Cost Code structure (e.g. CSI
  MasterFormat divisions) crossed with WBS/Project, richer than a flat GL Cost Center. HadionERP currently
  has only the generic `CostCenter` from Master Data — no construction-specific cost coding.
- **Subcontractor & Supplier Insurance/Bonds/Guarantees tracking** — Performance Bonds, Advance Payment
  Guarantees, Retention Bonds, and insurance certificate expiry tracking (with renewal alerts) are standard
  in Vendor Prequalification-adjacent modules on real construction ERPs. `VendorPrequalification` exists but
  has no bond/guarantee/insurance-expiry fields.
- **Equipment/Plant Costing & Utilization** — beyond Plant Maintenance (§22, service scheduling), construction
  ERPs also track equipment **cost allocation to projects** (internal hire rates, fuel/operator cost per
  project) — a distinct concern from both Fixed Assets (book value) and Maintenance (service schedule).
- **Document Control / Drawings & Revisions** — construction-specific document management (drawing
  register, revision control, RFIs — Requests for Information, submittals, transmittals) is a category of
  its own in tools like Aconex/Procore; `Platform.Attachments` is generic file storage with no
  drawing-revision or RFI workflow concept.
- **Site Diary / Daily Progress Report** — a structured daily log (weather, manpower on site, equipment used,
  work performed, delays/incidents) that many jurisdictions and main contracts require as a formal record;
  no equivalent exists.
- **HSE (Health, Safety, Environment) Incident Tracking** — near-miss/incident logging, safety observation
  tracking, and toolbox-talk records — standard in construction-specific ERP/EHS modules; absent entirely
  (though Vendor Prequalification does have an HSE *approval step*, which is a different thing).
- **Joint Venture (JV) / Consortium Accounting** — for large contracts executed as a JV, real construction
  ERPs support partner cost/revenue-sharing splits; not modeled anywhere, and depends on §2 (Multi-Company)
  existing first.
- **Labor/Manpower Tracking (as distinct from Payroll)** — daily timesheets tied to a WBS element/cost code
  for productivity tracking, separate from `Modules.Payroll`'s pay-run concern; not modeled.
- **Material Reservation/Issue to Site** — even before a full Inventory module (§21), construction ERPs
  typically support "reserve this material for this project/WBS element" — currently impossible since there
  is no on-hand-quantity concept at all.

---

## 5. Priority recommendations (combining platform, core-data, and construction gaps)

**Tier 1 — should close before any real pilot/production use:**
1. Business Partner missing fields (§3.15) — Payment Terms, Bank/IBAN, Credit Limit, Reconciliation Account
2. Accounts Receivable / Customer Invoice + Aging (both AP and AR) — flagged new in §3
3. Fiscal Year / Period management (§2.11) — a Journal Entry can currently post to any date forever
4. Real Budget Check (§3, `PassThroughBudgetCheckService` is a stub) — the PO/PR flow already calls it, it
   just never actually enforces anything
5. Amount-conditioned approval matrices (§2.5) — a construction company's PO values vary by orders of
   magnitude; single-tier approval is a real control gap

**Tier 2 — needed to hit Phase 3's own stated exit criteria ("a project can be set up with a BOQ,
subcontracted, progress-measured on site, and [presumably] billed"):**
6. BOQ mapped onto WBS elements (§4.1)
7. Subcontracts as a distinct procurement document type (§4.1)
8. Site Progress/Measurement (§4.1)
9. Variation Orders (§4.1)
10. Retention terms on both AR and AP sides (§4.1)
11. Progress/Certified Payment Billing (§4.2) — the real payoff of items 6-10; without it, BOQ/measurement
    exist but can't actually generate a customer invoice

**Tier 3 — construction-differentiated capability, natural fit after Phase 3:**
12. Fixed Assets (§3.20) — cranes/trucks/generators/formwork
13. Plant/Equipment Maintenance (§3.22) + Equipment Costing/Utilization (§4.2)
14. WIP/Percentage-of-Completion revenue recognition (§4.2)
15. Cost Codes distinct from generic Cost Center (§4.2)
16. Inventory/Warehouse Management (§3.21) — site material tracking

**Tier 4 — real but lower urgency / genuinely later-phase:**
17. Multi-Company/Legal Entity (§2.2) + JV/Consortium accounting (§4.2) — needed together once a second
    entity or JV contract is real
18. Multi-currency (§2.10)
19. Row/Field-level security (§2.4) — becomes real once Tier 4-17 exists to scope against
20. Document Control/Drawings/RFIs, Site Diary, HSE Incident Tracking (§4.2) — high value but not
    Finance/Procurement-blocking; candidates for Phase 5/6 or a dedicated Construction-Ops checkpoint
21. Notifications & Output Management (§2.7), ZATCA wiring (§2.8), Hijri calendar wiring (§2.9) — all have
    working backend code already, just need to be connected

**Open question to resolve before scoping, not to guess at:**
22. Real Estate/Site-Land Management (§3.23) — does the business own/lease land as a concern separate from
    the Projects built on it?

---

## 6. How this maps onto the existing roadmap

This document does not propose a competing roadmap — `docs/architecture/06-roadmap.md` already sequences
most of the above correctly (Phase 3 = Construction/PM, Phase 4 = HR/Payroll + Fixed Assets/Multi-Company,
Phase 5 = Reporting/Notifications, Phase 6 = Extensibility). The two changes this document recommends:

1. Mark `ARCHITECTURE-AUDIT.md` §16 as resolved (§0 above) so the audit file stays trustworthy.
2. Elevate **Accounts Receivable + Progress/Certified Billing + BOQ** as a *combined* Tier 1/2 priority — the
   roadmap currently treats "AR/Cash-Bank" as one deferred label and BOQ as a Phase 3 line item, but for a
   construction company specifically, **AR is not usable without BOQ/measurement**, and BOQ/measurement is
   **not valuable without AR to bill against** — they are one real-world capability, not two independent
   backlog items, and should be scoped/built together rather than sequentially.

---

## 7. Roadmap Expansion Review — Treasury, Employee Finance, HR, Statements Center, Construction Ops, Equipment

*(Added 2026-07-16, in response to a solution-architect-style roadmap review request. Verified directly
against current source — none of the concepts below have any code today; the only matches a codebase search
returned were incidental false positives (e.g. a `Level` field on `GLAccount.cs`, unrelated `MaskingStrategies.cs`
text). This section assumes zero existing coverage for everything listed unless stated otherwise.)*

### 7.1 Treasury & Cash Management — **entirely missing**
Today `Modules.Finance` has `BankAccount`/`Payment`/`PaymentAllocation` only (§3.16 above). Nothing else in
this domain exists:

| Missing | Why it's required | Depends on |
|---|---|---|
| Cash Boxes / Petty Cash Custodians | Site offices run on physical cash float, not just bank transfers — every construction company has custodian-held petty cash that needs its own sub-ledger | BankAccount (as the funding source), Employee (as custodian) |
| Cash Advances / Petty Cash Settlement / Replenishment | The advance→spend→settle→replenish cycle is how petty cash actually gets accounted for; without it "petty cash" is just an unreconciled bank withdrawal | Petty Cash Custodian, GL posting |
| Cash Counts | Physical reconciliation of custodian float vs. book balance — an internal-control requirement most auditors will ask for | Petty Cash Custodian |
| Bank Reconciliation | Referenced in `HD-FIN-001`'s Period Status Panel mockup text ("2 Bank Reconciliations") but no domain object exists to reconcile against | BankAccount, Payment |
| Check Management | Post-dated checks (issued and received) are still common in KSA/GCC construction payment terms; needs its own status lifecycle (Issued → Cleared → Bounced/Cancelled), distinct from a simple Payment | BankAccount, Payment |
| Letter of Credit (LC) | Import-heavy construction procurement (steel, MEP equipment) commonly uses LCs; needs tracking of issuing bank, expiry, utilization against POs | Procurement (PO), BankAccount |
| Letter of Guarantee (LG) | Performance bonds, advance payment guarantees, retention guarantees — already flagged as a gap in §4.2 of this document from the construction-ERP angle; this section confirms it from the Treasury angle too | Vendor/Customer Contract, BankAccount |
| Bank Charges | Currently the mockup (`HD-FIN-001` Journal Entry example) shows a manual "Bank Charges" journal — that's a workaround, not a modeled transaction type | BankAccount |
| Treasury Reporting / Cash Forecasting | The Finance Dashboard mockup already *shows* a "Cash Flow Forecast" widget with a 7-day projection — but nothing in the domain model computes that; it's currently a static mockup number | Bank Accounts, AP/AR, Payroll (for outflow forecasting) |

**Priority: Core.** Every item here is a normal weekly operation at a real construction company; without it,
"Finance" only covers formal AP/GL, not how cash actually moves day to day.

### 7.2 Employee Financial Management — **entirely missing**
`Modules.HR` and `Modules.Payroll` are both empty (README-only, confirmed in §1). This means none of the
following exist even conceptually yet:

| Missing | Why it's required | Depends on |
|---|---|---|
| Payroll Payables | The GL liability side of payroll — needed the moment Payroll exists at all | Payroll, GL |
| Salary Advances / Employee Loans | Extremely common in construction workforces (often the majority of blue-collar staff); needs its own approval + repayment-schedule lifecycle, deducted from future payroll runs | Payroll, GL |
| Business Trip Advances / Expense Claims / Reimbursements | Site engineers and project managers travel between sites/HQ regularly; without this, travel spend either doesn't get tracked or gets forced through AP as a workaround | Payroll or AP, Project (for cost allocation) |
| Deductions | Generic deduction engine (loan repayment, advance recovery, disciplinary, GOSI) — a payroll primitive, not a one-off feature | Payroll |
| Final Settlement / End of Service Benefits (EOS) | Legally mandated in KSA on contract termination/resignation — this is not optional for a KSA construction ERP | Payroll, HR (tenure/contract data) |
| Vacation Liability | The accrued-but-unpaid annual leave balance is a real GL liability that needs monthly accrual, not just an HR leave-balance number | HR (Leave Management), GL |
| Air Ticket Eligibility / Ticket Encashment | Standard GCC expatriate-workforce benefit (contractual annual ticket entitlement, or cash-in-lieu) — construction workforces in KSA are majority expatriate, so this isn't a niche feature | HR (contract terms), Payroll |
| Employee Financial Statement | A per-employee running ledger (advances, loans, deductions, EOS accrual) — the employee-side equivalent of the Statements Center in §7.4 | Payroll, all of the above |

**Priority: Core for a KSA construction workforce specifically** — EOS and ticket entitlement in particular
are not "nice to have," they're contractual/legal obligations. **Depends entirely on `Modules.HR` and
`Modules.Payroll` existing at all first** — currently blocking, since both modules are 0% built.

### 7.3 Human Resources — **entirely missing, scope wider than typically assumed**
Beyond the general employee-lifecycle items (Recruitment, Onboarding, Transfers, Promotions, Contract
Management, Probation, Leave, Attendance, Overtime — all standard and all currently unbuilt), two items are
specifically worth calling out because they're easy to underscope if HR is designed generically rather than
for a KSA-based, expatriate-heavy construction workforce:

- **Document Expiry Monitoring (Passport / Visa / Iqama / Medical Insurance / Certifications)** — this needs
  to be a first-class, proactively-alerting capability, not a file attachment with a date field. An expired
  Iqama can legally stop an employee from being on a job site; this should feed the same Priority Alerts
  panel defined in `HD-WS-001`, not sit in an HR record nobody checks.
- **Exit Clearance / Asset Return** — tied to Final Settlement (§7.2): an employee can't be paid out until
  site equipment, PPE, and company property are confirmed returned. This is a workflow that spans HR,
  Equipment/Fleet (§7.6), and Payroll — a good example of why the Bible's "no independent modules" principle
  (`01-Enterprise-Philosophy`) matters operationally, not just visually.

**Priority: Core.** Same blocking dependency as §7.2 — `Modules.HR` is currently 0% built.

### 7.4 Financial Statements Center — **entirely missing, and architecturally significant**
No unified statement concept exists anywhere in the codebase today — AP has invoices, GL has journal entries,
but there is no "running balance ledger view" for any party. This is worth treating as its own build item
rather than an afterthought of each module, because:

- It's the natural real-world answer to `HD-OBJ-001`/`HD-OBJ-003`'s repeated requirement that every object
  expose a **Financial Impact panel** (flagged as `☆☆☆☆☆ BUILD` in both specs, and confirmed missing on both
  object-page mockups in the earlier compliance audit) — a Statement is what that panel actually *is* for
  any party-type object (Customer, Supplier, Subcontractor, Employee).
- Building it once as a generic "Statement" capability (Opening Balance → Transactions → Running Balance →
  Aging where applicable) and parameterizing it per party type (Customer/Supplier/Subcontractor/Employee/Cash
  Box/Bank/Equipment/Vehicle/Asset/Contract/Project/GL Account) avoids exactly the kind of per-module
  duplication `01-Enterprise-Philosophy` warns against.
- It has hard dependencies on almost everything else in this document: it can't be built before AR exists
  (§3 of this document), before Petty Cash exists (§7.1), before Payroll exists (§7.2/7.3), or before
  Equipment cost tracking exists (§7.6) — so while the *concept* should be designed now, the *rollout* is
  necessarily incremental, one party-type at a time, as each underlying domain gets built.

**Priority: Core capability, Incremental rollout.** Design the generic Statement pattern now so every module
built afterward (AR, Petty Cash, Payroll, Equipment) implements it the same way from day one, rather than
retrofitting five different "statement-like" screens later.

### 7.5 Construction Operations — mostly overlaps §4 of this document, two genuinely new items
Most of this list (WBS, Progress Measurement, Retention, Variations — even "Multiple Subcontractors per
Activity") was already covered in §4.1/§4.2 above. Two items are worth adding as new, since they weren't
previously flagged:

- **IPC (Interim Payment Certificate) Management** — this is a more precise name for what §4.2 called
  "Progress/Certified Payment Billing." Worth noting explicitly: an IPC is a *distinct document type* from a
  standard AR invoice — it has its own approval chain (contractor submits → consultant reviews/certifies →
  possibly disputed/partially certified → then becomes billable), and the *certified* amount can differ from
  the *submitted* amount. Modeling it as "AR Invoice with extra fields" would lose that submitted-vs-certified
  distinction, which is central to how construction billing disputes actually get resolved.
- **Back Charges / Material Recovery** — deducting subcontractor-caused costs (damage, rework, materials
  supplied by the main contractor) from a subcontractor's next certified payment. This is a genuine gap not
  previously listed: it requires a link between a cost event (e.g. a Site Instruction or a rework GRN) and a
  specific Subcontract's next payment cycle — effectively a negative line item inside IPC Management above.

**Priority: Core** (same tier as the rest of §4's Construction findings — these block Phase 3's own stated
exit criteria).

### 7.6 Equipment & Fleet — **entirely missing, and currently conflated with two other concerns**
This document's original audit (§3.20–3.22) already flagged Fixed Assets and Plant Maintenance as missing,
and flagged Equipment Costing/Utilization as a construction-specific gap in §4.2. This roadmap review adds
enough detail to make clear **Equipment & Fleet is not the same thing as those two**, and conflating them
will cause modeling problems later:

| Concern | What it tracks | Existing gap reference |
|---|---|---|
| **Fixed Assets** | Book value, depreciation schedule, capitalization — an *accounting* concern | §3.20 |
| **Plant Maintenance** | Service schedules, breakdown/repair tickets — an *operational reliability* concern | §3.22 |
| **Equipment & Fleet (this section)** | Fuel consumption, operator assignment, inter-project transfers, rental-in/rental-out, utilization %, and — critically — **cost allocation to whichever project is using the equipment on a given day** | New |

The third concern is the one with no home anywhere in the current design. An excavator's fuel and operator
cost need to land on the correct Project/WBS element's Actual Cost for that project's Financial Impact panel
and Project Finance widget (both already specified in `HD-FIN-001`/`HD-OBJ-003`) to mean anything. Without
Equipment cost allocation, "Actual Cost" on a project is understated by however much company-owned equipment
was used on it — a material accuracy problem for a construction P&L, not a cosmetic one.

**Priority: Important** (not blocking Phase 3 the way BOQ/AR/Retention are, but should follow immediately
after — construction project cost accuracy is directly at stake).

### 7.7 Object Page Standard — no new gap
This requirement (Overview / Transactions / Documents / Attachments / Activities / Approvals / Financial
Summary / Analytics / Related Records / Audit Trail / Timeline) is a UX/presentation-layer standard rather
than a missing data domain — every business object listed throughout this document (BOQ, Subcontract, IPC,
Petty Cash Custodian, Equipment, etc.) should expose these sections once built, but that's a page-template
requirement applied consistently at implementation time, not an additional backend capability to scope here.

### 7.8 Updated priority tiers (supersedes §5 for the newly-reviewed domains)

**Tier 1 additions (blocking, same urgency as the original Tier 1):**
- Generic Statement pattern design (§7.4) — design now even though rollout is incremental, so nothing already
  being built (AR, Petty Cash) has to be retrofitted later.

**Tier 2 additions (construction-differentiated, same tier as original BOQ/Subcontracts/Retention):**
- IPC Management as its own document type distinct from AR Invoice (§7.5)
- Back Charges / Material Recovery (§7.5)

**Tier 3 additions (construction-differentiated capability, natural fit after Phase 3):**
- Equipment & Fleet cost allocation to projects, kept explicitly separate from Fixed Assets and Plant
  Maintenance (§7.6)

**New Tier — HR/Payroll foundation (currently has no tier in the original document because both modules were
scoped as empty placeholders; this roadmap review shows they carry real Core-priority weight, not just Future
scope):**
- `Modules.HR` and `Modules.Payroll` basic structure — blocking dependency for §7.2 and §7.3 in full
- Employee Financial Management (§7.2) — Salary Advances, Loans, EOS, Vacation Liability, Ticket Encashment
- HR document-expiry monitoring wired into Priority Alerts (§7.3) — small to build once HR exists, high
  real-world consequence if skipped
- Treasury & Cash Management (§7.1) — Petty Cash cycle, Bank Reconciliation, Checks, LC/LG

This tier was previously invisible in the roadmap because HR/Payroll/Treasury read as "just missing modules"
rather than as **dozens of individually load-bearing business processes** — the point of this section is that
they should be scoped and estimated at that level of granularity, not as three single backlog line items.

---

## 8. Fixed Assets & Depreciation-to-Project Cost Allocation — deep dive

*(Added 2026-07-16. Confirmed by direct code search: zero matches for "FixedAsset" or "Depreciation" anywhere
in `src`. §3.20 already flagged Fixed Assets as missing in general terms — this section covers the specific
mechanic asked about: when Machine 1 works on Project A this month, how does its depreciation actually land
on Project A's cost, and how do SAP/Dynamics solve it.)*

### 8.1 The real-world problem
A piece of owned equipment (excavator, crane, generator) is a single Fixed Asset in the books, but it doesn't
sit on one project forever — it gets deployed to whichever project needs it, sometimes moving mid-month,
sometimes splitting time across two projects in the same period. Two separate numbers have to come out of
this correctly:

1. **Statutory depreciation** — the accounting-mandated monthly expense (straight-line, declining balance,
   etc.), which is a property of the asset itself and generally doesn't change based on where it was used.
2. **Project cost allocation** — *which project's Actual Cost absorbs that depreciation expense*, which
   changes every time the asset moves. This is what makes Project A's P&L honest — if Machine 1 depreciates
   SAR 10,000/month and spent three weeks on Project A and one week on Project B, Project A's Actual Cost
   should carry roughly SAR 7,500 of that, not SAR 0 and not the full SAR 10,000.

Today HadionERP has **neither the asset nor the allocation mechanism** — this is a compounding gap on top of
the WBS/Cost Code gap already flagged in §4.2: even once Fixed Assets exists, it needs to know how to talk to
WBS elements to do this correctly.

### 8.2 How SAP handles it (FI-AA + CO/PS)
- Every Fixed Asset master record carries a **cost-object assignment** — a Cost Center, Internal Order, or WBS
  Element — on its "Time-Dependent Data" tab. Critically, this assignment is **time-dependent, not a static
  foreign key**: it has an effective-from date, so when Machine 1 moves from Project A's WBS element to
  Project B's mid-month, the asset master gets a new time-dependent row rather than overwriting the old one.
- The periodic **Depreciation Run** (transaction AFAB) reads whichever cost-object assignment was valid on
  each day/period it's posting for, and posts the depreciation journal entry (Dr Depreciation Expense / Cr
  Accumulated Depreciation) **split and allocated to the correct WBS element(s) automatically** based on that
  time-dependent history — no manual journal entry required.
- When an asset genuinely serves multiple cost objects **simultaneously** (not just sequentially), SAP
  supports **Distribution Rules** that split a single asset's depreciation across multiple WBS
  elements/cost centers by percentage, rather than forcing an all-or-nothing assignment.
- Separately — and this is the part construction companies actually rely on for internal project costing —
  SAP Plant Maintenance (PM) equipment records support **internal activity allocation**: an hourly/daily
  internal usage rate posted against actual confirmed usage hours (via time confirmation), which is a
  *variable, usage-based* cost distinct from the *fixed, statutory* depreciation charge. A real project's
  "equipment cost" in SAP construction implementations is usually the sum of both: the allocated depreciation
  share **plus** the internal usage/hire charge for hours actually worked that period.

### 8.3 How Dynamics 365 handles it
- Business Central's Fixed Asset card carries a **default dimension** (Project, Cost Center) and supports an
  **FA Allocation Key** — a percentage table — so a single depreciation entry can be split across multiple
  dimension values (multiple projects) in one posting, rather than requiring one asset per project.
- Dynamics 365 Finance & Operations (Project Operations) goes further: equipment can be set up as a **Project
  resource with an internal hourly/daily cost rate**, bookable to specific projects, so project cost absorbs
  equipment usage the same way it absorbs labor — as a resource booking, not just a depreciation journal
  split. Statutory depreciation still posts through the Fixed Assets module separately; project costing and
  statutory depreciation are related but not the same posting.

### 8.4 The pattern, distilled
Both systems separate this into **two linked but distinct mechanisms**, and HadionERP needs both, not either:

| Mechanism | What it answers | Frequency | Where it depends |
|---|---|---|---|
| **Time-dependent cost-object assignment on the Fixed Asset** | "Which project should this month's *statutory* depreciation land on?" | Changes only when the asset physically moves | Fixed Asset domain (§3.20, still 0% built) linked to WBS Element (exists, but has no cost-object linkage field today) |
| **Equipment usage/internal hire allocation** | "How much of this asset's *operating* cost did this project actually consume this period?" | Changes with every day/hour of actual usage, independent of the depreciation schedule | Equipment & Fleet (§7.6, flagged as missing) linked to WBS Element via confirmed usage hours |

Building only the first (a simple FK from Asset to Project) would be a naive implementation that breaks the
moment equipment moves projects mid-period or serves two projects at once — exactly the scenario in the
question that prompted this section. **The FK needs to be a dated/versioned assignment, and there needs to be
a genuine allocation/distribution step at depreciation-posting time**, not a static lookup.

### 8.5 What this means for HadionERP specifically
1. **Fixed Assets (§3.20) cannot be built as a generic, construction-agnostic module** — it needs the
   time-dependent WBS/Project assignment and distribution-rule capability from day one, or it will need a
   breaking rework the first time a real customer moves equipment between projects, which for a construction
   company is not an edge case, it's normal monthly operation.
2. **Fixed Assets and Equipment & Fleet (§7.6) are two different domains that must share a WBS-facing
   interface** — Fixed Assets owns statutory depreciation and book value; Equipment & Fleet owns usage
   tracking, fuel, maintenance, and operator assignment; Project Cost Control needs numbers from both to be
   accurate, the same way `HD-FIN-001`'s Project Finance concept describes "Actual Cost" as a single number
   the project trusts.
3. **Priority: Core, and sequenced ahead of general Fixed Assets** — recommend building the time-dependent
   cost-object assignment pattern as part of the *first* version of Fixed Assets, rather than shipping a
   simple asset register now and retrofitting allocation logic later once real equipment-sharing scenarios
   surface in production.

---

## 9. Final consolidated missing-list (all sections, ranked)

This is the short version of everything in this document, for planning purposes.

**Blocking / Core — build first:**
- Accounts Receivable + Aging (§3) — Finance is currently AP-only
- Real Budget Check (§3) — currently a pass-through stub despite being called in the PO/PR flow
- Fiscal Year / Period management (§2.11)
- BOQ mapped onto WBS, Subcontracts, Site Progress/Measurement, Variation Orders, Retention (§4.1) —
  Phase 3's own stated exit criteria
- Progress/Certified (IPC) Billing as its own document type, distinct from AR Invoice (§4.2, §7.5)
- Back Charges / Material Recovery (§7.5)
- Generic Statement pattern design (§7.4) — design now, roll out incrementally per party type
- `Modules.HR` / `Modules.Payroll` foundation, including Salary Advances, Loans, EOS, Vacation Liability,
  Ticket Encashment, document-expiry monitoring (§7.2, §7.3)
- Treasury: Petty Cash cycle, Bank Reconciliation, Checks, LC/LG (§7.1)
- **Fixed Assets with time-dependent WBS/Project cost-object assignment + distribution rules** (§8) — not a
  simple asset register; must support equipment that moves between or serves multiple projects

**Important — sequence immediately after the above:**
- Equipment & Fleet usage/internal-hire cost allocation to projects, kept distinct from Fixed Assets
  depreciation and from Plant Maintenance service scheduling (§3.22, §7.6, §8.4)
- WIP/Percentage-of-Completion revenue recognition (§4.2)
- Cost Codes distinct from generic Cost Center (§4.2)
- Multi-Company / Legal Entity structure (§2.2)
- Inventory / Warehouse Management (§3.21)

**Real but lower urgency:**
- Multi-currency (§2.10), Withholding Tax (§3.19), Document Control/Drawings/RFIs/Site Diary/HSE (§4.2),
  Notifications & Output Management (§2.7), ZATCA/Hijri wiring (already built, just unwired — §2.8/§2.9)

**Open questions to resolve before scoping, not to guess at:**
- Real Estate/Land Management as a concern separate from Projects (§3.23)
- Joint Venture/Consortium accounting, which depends on Multi-Company existing first (§4.2, §7.5 Tier 4)
