# 01 — Architecture Foundation

## 1. Layered Architecture

Every module (business or platform) is internally structured in the same four layers, dependencies point
**downward only**:

```
┌─────────────────────────────────────────────────────────┐
│ 1. Presentation Layer        (Apps/*)                    │  React apps, List Report & Object Page renderers
├─────────────────────────────────────────────────────────┤
│ 2. API Layer                  (*.Api)                     │  Controllers, DTOs, OpenAPI contracts, BFF composition
├─────────────────────────────────────────────────────────┤
│ 3. Application Layer          (*.Application)              │  Use cases / command & query handlers, workflow triggers,
│                                                             │  validation, orchestration — NO business rules here
├─────────────────────────────────────────────────────────┤
│ 4. Domain Layer               (*.Domain)                   │  Business Objects, state machines, domain events,
│                                                             │  business rules — the only layer allowed to enforce rules
├─────────────────────────────────────────────────────────┤
│ 5. Infrastructure Layer       (*.Infrastructure)            │  EF Core repositories, external adapters (ZATCA, GOSI,
│                                                             │  banks), message bus implementation
└─────────────────────────────────────────────────────────┘
             ▲
             │ all layers depend on
┌─────────────────────────────────────────────────────────┐
│ 0. Platform Kernel            (src/Platform/*)             │  Security, Localization, Workflow, Events, Audit,
│                                                             │  Configuration, UI Design System, Extensibility SDK
└─────────────────────────────────────────────────────────┘
```

Rules:
- Domain layer has **zero** dependency on Infrastructure, API, or any other module's Domain layer — it only
  depends on `Platform.Core` abstractions (interfaces).
- Application layer depends on Domain (same module) + Platform services, and may depend on **other modules'
  published contracts only** (see §3, Module Boundaries) — never on another module's Domain/Infrastructure.
- Infrastructure implements the interfaces Domain/Application define (Dependency Inversion) — swapping
  Postgres for another store, or RabbitMQ for Kafka, never touches Domain code.
- This mirrors Clean/Hexagonal Architecture; it is what lets SAP/Dynamics keep a 20-year-old core alive under
  constantly changing UI and infra.

## 2. Full Repository Folder Structure

```
erp-platform/
├── ARCHITECTURE.md
├── CLAUDE.md
├── docs/
│   └── architecture/
│
├── src/
│   ├── Platform/
│   │   ├── Platform.Core/                # BO base classes, lifecycle FSM, number ranges, extension fields
│   │   ├── Platform.Security/            # AuthN/AuthZ, RBAC/ABAC, row/field-level security, SoD engine
│   │   ├── Platform.Localization/        # i18n/l10n engine, RTL, calendars, ZATCA/tax localization
│   │   ├── Platform.Workflow/            # workflow engine wrapper, approval matrix runtime
│   │   ├── Platform.Events/              # domain/integration event bus abstraction, outbox
│   │   ├── Platform.Audit/               # immutable audit log, change tracking
│   │   ├── Platform.Reporting/           # report/print engine, layout designer runtime
│   │   ├── Platform.Configuration/       # multi-level config store, feature flags, business rule engine
│   │   ├── Platform.Api/                 # shared API conventions: base controllers, OData-query middleware
│   │   ├── Platform.UI/                  # design system: tokens, components, List Report & Object Page templates
│   │   ├── Platform.Extensibility/       # extension point registry, plugin manifest/runtime, sandboxing
│   │   └── Platform.Integration/         # adapter contracts: ZATCA, GOSI, WPS, SADAD, banks, e-signature
│   │
│   ├── Modules/
│   │   ├── Modules.MasterData/           # Business Partners, CoA, Items/Materials, Cost Centers, Projects (master), Employees (master), UoM, Tax codes
│   │   │   ├── Domain/
│   │   │   ├── Application/
│   │   │   ├── Infrastructure/
│   │   │   └── Api/
│   │   ├── Modules.Finance/              # Universal Journal (GL/AP/AR/Assets), Document Splitting, Parallel Ledgers, Controlling, Results Analysis & Settlement to CO-PA — see doc 07
│   │   ├── Modules.Procurement/          # Purchase Requisition, RFQ/Tender, PO, GRN, Vendor Invoice matching
│   │   ├── Modules.ProjectManagement/    # Project Definition, WBS elements (Controlling backbone), Networks/Activities/Milestones, resource/equipment allocation — see doc 07 §4
│   │   ├── Modules.Construction/         # Contracts, BOQ (mapped to WBS), Subcontracts, Site Progress, Variation Orders, Retention — commercial layer over ProjectManagement's WBS
│   │   ├── Modules.HR/                   # Employee lifecycle, org structure, Saudization/Nitaqat tracking, leave
│   │   ├── Modules.Payroll/              # Payroll runs, WPS export, GOSI integration, EOSB
│   │   └── Modules.Reporting/            # cross-module statutory & management reports (built on Platform.Reporting)
│   │
│   ├── Apps/
│   │   ├── Apps.Shell/                   # app shell: shell bar, launchpad/home, intent router, theming
│   │   ├── Apps.Finance/
│   │   ├── Apps.Procurement/
│   │   ├── Apps.Construction/
│   │   ├── Apps.HR/
│   │   └── Apps.Payroll/
│   │
│   └── Gateway/
│       └── Gateway.Api/                  # API gateway / BFF: auth termination, routing, rate limiting, aggregation
│
├── tests/
│   ├── UnitTests/         (mirrors src/ per module)
│   ├── IntegrationTests/
│   └── E2ETests/
│
├── infra/
│   ├── terraform/
│   ├── kubernetes/
│   └── pipelines/
│
└── tools/
    ├── bo-scaffold/        # CLI: generate a new Business Object (Domain+App+Api+UI) from a metadata spec
    └── object-page-gen/    # CLI: generate a List Report + Object Page from BO metadata
```

