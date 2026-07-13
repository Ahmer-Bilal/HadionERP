# 03 — Platform Services: Localization, Security, Events, Workflow, Audit

These five services are injected into every module from day one via `Platform.*` — no module is allowed to
implement its own localization, auth, workflow, eventing, or audit logic. This section is the one most
specific to operating legally and credibly in Saudi Arabia.

## 1. Localization Architecture

### 1.1 Language & direction
- **Arabic (RTL) and English (LTR)** are both first-class from the first commit — not "English now,
  Arabic later." Every BO label, every UI string, every printed document ships with an AR and EN resource
  key from day one.
- Resource strings are namespaced per module (`Modules.Procurement.PurchaseOrder.Header`) in a translation
  store managed by `Platform.Localization`, with a fallback chain (tenant override → module default → EN).
- Users pick language per-session; the platform does not assume language == locale == country.

### 1.2 Calendars & formatting
- Dual calendar support: **Hijri (Umm al-Qura) and Gregorian**, with every date-bearing field able to
  display either based on user/company preference, always **stored in Gregorian/UTC** internally to avoid
  ambiguity — conversion is a presentation concern only.
- Number/currency formatting respects locale (Arabic-Indic vs Western digits, decimal/thousand separators),
  base currency SAR with multi-currency support for international vendors/subcontractors.

### 1.3 Saudi statutory/tax localization (KSA Localization Pack)
A dedicated `Modules.Localization.KSA` package (built on `Platform.Localization` + `Platform.Integration`)
provides, so it can later be swapped for a UAE/Bahrain/Qatar pack without touching core modules:

- **ZATCA e-invoicing**: Phase 1 (generation of compliant simplified/standard tax invoices with QR code) and
  Phase 2 (XML/UBL 2.1 invoice generation, digital signing, clearance/reporting API integration with ZATCA)
  as a pluggable adapter under `Platform.Integration`.
- **VAT** (currently 15%) as configured tax codes in MasterData, not hard-coded rates.
- **WPS (Wage Protection System)** payroll file export format for the Payroll module.
- **GOSI** integration (contribution calculation & submission) for Payroll/HR.
- **Saudization / Nitaqat** tracking as HR reporting, since it affects government relations and quota
  compliance, not just headcount.
- **Zakat/Income Tax (ZATCA)** support alongside VAT for corporate tax filings.

### 1.4 Translation management
- Non-developer-friendly translation workbench (config UI, not a code change) so functional consultants can
  maintain terminology per customer without redeploying.

## 2. Security Architecture

### 2.1 Authentication
- OpenID Connect / SAML2 SSO against an internal IdP (Duende IdentityServer or Azure AD B2C — deployment
  dependent), MFA enforced for financial-approval and admin roles, device/session management, configurable
  session timeout per role sensitivity.

### 2.2 Authorization — hybrid RBAC + ABAC
- **Roles → Duties → Privileges** hierarchy (same shape as Dynamics 365 security model): a **Privilege** is
  the smallest permission (e.g. "Approve PO up to 50,000 SAR"); **Duties** bundle privileges into a job
  function; **Roles** bundle duties and are assigned to users. This granularity is what supports
  **Segregation of Duties (SoD)** checks — e.g. the same user cannot hold both "Create Vendor" and "Approve
  Vendor Payment" duties unless an explicit, logged exception is granted.
- **Attribute-based** overlay for context that roles alone can't express: company/branch/project scoping,
  approval amount thresholds, cost-center ownership.

### 2.3 Row-level & field-level security
- **Row-level**: every transactional table carries `CompanyId`/`BranchId`/`ProjectId`; Postgres native Row
  Level Security policies enforce scoping at the database layer as a defense-in-depth backstop to
  application-layer checks.
- **Field-level**: sensitive fields (salary, bank IBAN, national ID/Iqama number) are maskable per role,
  enforced centrally by `Platform.Security` field policies, not per-screen logic.

