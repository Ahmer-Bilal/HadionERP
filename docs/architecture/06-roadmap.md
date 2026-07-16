# 06 — Development Roadmap

No business modules are built until Phase 0 (the platform kernel) is solid — every subsequent module leans
on it, and retrofitting security/localization/workflow/audit into modules built without them is exactly the
kind of rework this architecture exists to avoid.

**Standing rule for every phase below (added 2026-07-13)**: at the end of each phase, the solution must
compile, `src/Gateway/Gateway.Api` (backend) and `src/Apps/Apps.Shell` (frontend) must both start without
errors, and the work must be verifiable by opening the application in a browser — not just by a passing
test suite. Everything built is production-shaped from the start; nothing here is a prototype meant to be
replaced later. See AGENTS.md.

## Phase 0 — Platform Foundation (prerequisite for everything else)
- `Platform.Core`: BO base classes, FSM lifecycle engine, number ranges, extension field storage
- `Platform.Security`: authN (OIDC/SSO), RBAC+ABAC, row/field-level security, SoD engine
- `Platform.Localization`: AR/EN resource pipeline, RTL, Hijri/Gregorian calendars, base KSA tax framework
- `Platform.Events`, `Platform.Workflow`, `Platform.Audit`: eventing, approval engine, immutable audit log
- `Platform.UI`: design system, List Report + Object Page templates, Apps.Shell + Intent Router
- `Platform.Api`, `Platform.Configuration`: API conventions, config hierarchy, rule engine
- `tools/bo-scaffold`, `tools/object-page-gen`: codegen so Phase 1+ modules are fast to stand up
- **Exit criteria**: a trivial demo BO (no business meaning) can be scaffolded end-to-end — created,
  submitted, approved via a configured workflow, posted, audited, printed, bilingually — proving the whole
  kernel works before any real business logic is written. In addition (per the standing rule above):
  `Gateway.Api` runs and serves a real system-status/health surface built on the kernel services, and
  `Apps.Shell` runs and displays it in a browser, in both English and Arabic.

## Phase 1 — Master Data + Finance Core
- `Modules.MasterData`: Business Partners, Chart of Accounts, Items, Cost Centers, Tax codes, Number ranges
- `Modules.Finance`: GL, AP, AR, Cash/Bank — the ledger every other module eventually posts into
- ZATCA e-invoicing Phase 1 (QR-coded compliant invoices) live for AR
- **Built 2026-07-14 (ahead of the rest of Phase 2)**: `BusinessPartner.PartnerType` (Customer/Vendor/Both,
  a single enum) has been replaced by `BusinessRoles` — a multi-select child collection, same pattern as
  `Addresses`/`Contacts` — since a real construction-industry partner commonly holds several roles at once
  (a company can be both a Supplier and a Subcontractor). Built first, ahead of the rest of Phase 2, because
  Vendor Prequalification (below) needs it to exist. See `Modules.MasterData/README.md`'s "Phase 2:
  BusinessRoles replaces PartnerType" section for what actually shipped.
- **Exit criteria**: a company can maintain its chart of accounts and vendors, and post/reverse a GL journal
  and an AP invoice end-to-end with full audit trail.