Each `Modules.X` folder is a **project group**, not a single project — internally it repeats the same
Domain/Application/Infrastructure/Api sub-layers as the platform kernel, so any engineer who has learned one
module's shape already knows all of them. This uniformity is deliberate and is what the Coding Standards doc
enforces.

## 3. Module Boundaries & Dependency Rules

### 3.1 Module list and single responsibility

> **Correction note (2026-07-13)**: Finance and the Construction/ProjectManagement split below were
> originally too shallow (Finance as siloed GL/AP/AR/Assets; Construction as a flat list of documents with
> no shared cost/revenue backbone). They are corrected here to match the SAP-referenced model in
> [doc 07](07-project-accounting-and-financial-architecture.md) — read that doc for the *why*.

| Module | Owns | Does NOT own |
|---|---|---|
| **MasterData** | Business Partners (customers/vendors/subcontractors), Chart of Accounts, Items/Materials, UoM, Cost Centers, Employee master, Tax codes, Number range definitions | Any transaction documents, any balances, WBS structures |
| **Finance** | One Universal Journal (line-item store) covering GL, AP, AR, Fixed Assets, Cash/Bank; Document Splitting (real-time dimensional balancing); Parallel Ledgers (IFRS + Saudi statutory basis); Controlling objects (Cost Centers, Internal Orders, Profitability Segments); Budget control; **Results Analysis (POC revenue recognition) and Settlement to CO-PA**; Financial statements | Vendor/customer master, WBS/Network structures (owned by ProjectManagement), PO/GRN documents |
| **Procurement** | PR → RFQ → PO → GRN → 3-way match, cost/commitment posting against Controlling objects (Cost Center/WBS element) | Vendor master, GL posting rules (calls Finance via contract) |
| **ProjectManagement** | Project Definition, **WBS elements** (the cost/revenue/Controlling backbone shared by every project-based module — planning/account-assignment/billing flags), **Networks/Activities/Milestones** (scheduling, dependencies), resource/equipment allocation | Commercial contract/BOQ documents (owned by Construction), revenue recognition (owned by Finance §Results Analysis) |
| **Construction** | The construction-industry commercial layer referencing WBS elements: Customer Contracts, BOQ (mapped to WBS elements), Subcontracts (procurement documents assigned to WBS elements), Site progress/measurement, Variation Orders (adjust WBS budget), Retention terms | Payroll, GL posting, WBS/Network structures themselves (owned by ProjectManagement) |
| **HR** | Employee lifecycle, org structure, leave, Saudization/Nitaqat | Payroll calculation, GL posting |
| **Payroll** | Payroll run, WPS file generation, GOSI submission, EOSB, labor cost posting against Controlling objects (Cost Center/WBS element) | Employee master data (owned by HR/MasterData) |
| **Reporting** | Cross-module statutory & management reports | No transactional ownership |

Note the reversed dependency vs. the original draft: **ProjectManagement now owns WBS elements** as the
shared Controlling/cost backbone, and **Construction depends on ProjectManagement** (it references WBS
elements), not the other way around — Construction is the industry-specific commercial skin over a
generic project-costing core, matching how SAP separates Project System (generic) from
industry-specific solutions built on top of it.

### 3.2 Dependency direction rules (enforced by architecture tests, not convention)

1. `Modules.*` may depend on `Platform.*` freely.
2. `Modules.*` may depend on **another module's published `Contracts` package only** (a thin
   interfaces+DTOs+events assembly each module exposes, e.g. `Modules.Finance.Contracts`) — never on another
   module's `Domain`, `Infrastructure`, or `Application` internals.
3. Cross-module communication is either:
   - a **synchronous contract call** through the other module's published Application-layer interface
     (e.g. Procurement asks Finance's `IBudgetCheckService` before releasing a PO), or
   - an **asynchronous domain event** through `Platform.Events` (e.g. `PurchaseOrderApprovedEvent` triggers a
     budget commitment in Finance without Procurement knowing Finance exists).
4. No circular module dependencies. The allowed high-level graph:

```
MasterData  ←──────────────┐
   ▲                        │
   │                        │
Finance ← Procurement ← ProjectManagement ← Construction
   ▲              (WBS/Networks,       (commercial layer:
   │               Controlling         Contracts/BOQ/
   │               backbone)           Subcontracts/VOs)
Payroll ← HR ←──────────────────────────────┘
   ▲
Reporting (reads from all, owned by none)
```

Finance additionally exposes a `Results Analysis / Settlement` contract that ProjectManagement's WBS
elements are read by (Finance pulls actual/planned cost per WBS element each period-close; this is a
Finance-owned batch process, not a call Construction/ProjectManagement initiate) — see doc 07 §5.

5. Every module ships a `module.manifest.json` declaring: module id, version, exposed contracts, consumed
   contracts, exposed extension points, database schema name. This manifest is what
   `Platform.Extensibility` and the deployment pipeline validate against at build/deploy time — a module that
   violates its declared dependencies fails CI.

### 3.3 Why this matters

This is the exact discipline that keeps SAP's LoB (Line of Business) packages independently upgradable and
lets Dynamics 365 ship Finance and SCM as separately licensable/deployable modules on one shared platform. A
new module (say, Fleet/Equipment Management for construction) can be added later without touching existing
module internals — it only needs to consume published contracts and register with the platform kernel.