### 2.4 Data residency & compliance
- KSA-hosted deployment target for tenants subject to **PDPL (Personal Data Protection Law)** and **NDMO/
  SDAIA** data classification rules — financial and HR/payroll data (especially national ID, Iqama, IBAN)
  is classified and residency-pinned at the infrastructure layer (ADR-9).
- Encryption at rest (database + blob) and in transit (TLS 1.2+ everywhere), centralized secrets management
  (Key Vault/HashiCorp Vault), no secrets in config files or source control.

### 2.5 API security
- OAuth2 client-credentials for system-to-system integration, scoped tokens per module/contract, mutual TLS
  option for bank/ZATCA integrations where required.

## 3. Event Architecture

- **Domain events** (in-process, same transaction, e.g. `PurchaseOrderApprovedEvent`) vs **integration
  events** (cross-module/cross-service, published on the bus, e.g. `InvoicePostedIntegrationEvent`) are
  explicitly distinct types — domain events never leave the module's process boundary; only integration
  events cross it.
- **Outbox pattern**: integration events are written to an outbox table in the same DB transaction as the
  business change, then relayed to the bus by a background publisher — guarantees no lost events even if the
  bus is briefly unavailable.
- **Event bus**: RabbitMQ by default (ADR-5), abstracted behind `Platform.Events.IEventBus` so a cloud
  deployment can swap in Azure Service Bus/Kafka without touching module code.
- **Event catalog & versioning**: every integration event is a versioned, schema-registered contract
  (`{Module}.{Event}.v{N}`) published in each module's `Contracts` package — consumers depend on the
  contract package, never on the publisher's internals.
- Typical cross-module reactions this enables: PO Approved → Finance creates budget commitment; GRN Posted →
  Procurement triggers 3-way match; Payroll Run Posted → Finance posts payroll journal; Employee Terminated →
  Payroll triggers EOSB calculation.

## 4. Workflow Engine

- Built on **Elsa Workflows** (ADR-6), wrapped by `Platform.Workflow` so business modules only ever see our
  own `IWorkflowService` — swapping the underlying engine later is an infrastructure change, not a module
  rewrite.
- **Configurable approval workflows**: condition-based routing (amount thresholds, cost center, project,
  vendor risk rating), multi-level sequential/parallel approval, delegation (out-of-office), SLA timers with
  escalation, reject-with-comment-and-resubmit loop.
- Workflows are attached to a BO type + transition (e.g. "PurchaseOrder.Submit triggers workflow
  `PO_Approval_v3`") via configuration (Platform.Configuration), not code — functional consultants maintain
  approval matrices without a release.
- Every workflow instance surfaces on the BO's Object Page "Approval History" facet automatically (§2 of
  doc 02) and emits domain events on every step for Audit and notification purposes.
- Notifications (in-app, email, SMS/WhatsApp for site-based construction approvers) are a workflow side
  effect, not workflow logic itself.

## 5. Audit Framework

- **Immutable audit log**: every create/update/status-transition/delete-attempt is recorded with who, what
  (field-level before/after), when, from where (IP/device), and why (linked workflow/comment if applicable).
  Audit records are append-only (no UPDATE/DELETE grants on the audit schema, enforced at the DB role level).
- **Tamper evidence**: audit records are hash-chained (each record's hash includes the previous record's
  hash) so undetected retroactive edits are computationally evident — important for financial/statutory
  defensibility, similar in spirit to SAP's change document + logging framework.
- **Financial posting audit**: posted financial documents are never edited or deleted, only reversed (§2,
  doc 02) — the audit trail plus the BO lifecycle together produce a defensible chain for external auditors
  and ZATCA.
- **Compliance exports**: audit data feeds statutory/compliance reporting (ZATCA audit file (SAF-T-like),
  GOSI/Ministry of Human Resources reporting) via `Modules.Reporting`, without giving those consumers direct
  DB access.
- **Retention policy**: configurable per data classification (financial records: statutory minimum, HR/
  payroll: per Saudi labor law retention requirements), enforced by an archiving job, not manual deletion.