## Phase 2 — Procurement
- `Modules.Procurement`: PR → RFQ → PO → GRN → 3-way match against AP
- Budget-check integration event flow with Finance
- **Vendor Prequalification & Business Roles** (design captured 2026-07-14, owner decided to be
  Procurement, not Master Data — matches SAP Ariba SLP / Dynamics Vendor Onboarding practice: qualification
  is a procurement process against a master-data party, not master data itself):
  - `BusinessPartner.BusinessRoles`: multi-select, not the current single `PartnerType` — Client (the
    construction-industry label for what SAP calls Customer/Debtor — do not model both as separate roles,
    they're the same AR-invoiced counterparty), Supplier, Subcontractor, Consultant, Joint Venture Partner,
    Government Authority, Rental Company, Manufacturer, Manpower Supplier, Testing Laboratory. Each
    Supplier/Subcontractor/Consultant-family role carries its own Trade/Specialty sub-classification
    (e.g. Subcontractor → Electrical/Concrete/Mechanical/Steel Structure/Earthworks; Supplier →
    Steel/Cement/MEP Materials/Aggregates; Consultant → Structural/Architectural/MEP Design/Geotechnical) —
    a configurable lookup (`Platform.Configuration`), not a hardcoded enum, since trades vary by discipline
    and grow over time.
  - **Government Authority is not prequalified at all** — no commercial relationship, no AP/AR posting, no
    scorecard. It exists purely so permit/license/inspection correspondence has a Business Partner to
    attach to (mirrors SAP's public-sector "Authority" partner role). Prequalification logic must therefore
    be conditional on role (reusing the same `AttributeConstraints` condition-gating `Platform.Workflow`
    steps already use), not a blanket process every partner goes through.
  - **Joint Venture is a relationship, not just an attribute** — a JV is two or more partners forming a
    specific arrangement for a specific project (who, which project, ownership split, lead partner), which
    a role checkbox alone can't capture. Keep "Joint Venture Partner" as a simple role for now (this
    external company is one of our JV partners); real JV modeling (the partnership itself) is a
    Project/Contract-level concern for a later phase, deliberately not solved here.
  - `VendorPrequalification` (or similar — final BO name TBD when this phase starts): a real Business
    Object, one per Business Partner + Role + Trade/Specialty (a vendor can be prequalified as a Steel
    Supplier without being prequalified as an Electrical Subcontractor), standard BO lifecycle
    (Draft → Submit → Approved/Rejected) driven by `Platform.Workflow` with role-specific review steps —
    Commercial (CR, financial statements, bank guarantee capacity), Legal (GOSI, Saudi Contractor
    Classification grade, Zakat/VAT certificate), Technical (experience, project references, capacity —
    criteria genuinely differ per role: a Manpower Supplier needs GOSI/Iqama-sponsorship/WPS compliance, a
    Testing Laboratory needs ISO/IEC 17025 accreditation specifically, a Rental Company needs equipment
    calibration/certification records, a Manufacturer needs a factory/production-capacity audit — one
    generic checklist does not fit all roles), HSE (safety record, HSE management system), and Quality
    (ISO 9001/14001/45001). Configurable validity period (industry-typical 1–3 years) with re-qualification
    before expiry, via `Platform.Configuration`, not a hardcoded duration.
  - Supporting documents (CR copy, GOSI certificate, ISO certificates, bank letter, HSE policy, etc.) are
    the first real use case for the platform's Attachments capability (`Platform.Core`, itself still not
    built as of 2026-07-14) — build Prequalification against a real document-heavy process, not a toy one.
  - Every score, decision, and re-qualification flows through `Platform.Audit`/`Platform.Workflow`/
    `Platform.Security` exactly as Business Partner's own onboarding approval already does — no new
    platform capability required, only a new module consuming the existing ones.
  - Natural (but later, not this phase) extension: post-award Vendor Performance Management/blacklisting
    feeding back into future re-qualification decisions, the way SAP Ariba SLP separates one-time
    qualification from ongoing performance scoring.
- **Exit criteria**: full procure-to-pay cycle with configurable approval matrix.

## Checkpoint — UI/Visual Density Pass (before Phase 3, decision 2026-07-14)
Doc 02 §2/§3 already specifies the *target* pattern — merged List+Details form, dense sortable/filterable
grid (compact rows, saved views, like Dynamics 365's vendor list), FastTabs, an Action Pane command bar
driven by BO state/security, and drill-down where a key/ID field renders as a hyperlink (e.g. blue vendor
ID) that navigates to that record's own full List+Details form. What's actually been hand-built through
Phase 0–2 (Business Partner, GL Account, Items, Cost Centers, Tax Codes, Journal Entry, AP Invoice, Vendor
Prequalification, Purchase Requisition, RFQ) is functionally correct but visually simpler — each page was
hand-rolled per slice, `tools/object-page-gen` was never finished into a real generator, and there's no
shared `Platform.UI` List+Details component yet.

- **Do the pass once Phase 2's procure-to-pay cycle (PO/GRN/3-way match) is done, not before.** By then
  every recurring page shape has appeared at least once (flat list, hierarchical list, multi-step workflow,
  line-item grid, cross-document drill-down like PR→RFQ→PO), so one shared component retrofits every
  existing page in one go and every Phase 3+ page gets the dense look for free — cheaper than reworking each
  module's pages twice.
- **Until then, keep building new pages the same functional way already established** — don't hand-roll
  partial density per page. This note exists so nobody re-styles piecemeal before the shared component
  exists; that would be the exact rework this checkpoint is meant to avoid.
- Concretely, the pass should build a real `Platform.UI` List+Details template (or finish
  `tools/object-page-gen`) implementing: dense grids, ID/key fields as hyperlinks opening the target
  record's own page (not a modal), FastTabs replacing today's flatter detail sections, and a state-driven
  Action Pane — then apply it to every existing page, not just new ones.

## Checkpoint — Architecture Gap Audit & Platform Hardening (added 2026-07-15)

A full SAP S/4HANA/Dynamics 365-vs-HadionERP gap audit was performed 2026-07-15, per explicit user
instruction to "act as real architecture of SAP" and identify what a real deployment of either reference
product has that this system doesn't. The full findings, with file-level evidence and severity per gap, live
in **`ARCHITECTURE-AUDIT.md`** at the repo root — read that file before starting any of the items below; this
section only lists them with their assigned phase, it doesn't repeat the evidence.

- ✅ **Resolved 2026-07-15**: real Authentication & Identity (§1 of the audit) — `Modules.Identity` now
  provides real username/password login (JWT bearer tokens), a persisted Users admin surface, and a global
  default-deny authorization policy; every controller resolves the real logged-in user instead of a
  hardcoded literal. This also resolved Segregation of Duties conflict checking (§3) for free — role
  assignment now runs `ISodEngine.FindUnresolvedConflicts` for the first time ever, live-verified blocking a
  conflicting assignment and accepting an explicit override. See `Modules.Identity/README.md` and
  `PROGRESS.md`'s "Real Authentication & Identity" entry. Delegation (§6) is now *buildable* (real users
  exist) but its UI itself is still separate, not-yet-started work — not resolved by this pass.
