# HadionERP — Architecture Index

**Product:** HadionERP, by hAdisHere — created by aHmAr
**Category:** Enterprise ERP for Construction & Finance companies operating in the Kingdom of Saudi Arabia
**Status:** Architecture baseline v1.0 — no business modules implemented yet
**Owner:** Chief Architect
**Date:** 2026-07-13

## 1. Vision

HadionERP is a modular, multilingual (Arabic/English), KSA-compliant ERP platform that gives construction and finance
companies a single system of record for **Finance, Procurement, Construction/Projects, HR and Payroll** —
built the way SAP S/4HANA and Dynamics 365 F&O are built: a thin, stable **platform kernel** of cross-cutting
services (security, localization, workflow, events, audit, reporting) that every business module consumes,
and business modules that are thin, replaceable, and never reach around the kernel to talk to each other
directly.

The platform must still be recognizable, supportable, and extensible by a normal engineering team five years
from now — not just buildable today.

## 1.1 Reference Split (explicit, not incidental)

Two different enterprise systems are used as references for two different concerns, deliberately, not
interchangeably:

- **SAP S/4HANA** is the reference for **financial architecture and project accounting** — the Universal
  Journal, Document Splitting, Parallel Ledgers, Controlling objects, WBS/Network project structure, and
  Results Analysis/Settlement for percentage-of-completion revenue recognition. See
  [doc 07](docs/architecture/07-project-accounting-and-financial-architecture.md) for the full,
  source-checked detail. Project accounting is this platform's highest-priority capability, which is why
  this reference is followed at full rigor rather than simplified.
- **Dynamics 365 Finance & Operations** is the reference for **UI, navigation, and day-to-day usability** —
  Workspaces, the Navigation Pane (Modules → Areas → menu items), the merged List+Details record form,
  Action Pane, and FastTabs. See [doc 02](docs/architecture/02-business-object-model.md) §2–3 for the
  source-checked detail.

