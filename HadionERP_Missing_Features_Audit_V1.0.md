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