- **Phase 1 depth, should precede real Finance production use**: Fiscal Year/Period management (§11 — no
  period-close/lock exists at all today) and actually wiring the already-built-but-unused ZATCA QR invoice
  code (§8) into `APInvoice` — Phase 1's own original scope named this, it was never finished.
- **Next Procurement/Finance approval work**: amount-conditioned approval matrices (§5) — Phase 2's exit
  criteria named "configurable approval matrix" but what shipped is role-based, not amount-conditioned; the
  workflow engine's `AttributeConstraints` condition-gating primitive already exists and is proven (Vendor
  Prequalification's 5-step chain), it has just never been pointed at a document amount. Purchase Order is
  the natural first module to prove this on.
- **Next UI pass**: wire the already-built-but-unused Hijri calendar service (§9) into date fields.
- **Phase 4+ (multi-entity depth)**: Multi-Company/Legal Entity structure (§2), Row-Level Security scoped to
  it (§4), Multi-Currency (§10).
- **Phase 4 (HR & Payroll)**: Field-Level Security for its first genuinely sensitive fields (§4 — salary,
  IBAN, national ID).
- **Phase 5 (Reporting, Analytics & Mobile)**: Notifications & Output Management (§7 — currently zero code,
  not even a stub) should land before the statutory-report generation work already scoped for this phase,
  since it's the more foundational of the two; Escalation (§6 — currently fully orphaned code) fits here too
  since it implies the same kind of scheduled background processing as report generation.
- **Phase 6 (Extensibility)**: `Platform.Extensibility`/`Platform.Integration` are currently README-only
  placeholders (§13) — confirms rather than changes this phase's already-stated scope.
- **As-needed, not urgent**: real object storage for Attachments (§14); virus scanning should land before any
  external-facing deployment specifically, independent of the storage-backend question.

**Part 2 (added same day, same file)**: a second pass audited *data-model and module completeness*
specifically, per explicit user follow-up ("i also want other things which are missing like modules and core
data... that sap/dynamic have in module 1 but we are missing"), separate from the cross-cutting platform
findings above.

- ✅ **Resolved 2026-07-16**: AP Payment Recording & Cash/Bank Management (§16, the highest-priority
  data-model gap in the whole audit) — `Modules.Finance` now has `BankAccount` master data and a `Payment`
  Business Object (Draft → Submit → Approve → Post → Reverse) whose `PostAsync` generates a real linked
  Journal Entry (Debit each allocated invoice's own Payable account, Credit the Bank Account's linked G/L
  account), with cumulative overpayment protection across multiple payments and a computed
  `APInvoice.OutstandingBalance`. See `Modules.Finance/README.md`'s "Bank Accounts & AP Payment Recording"
  section and `PROGRESS.md`'s matching entry. Business Partner master data (§15 — no default Payment Method/
  Bank Account/Payment Terms/Credit Limit/Reconciliation Account/Withholding Tax *on the vendor master
  itself*) is only partially resolved by this — Bank Account and Payment Method now exist as standalone
  concepts, but nothing on `BusinessPartner` defaults them yet, so this remains open, unassigned to a phase.
