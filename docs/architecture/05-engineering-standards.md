# 05 — Coding Standards, Naming Conventions, Extension/Plugin Model

## 1. Coding Standards

- **Language/style**: C# (backend) follows the standard .NET/Microsoft style guide + `.editorconfig`
  enforced in CI; TypeScript/React (frontend) follows a single shared ESLint/Prettier config in
  `Platform.UI`. One style, enforced by tooling, not code review debate.
- **SOLID enforcement points**: Domain layer classes are sealed by default (open only where an extension
  point is explicitly declared — doc §3); Application layer handlers depend on interfaces, never concrete
  Infrastructure classes; architecture unit tests (e.g. NetArchTest) fail the build if a Domain project
  references an Infrastructure or another module's internals (enforces doc 01 §3.2 automatically).
- **Testing pyramid**: unit tests over Domain/Application logic (majority of tests, fast, no DB), integration
  tests over Infrastructure (real Postgres via Testcontainers), a thin layer of end-to-end tests over
  critical business flows (PO approval, invoice posting, payroll run) through the actual API + UI. Coverage
  gates apply to Domain/Application layers, not to generated Object Page boilerplate.
- **Code review gates**: no direct commits to main; PRs require passing architecture tests, unit tests,
  static analysis (SonarQube/Roslyn analyzers), and a security scan (dependency + SAST) before merge.
- **Documentation-as-code**: BO metadata (used to generate Object Pages) doubles as living documentation of
  what fields/behaviors exist — a separate hand-maintained field dictionary is not maintained in parallel.

## 2. Naming Conventions

Consistency here is what makes the codebase navigable by convention alone, the same way "you always know
where to find the GL posting logic in SAP" is a learned skill that should transfer instantly between modules
here.

| Element | Convention | Example |
|---|---|---|
| Module (folder/assembly) | `Modules.{ModuleName}` PascalCase, singular business area | `Modules.Procurement` |
| Business Object class | PascalCase noun, no "Model"/"Entity" suffix | `PurchaseOrder`, `SubcontractVariationOrder` |
| BO number/doc prefix | `{ModuleAbbrev}-{DocAbbrev}-{Year}-{Seq}` | `PROC-PO-2026-000123` |
| Database schema | lower_snake_case, matches module | `procurement`, `masterdata` |
| Database table | lower_snake_case, plural | `purchase_orders`, `purchase_order_lines` |
| API route | kebab-case, plural resource | `/api/v1/procurement/purchase-orders/{id}` |
| Domain event | `{BO}{PastTenseTransition}Event` | `PurchaseOrderApprovedEvent` |
| Integration event contract | `{Module}.{Event}.v{N}` | `Procurement.PurchaseOrderApproved.v1` |
| Permission/privilege | `{Module}.{BO}.{Action}` | `Procurement.PurchaseOrder.Approve` |
| Extension point | `{BO}.{HookName}` | `PurchaseOrder.OnBeforeSubmit` |
| React component (Platform.UI) | PascalCase, prefixed `ObjectPage`/`ListReport` for templates | `ObjectPageHeader`, `ListReportFilterBar` |
| Config key | `{module}.{area}.{setting}` lower dot-case | `procurement.po.requireSecondApprovalAbove` |

## 3. Extension/Plugin Model

The core promise to keep the platform maintainable for years is: **customer- or site-specific needs are met
by extensions, never by forking core module code** ("clean core" — the same philosophy behind SAP BTP
side-by-side extensibility and Dynamics 365's extension packages).

### 3.1 Extension points
Every module explicitly declares, in its manifest, the points where behavior can be extended:

- **Field extensions**: add typed custom fields to a BO's `extension_data` (doc 04 §1.3) — surfaced
  automatically on the Object Page's "General" facet without UI code, positioned via metadata.
- **Business logic hooks**: `OnBefore{Transition}` / `OnAfter{Transition}` hooks per BO transition (e.g.
  `PurchaseOrder.OnBeforeSubmit`) where an extension can add validation or side effects without modifying
  core Domain code — hooks run in an isolated scope and cannot themselves block the platform's own
  guaranteed invariants (e.g. cannot skip the audit log).
- **UI extension slots**: named slots on the Object Page/List Report (e.g. an extra tab, an extra action
  button, an extra column) that an extension can register into, using `Platform.UI` components — so
  extended UIs stay visually and behaviorally consistent with the rest of the platform.
- **Custom Business Objects**: a net-new BO type (e.g. a customer-specific "Site Safety Inspection" object)
  can be defined entirely through configuration + a thin extension package, inheriting the full BO base
  model (lifecycle, Object Page, audit, workflow) for free.

### 3.2 Packaging & versioning
- Extensions are packaged as independently versioned NuGet/npm packages with a manifest declaring: target
  module + minimum/maximum core version, declared extension points used, and their own exposed
  contracts/events if any.
- Core version upgrades run an automated **extension compatibility check** against installed extensions'
  manifests before deployment — analogous to SAP's "clean core" upgrade compatibility checks — so a platform
  upgrade cannot silently break a site's customizations.

### 3.3 Isolation
- Extension code executes in a constrained scope (own DI container child scope, no direct DB access outside
  its own extension schema/tables, no ability to bypass Security/Audit/Workflow platform services) — an
  extension can add behavior but cannot weaken the platform's guarantees.

### 3.4 Marketplace (future)
- Longer-term, extensions with no site-specific secrets can be published to an internal marketplace/catalog
  or bundled as an official "KSA Construction Extensions" pack — deferred to the Roadmap (doc 06), not a
  Phase 0-1 requirement.
