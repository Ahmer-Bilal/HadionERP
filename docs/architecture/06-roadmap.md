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

- **Phase 1 depth, the highest-priority data-model gap in the whole audit**: AP Payment Recording & Cash/Bank
  Management (§16) — there is currently no way anywhere in this system to record that an AP invoice was
  actually paid (`APInvoice.Post()` only ever posts Debit Expense/Credit Payable; nothing ever debits Payable/
  credits a bank account). Business Partner master data (§15 — no Payment Terms, Bank/IBAN, Credit Limit,
  Reconciliation Account, Withholding Tax) is the same real-world capability's master-data half and should
  land alongside it, not separately.
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

## Phase 3 — Construction & Project Management
- `Modules.Construction`: Contracts, BOQ/WBS, Subcontracts, Site Progress/Measurement, Variation Orders,
  Retention
- `Modules.ProjectManagement`: scheduling, resource/equipment allocation, cost roll-up into Finance
- **Exit criteria**: a project can be set up with a BOQ, subcontracted, progress-measured on site, and
  variation orders flow through approval into cost and (if applicable) revenue recognition in Finance.

## Phase 4 — HR & Payroll
- `Modules.HR`: employee lifecycle, org structure, leave, Saudization/Nitaqat tracking
- `Modules.Payroll`: payroll run, WPS file export, GOSI integration, EOSB calculation, GL posting of payroll
- ZATCA e-invoicing Phase 2 (XML/UBL clearance integration) generalized across AR
- **Exit criteria**: a full payroll cycle runs, generates a compliant WPS file, and posts to GL.

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