- **Phase 1/4 depth, lower urgency**: Withholding Tax as a distinct concept from VAT (§19, after §16 exists to
  withhold against); GL Document Type grouping (§17, not urgent until document-shape count grows); Profit
  Center (§18, Phase 4+).
- **Confirm against Phase 3's own WBS depth, don't build twice**: Internal Order (§18) — SAP's temporary,
  project-scoped cost collector is structurally the same job a WBS Element's `IsAccountAssignmentElement`
  already does; when Phase 3 builds real cost-posting against WBS elements, confirm intentionally whether a
  separate Internal Order concept is still needed or whether WBS already covers it.
- **New roadmap items, not previously named anywhere**: **Fixed Assets** (§20 — construction-specific
  relevance: cranes/trucks/generators/formwork as real capital assets) and **Plant/Equipment Maintenance**
  (§22 — maintenance scheduling for the same equipment, a distinct concern from Fixed Assets' depreciation
  accounting) pair naturally and are recommended as a new checkpoint between Phase 3 and Phase 4, or folded
  into Phase 4. **Inventory/Warehouse Management** (§21 — `Item.cs`'s own doc comment already presupposes an
  Inventory module that doesn't exist; Procurement's GRN records receipt but never increments any stock
  balance) is a new roadmap item best sequenced after Phase 3's Construction module exists, since site
  material consumption is the real driver of on-hand tracking for this business.
- **Open question, not a confirmed gap**: Real Estate/Site-Land Management (§23) — whether this is needed
  depends on the user's own land/site-ownership model; ask before scoping rather than guessing it into the
  roadmap.

None of the above (Part 1 or Part 2) blocks Phase 3 (Construction & Project Management, in progress) from
continuing — these are platform-hardening and new-module items layered onto or added alongside phases already
in the roadmap, not new phases inserted ahead of Phase 3.

## Checkpoint — Missing-Features Audit & Build Sequencing (added 2026-07-16)

A second, wider audit (`HadionERP_Missing_Features_Audit_V1.1.md` at repo root — supersedes the earlier
`V1.0` draft) was performed 2026-07-16, extending `ARCHITECTURE-AUDIT.md` with a construction-industry lens
(BOQ/Subcontracts/IPC billing, Fixed Assets/Equipment cost allocation, Treasury, Employee Financial
Management) and confirming §16 (AP Payment Recording) is now genuinely resolved. Read that file for the full
evidence and tiering (§5, §9) before starting any item below — this section only records the **sequencing
decision** made from it, so the next contributor doesn't have to re-derive an order from a 20-section audit.

**The core sequencing call**: the audit's own §6 finding is that **Accounts Receivable is not usable without
BOQ/measurement, and BOQ/measurement is not valuable without AR to bill against** — they're one real-world
capability (a construction company gets paid via Interim Payment Certificates measured against a BOQ, not a
plain AR invoice), not two backlog items on different phases. Phase 3 below is expanded to build both
together, rather than shipping Construction first and bolting AR on later. Everything else the audit flagged
(Treasury, Fixed Assets, Employee Financial Management, Multi-Company, etc.) is real but sequenced *after*
Phase 3, not folded into it — Phase 3 is already large; adding unrelated Finance/HR depth to it would blur
its own exit criteria.

- **Phase 3 (expanded below)**: Construction module + the AR/IPC/Statement/Fiscal-Period/Budget-Check depth
  needed to actually bill and control a project, since none of it is separable from "can this company invoice
  a customer for work done."
