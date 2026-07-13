# 04 — Database Strategy, API Architecture, Configuration Strategy

## 1. Database Strategy

### 1.1 Engine & multi-tenancy
- **PostgreSQL** (ADR-2) as the primary RDBMS. Multi-tenancy model: **schema-per-tenant** within a shared
  cluster — one tenant's data is fully isolated at the schema level (simpler backup/restore/residency-per-
  customer story than row-level-only multi-tenancy), while all tenants share the same application binaries
  and platform kernel tables (translations, module manifests).
- Each `Modules.X` owns its own Postgres **schema** (`finance.*`, `procurement.*`, `masterdata.*`, …) — this
  physically enforces the module boundary from doc 01 at the database level, not just in application code.
- Row Level Security policies (company/branch/project scoping, doc 03 §2.3) are enabled on every
  transactional table as defense-in-depth.

### 1.2 Master data vs transactional data
- Physically separate schemas: `masterdata.*` tables are referenced by foreign key from every transactional
  schema; transactional schemas never duplicate master attributes (e.g. vendor name is never copied onto a
  PO row — only the vendor key, with a point-in-time snapshot captured only where legally required, e.g.
  invoice address at time of issue).

### 1.3 Standard table conventions
Every transactional table includes:

| Column | Purpose |
|---|---|
| `id` (uuid) | Surrogate key |
| `doc_number` | Human-facing number from a Number Range |
| `company_id`, `branch_id` | Row-level security scope |
| `status` | FSM state (doc 02 §1.1) |
| `row_version` | Optimistic concurrency token |
| `created_by`, `created_at`, `modified_by`, `modified_at` | Standard audit columns (in addition to the full audit log in `Platform.Audit`) |
| `extension_data` (jsonb) | Extension field storage (doc 05 §3) |

- **No hard deletes** on any table past Draft status (doc 02 §1.1) — enforced by a database trigger, not
  just application discipline, so a bug or an ad-hoc SQL script cannot silently destroy financial history.

### 1.4 Reporting & scale
- Read replicas serve `Modules.Reporting` and BI (Metabase/Power BI) so heavy analytical queries never
  contend with OLTP traffic.
- Partitioning (by fiscal year/company) for high-volume tables (GL line items, payroll lines) once volume
  warrants it — not a day-one requirement, but the schema design (immutable posted rows, date-based
  partition key candidates) is chosen so it can be added without a redesign.
- Archiving: closed fiscal-year data can be moved to a cold/archive tablespace or separate archive database,
  governed by the retention policy (doc 03 §5), with the Object Page's "Related/History" facets still able
  to resolve archived records transparently.

### 1.5 Backup & DR
- Point-in-time recovery, RPO ≤ 15 min / RTO ≤ 4 hr targets for production (tune per customer SLA),
  cross-AZ replication within the KSA region to satisfy residency while still meeting availability targets.

## 2. API Architecture

### 2.1 Style & conventions
- **REST, resource-oriented, OData-inspired query conventions**: `$filter`, `$select`, `$expand`, `$orderby`,
  `$top`/`$skip` supported via `Platform.Api` middleware on every List Report endpoint — consistent,
  predictable, and matches what integrators expect coming from SAP/Dynamics backgrounds.
- **Contract-first**: every module publishes an OpenAPI 3.1 spec generated from its Application-layer
  contracts; the spec *is* the source of truth for the `Contracts` package and for external integrators.
- **Versioning**: URL-segment major version (`/api/v1/...`) + header-based minor negotiation; breaking
  changes require a new major version and a documented deprecation window — never a silent breaking change
  to a shipped contract.

### 2.2 Composition
- **API Gateway / BFF** (`Gateway.Api`) terminates auth, applies rate limiting, and composes per-app
  aggregated calls (e.g. an Object Page that needs BO data + related objects + workflow status in one round
  trip) — individual modules still expose clean, independent REST APIs behind it for direct
  integration/automation use cases.
- **Batch/bulk operations** (`$batch`-style) supported for high-volume operations (e.g. bulk GRN posting,
  bulk payroll line import) so integrators aren't forced into N single-record calls.
- **Idempotency keys** required on all POST/state-transition endpoints — critical for financial documents
  where a retried network call must never double-post.

### 2.3 Events & integration surface
- **Webhooks/event subscriptions**: external systems (banks, ZATCA, government portals, site-based mobile
  apps) can subscribe to the same integration events defined in doc 03 §3, via a managed subscription API,
  rather than polling.
- **Integration adapters** for ZATCA, GOSI, WPS, SADAD/bank file formats live behind `Platform.Integration`
  contracts — each is swappable/versionable independently as government specs change (ZATCA Phase 2 rollout
  waves, for example) without touching business module code.

## 3. Configuration Strategy

### 3.1 Multi-level configuration
Configuration resolves through a strict override hierarchy, resolved and cached by
`Platform.Configuration`:

```
System defaults → Tenant → Company → Branch → User
```

Every configurable item declares which levels it may be overridden at (e.g. approval matrices at
Company/Branch; UI density preference at User only) — not everything is overridable everywhere.

### 3.2 What is configuration vs extension vs customization

| Change type | Mechanism | Example |
|---|---|---|
| **Configuration** | Data change via admin UI, no code, no deploy | Number range format, approval thresholds, tax codes, print layout selection |
| **Extension** | Attaches to a published extension point (doc 05 §3), packaged/versioned separately, upgrade-safe | Custom field on PO, extra validation on Subcontract approval, a new report |
| **Customization** | Core module code modified directly | **Disallowed** in this architecture — anything needing this becomes a documented extension point instead ("clean core" principle) |

### 3.3 Business rule engine
- Rules that are naturally declarative (validation conditions, posting rules, tax determination, approval
  routing conditions) are expressed in a **rules table** interpreted at runtime by
  `Platform.Configuration.Rules`, not compiled into module code — this is what lets a functional consultant
  change "PO requires second approval above 100,000 SAR" without a code release.

### 3.4 Feature flags & environment promotion
- Feature flags gate incomplete/opt-in functionality per tenant, independent of deployment — allows trunk-
  based development without exposing half-finished features.
- Configuration is packaged into versioned, exportable/importable **configuration packages** (à la Dynamics'
  data packages / SAP transport requests) to promote Dev → Test → UAT → Prod deterministically, with a diff/
  review step before applying to Prod.
