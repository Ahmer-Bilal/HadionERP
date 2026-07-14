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
- **Documented, not yet built**: `BusinessPartner.PartnerType` (Customer/Vendor/Both, a single enum) is
  planned to become `BusinessRoles` — a multi-select child collection, same pattern as
  `Addresses`/`Contacts` — since a real construction-industry partner commonly holds several roles at once
  (a company can be both a Supplier and a Subcontractor). See "Vendor Prequalification & Business Roles"
  under Phase 2 for the full design; captured here because it changes `BusinessPartner`'s own shape,
  decided 2026-07-14 to document now and implement once this slice is actually reached rather than rework
  Business Partner twice.
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