- **New checkpoint between Phase 3 and Phase 4** (added below): Treasury & Cash Management, Fixed Assets
  (built with time-dependent WBS cost-object assignment from day one per the audit's §8 deep-dive — not a
  simple asset register), Equipment & Fleet cost allocation, Plant Maintenance, Inventory/Warehouse
  Management, Cost Codes, WIP/Percentage-of-Completion revenue recognition, Multi-Company/Legal Entity. These
  depend on real WBS cost postings (Phase 3) existing first, and are grouped together because several of them
  share the same WBS-facing interface (see the audit's §8.5).
- **Phase 4 (expanded below)**: HR & Payroll gets a wider scope than the original roadmap line — Employee
  Financial Management (Salary Advances/Loans, EOS, Vacation Liability, Ticket Encashment — legally mandated
  or contractually standard for a KSA expatriate construction workforce, not "nice to have") and HR
  document-expiry monitoring (Iqama/Visa/Passport) wired into the same alerting surface Finance uses,
  reusing Phase 3's generic Statement pattern for the per-employee financial ledger.
- **Deliberately left later, not urgent yet**: Multi-currency, Withholding Tax, Document Control/Drawings/
  RFIs/Site Diary/HSE Incident Tracking, Notifications & Output Management, and wiring the already-built
  ZATCA/Hijri services — all real gaps, none blocking the phases above.
- **Open questions, not guessed into a phase**: Real Estate/Site-Land Management, and Joint Venture/
  Consortium accounting (which needs Multi-Company to exist first) — both need a direct answer from the user
  before scoping, per the audit's own recommendation.

## Phase 3 — Construction, Project Accounting & Accounts Receivable
- `Modules.Construction`: Customer Contracts (value, type — Lump Sum/Unit Price/Cost Plus, advance %,
  defects liability period), Bill of Quantities (BOQ) mapped onto WBS elements, Subcontracts (retention %,
  mobilization advance, back-charges — a distinct document type from a standard PO), Site Progress/
  Measurement against BOQ lines, Variation Orders with their own approval chain, Retention (withheld %,
  released after defects-liability period, on both the AR and AP side)
- `Modules.ProjectManagement`: scheduling, resource/equipment allocation, cost roll-up into Finance
- **Finance depth, elevated into this phase 2026-07-16** (see checkpoint above for why these aren't
  deferred): Accounts Receivable / Customer Invoice + Aging — `Modules.Finance` is AP-only today; **IPC
  (Interim Payment Certificate)** as its own document type distinct from a plain AR Invoice (submitted amount
  vs. consultant-certified amount is a real distinction, not extra fields on an invoice); Back Charges/
  Material Recovery (a negative line item inside IPC Management, linking a cost event to a subcontract's next
  payment cycle); Business Partner master-data fields still missing (§15 of `ARCHITECTURE-AUDIT.md`) —
  Payment Terms, default Payment Method, Bank/IBAN, Credit Limit, Reconciliation Account, Withholding Tax
  flag, one-time-vendor flag; Fiscal Year/Period management (open/close/lock — a Journal Entry can currently
  post to any date forever); real Budget Check (`PassThroughBudgetCheckService` is a literal pass-through
  stub today despite already being called in the PO/PR flow); amount-conditioned approval matrices (prove
  once on Purchase Order via the existing `AttributeConstraints` primitive, then apply to AP Invoice/Journal
  Entry/Purchase Requisition the same way); a generic **Statement pattern** (Opening Balance → Transactions →
  Running Balance → Aging, parameterized per party type) designed now so it isn't retrofitted per module
  later — this phase rolls it out for Customer and Supplier first.
- **Exit criteria**: a project can be set up with a BOQ, subcontracted, progress-measured on site, and
  variation orders flow through approval into cost; an Interim Payment Certificate can be submitted,
  certified, and billed to the customer through a real AR posting with aging visible on that customer's
  Statement.

## Checkpoint — Treasury, Fixed Assets & Equipment (added 2026-07-16, after Phase 3)
Depends on Phase 3's real WBS cost postings existing. See `HadionERP_Missing_Features_Audit_V1.1.md` §7-8 for
full detail; summarized here for sequencing only.

- **Treasury & Cash Management**: Petty Cash custodians (site offices run on physical float, not just bank
  transfers) with a real advance → spend → settle → replenish cycle and Cash Counts; Bank Reconciliation
  against `BankAccount`/`Payment` (currently referenced only in a mockup, no domain object exists); Check
  Management (Issued → Cleared → Bounced/Cancelled, common in KSA/GCC construction payment terms); Letter of
  Credit and Letter of Guarantee tracking (import-heavy procurement and performance/advance/retention bonds).
- **Fixed Assets — not a simple asset register**: must carry a **time-dependent** WBS/Project cost-object
  assignment (an effective-dated row, not a static foreign key) plus distribution rules for an asset serving
  multiple projects at once, modeled on SAP FI-AA's Time-Dependent Data tab / Dynamics's FA Allocation Key —
  because equipment moving between projects mid-month is normal monthly operation for this business, not an
  edge case. Building a naive static-FK version now would need a breaking rework later; build the
  time-dependent version from the first slice.
- **Equipment & Fleet** (distinct from Fixed Assets' book value and Plant Maintenance's service schedule):
  fuel/operator cost, inter-project transfers, rental-in/out, and — the part with no home today — usage/
  internal-hire cost allocation to whichever project actually used the equipment that period, so a project's
  Actual Cost isn't understated by company-owned equipment usage.
- **Plant/Equipment Maintenance**: service schedules, breakdown/repair tickets — pairs naturally with Fixed
  Assets since both reference the same equipment master.
- **Inventory/Warehouse Management**: on-hand quantity, goods movements, site material reservation/issue —
  `Item.cs`'s own doc comment already presupposes this exists; GRN today never increments any stock balance.
- **Cost Codes** distinct from the generic `CostCenter` — most construction ERPs cross a dedicated cost-code
  structure (e.g. CSI MasterFormat) with WBS/Project for real job costing.
- **WIP/Percentage-of-Completion revenue recognition** — depends on real WBS cost postings (this checkpoint
  and Phase 3) existing first; referenced conceptually in doc 07 but has zero implementation today.
- **Multi-Company/Legal Entity structure** — no `Company` entity exists; `"C001"` is hardcoded in ~15 places.
  Needed here (rather than left to Phase 4+) because Joint Venture/Consortium accounting — a real construction
  pattern for large contracts — depends on it existing first.

## Phase 4 — HR & Payroll
- `Modules.HR`: employee lifecycle, org structure, leave, Saudization/Nitaqat tracking; **Document Expiry
  Monitoring** (Passport/Visa/Iqama/Medical Insurance/Certifications) as a first-class, proactively-alerting
  capability feeding the same alerts surface Finance uses, not a file attachment with a date field — an
  expired Iqama can legally stop an employee working a site; **Exit Clearance/Asset Return** tied to Final
  Settlement below, spanning HR/Equipment/Payroll
- `Modules.Payroll`: payroll run, WPS file export, GOSI integration, EOSB calculation, GL posting of payroll
- **Employee Financial Management, elevated into this phase 2026-07-16** (see checkpoint above): Salary
  Advances/Employee Loans (common across construction blue-collar workforces, own approval + repayment
  schedule deducted from future runs); Business Trip Advances/Expense Claims/Reimbursements; a generic
  Deductions engine (loan repayment, advance recovery, disciplinary, GOSI); Final Settlement/End of Service
  Benefits (legally mandated in KSA, not optional); Vacation Liability monthly accrual (a real GL liability,
  not just an HR leave-balance number); Air Ticket Eligibility/Ticket Encashment (standard GCC expatriate
  benefit — this workforce is majority expatriate); a per-employee Statement reusing Phase 3's generic
  Statement pattern
- ZATCA e-invoicing Phase 2 (XML/UBL clearance integration) generalized across AR
- **Exit criteria**: a full payroll cycle runs (including advances/deductions/EOS where applicable), generates
  a compliant WPS file, and posts to GL.

## Phase 5 — Reporting, Analytics & Mobile
- `Modules.Reporting`: statutory reports (VAT return, ZATCA audit file, GOSI/MHRSD reports), management
  reports (project profitability, cash flow, WIP)
- BI embed (Metabase/Power BI) against read replicas
- Native-feeling mobile experience for site-based approvals and progress capture
- **Exit criteria**: finance/executive users have self-service dashboards; site staff can approve/record
  from mobile.

## Phase 6 — Extensibility Ecosystem & Advanced Capabilities
- Formalize the extension marketplace/catalog (doc 05 §3.4)
- AI-assisted features (invoice OCR/matching, forecasting, anomaly detection in audit logs) as opt-in
  extensions built on the same extension points as any third-party extension — the platform doesn't get a
  private backdoor.

## Team Shape (indicative, scales with phase)
- 1 Chief/Platform Architect (continuity across phases — this role should not rotate)
- Phase 0: 2–3 senior platform engineers (no module engineers needed yet)
- Phase 1+: 1 tech lead + 2–4 engineers per active module, a dedicated KSA localization/compliance SME
  attached from Phase 1 onward, and a dedicated QA/architecture-conformance engineer maintaining the
  architecture tests from doc 05 §1.

## Success Metrics per Phase
- **Phase 0**: 100% of scaffolded demo BO behaviors work with zero module-specific code.
- **Phase 1–4**: each module ships with zero direct cross-module Domain/Infrastructure references (verified
  by architecture tests, doc 01 §3.2) and zero hard-coded business rules that should be configuration (spot-
  audited).
- **All phases**: no financial or HR/payroll record is ever hard-deleted in production (verified by DB
  trigger + audit log completeness check).
