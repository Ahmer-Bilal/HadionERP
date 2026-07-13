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
- **Exit criteria**: a company can maintain its chart of accounts and vendors, and post/reverse a GL journal
  and an AP invoice end-to-end with full audit trail.

## Phase 2 — Procurement
- `Modules.Procurement`: PR → RFQ → PO → GRN → 3-way match against AP
- Budget-check integration event flow with Finance
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