An earlier draft of this architecture blurred the two (it used SAP Fiori's "Object Page"/"List Report"
terms for what was meant to be the Dynamics-referenced UI layer, and modeled Finance/Construction as flat
siloed modules instead of SAP's Universal-Journal/WBS-Controlling model). Docs 01, 02, and 07 have been
corrected against primary sources (SAP Help Portal, SAP-PRESS, Microsoft Learn) — see the "Correction note"
callouts in those docs.

## 2. Non-negotiable Design Principles

These are the rules every future design decision is checked against:

1. **Single Responsibility per module.** A module owns exactly one bounded context (e.g. Procurement owns
   PR→RFQ→PO→GRN; it does not own vendor master data or GL posting logic).
2. **Master data is separated from transactions.** Business Partners, Chart of Accounts, Items, Cost
   Centers, Projects, Employees live in `Modules.MasterData`; every transactional module *references* them,
   never duplicates or owns them.
3. **Business rules are configuration, not code.** Approval matrices, number ranges, tax rules, posting
   rules, validation rules are data driven by the [Configuration Strategy](docs/architecture/04-data-and-api.md)
   and the [Workflow Engine](docs/architecture/03-platform-services.md), not `if` statements shipped in a
   release.
4. **The UI is templated, not hand-built per screen.** Every business object gets a **List Report** and an
   **Object Page** generated from its metadata, per the
   [Object Page Standard](docs/architecture/02-business-object-model.md). One-off screens require an
   architecture exception.
5. **Localization, Security, Workflow, Events, Audit and Reporting are platform services**, injected into
   every module from day one — they are never bolted on per module later.
6. **Every Business Object follows the same lifecycle and interaction contract** — see
   [Business Object Model](docs/architecture/02-business-object-model.md).
7. **Clean core.** Customer-specific logic lives in **extensions** that attach to defined extension points;
   core module code is never forked per customer/site.

## 3. Document Map

| Doc | Contents |
|---|---|
| [01 — Architecture Foundation](docs/architecture/01-architecture-foundation.md) | Layered architecture, full repo folder structure, module boundaries & dependency rules |
| [02 — Business Object Model & UI](docs/architecture/02-business-object-model.md) | BO base model, lifecycle state machine, Object Page standard, Navigation model, UI framework |
| [03 — Platform Services](docs/architecture/03-platform-services.md) | Localization, Security, Event architecture, Workflow engine, Audit framework |
| [04 — Data & API](docs/architecture/04-data-and-api.md) | Database strategy, API architecture, Configuration strategy |
| [05 — Engineering Standards](docs/architecture/05-engineering-standards.md) | Coding standards, naming conventions, Extension/plugin model |
| [06 — Roadmap](docs/architecture/06-roadmap.md) | Phased development roadmap, team shape, milestones |
| [07 — Project Accounting & Financial Architecture](docs/architecture/07-project-accounting-and-financial-architecture.md) | **SAP-referenced, source-checked**: Universal Journal, Document Splitting, Parallel Ledgers, WBS/Networks, Results Analysis & Settlement to CO-PA |

## 4. Key Technology Decisions (ADR summary)

| # | Decision | Choice | Rationale |
|---|---|---|---|
| ADR-1 | Backend platform | **.NET 8, C#, ASP.NET Core** (modular monolith, microservice-ready) | Strong typing for financial code, best-in-class EF Core + LINQ, mainstream in KSA enterprise/gov IT, easy to source-support long-term |
| ADR-2 | Primary database | **PostgreSQL** (schema-per-tenant) | Enterprise-grade, no per-core licensing, native row-level security, JSONB for extension fields |
| ADR-3 | Frontend | **React 18 + TypeScript**, in-house Design System (`Platform.UI`) implementing the Dynamics 365 F&O record-form pattern (Workspace / Navigation Pane / merged List+Details form / Action Pane / FastTabs) | Component reuse across templates; large hiring pool; matches the explicit Dynamics-for-UI reference (§1.1) |
| ADR-4 | API style | **REST, OData-inspired query conventions**, OpenAPI contract-first | Predictable, tool-friendly, matches SAP/Dynamics developer expectations |
| ADR-5 | Messaging/events | **RabbitMQ** on-prem, pluggable to Azure Service Bus/Kafka in cloud | Outbox-pattern reliability without forcing a cloud vendor on-prem-only clients |
| ADR-6 | Workflow engine | **Revised 2026-07-13**: hand-built approval-routing engine behind `IWorkflowEngine`, not Elsa Workflows | Actual Phase 0 need (condition-based multi-step routing, quorum, delegation, SLA) doesn't require a full BPMN engine; Elsa remains adoptable later behind the same interface if genuine BPMN complexity (cross-system gateways/sub-processes) arises — see `src/Platform/Platform.Workflow/README.md` |
| ADR-7 | Reporting | In-house report/print layout engine (QuestPDF-based) + Metabase/Power BI embed for analytics | Statutory documents (ZATCA invoices, WPS files) must be pixel/schema exact; analytics can use off-the-shelf BI |
| ADR-8 | Identity | **OpenID Connect / SAML2 SSO**, internal IdP built on Duende IdentityServer or Azure AD B2C | Enterprise SSO expectation, supports on-prem and cloud KSA customers |
| ADR-9 | Deployment | Containerized (Docker/Kubernetes), Infra-as-Code (Terraform), **KSA-region hosting** for data residency | PDPL / NDMO data residency requirements for financial and HR data |
| ADR-10 | Localization baseline | **Arabic (RTL) + English (LTR)** first-class from day one, Hijri+Gregorian dual calendar, ZATCA e-invoicing native | Legal requirement, not an afterthought |
| ADR-11 | Finance ledger design | **Single Universal-Journal-style line-item store** (not siloed GL/AP/AR/Asset tables), with rule-based Document Splitting | Eliminates GL-vs-Controlling reconciliation by construction, per SAP's ACDOCA model — see [doc 07](docs/architecture/07-project-accounting-and-financial-architecture.md) §1–2 |
| ADR-12 | Accounting principles | **Parallel Ledgers**: one posting, multiple simultaneous bases (IFRS leading ledger + Saudi statutory/Zakat non-leading ledger) | Avoids the common Excel-bridge workaround between IFRS and local statutory books — [doc 07](docs/architecture/07-project-accounting-and-financial-architecture.md) §3 |
| ADR-13 | Project cost/revenue backbone | **WBS elements as Controlling objects** (owned by ProjectManagement), with Results Analysis (cost-based POC, IFRS 15 cost-recognized fallback) + Settlement to CO-PA run by Finance | This is the platform's highest-priority capability; full SAP Project System rigor chosen over a simplified project-cost model — [doc 07](docs/architecture/07-project-accounting-and-financial-architecture.md) §4–5 |

These are defaults for the architecture, not commitments the user is locked into — each is revisited in its
detail doc with the trade-offs considered.

## 5. Top-Level Repository Layout

```
erp-platform/
├── ARCHITECTURE.md                 ← this file
├── CLAUDE.md                       ← project-level agent instructions (points back here)
├── docs/
│   └── architecture/                ← detailed architecture docs (see Document Map)
├── src/
│   ├── Platform/                   ← cross-cutting kernel (security, localization, workflow, events, audit, UI, config)
│   ├── Modules/                    ← business modules (MasterData, Finance, Procurement, ProjectManagement, Construction, HR, Payroll)
│   ├── Apps/                       ← per-app frontends (micro-frontend shell + module bundles)
│   └── Gateway/                    ← API gateway / Backend-for-Frontend
├── tests/                          ← unit / integration / e2e, mirrors src/ structure
├── infra/                          ← Terraform, Kubernetes manifests, CI/CD pipelines
└── tools/                          ← code generators (BO scaffolding, Object Page scaffolding)
```

Full, module-by-module detail is in
[01 — Architecture Foundation](docs/architecture/01-architecture-foundation.md).

## 6. Explicitly Out of Scope (for this baseline)

- No business modules, entities, or screens are implemented yet.
- No cloud provider is locked in — architecture is cloud-agnostic with a KSA-residency constraint.
- No commercial licensing/pricing model is defined here — this is a technical architecture only.

Next step once this baseline is approved: scaffold `Platform.Core` (Business Object base classes, lifecycle
state machine, number ranges) per Phase 0 of the [Roadmap](docs/architecture/06-roadmap.md).
