# Modules.ProjectManagement

Phase 3's opening module: the WBS/Networks foundation per SAP Project System's own architecture — a Project
Definition containing a hierarchical Work Breakdown Structure, each WBS element a real Controlling object
(Planning/Account-Assignment/Billing flags), built and verified as the cost/schedule backbone that
`Modules.Construction` (the commercial layer) and `Modules.Finance` (Results Analysis/Settlement) will
reference later. Also intended to own Networks/Activities/Milestones (time sequence, dependencies, resource
& equipment allocation) per `docs/architecture/07-project-accounting-and-financial-architecture.md` §4 — not
yet built, see Deferred below. This README tracks what's actually built today.

## What's built (Phase 3, slice 1: Project + WBS Element)

- **Domain**: `Project : BusinessObject` — `ProjectName`/`ProjectNameArabic?`, an optional `CustomerId`
  (a Business Partner holding the "Client" role, validated via `IBusinessPartnerLookup` at creation — the
  same cross-module lookup pattern every other module uses, never a direct reference into
  `Modules.MasterData.Domain`), and `StartDate?`/`EndDate?`. Owns a child collection of `WbsElement`
  (Id/Code/Name/NameArabic?/ParentWbsElementId?/IsPlanningElement/IsAccountAssignmentElement/
  IsBillingElement) — `Project.AddWbsElement` validates Draft-only, a unique code within the project, and
  that any given parent element already belongs to this project (no cross-project or forward parent
  references). Stops at Approved like every master-data-ish BO so far (no Post/Reverse) — a Project
  Definition is a planning/organizational object, not a financial document; actual cost postings against its
  WBS elements are Finance's concern in a later slice.
- **tempId hierarchy resolution for one-shot creation**: `CreateWbsElementRequest(int TempId, int?
  ParentTempId, ...)` lets the frontend submit an entire multi-level hierarchy in one `CreateAsync` call
  before any element has a real database id — `ProjectService.CreateAsync` resolves parent-before-child
  ordering into real `Guid`s (throws `ArgumentException` if a child's `ParentTempId` appears before its
  parent in the request list, keeping the resolution a single forward pass rather than a general
  graph-sort). At least one WBS element is required — a Project with zero WBS elements has nothing to plan,
  assign cost to, or bill against.
- **`ProjectSecurity`/`ProjectWorkflow`**: same one-step Any-quorum Maintainer/Approver shape as every
  procurement document's first workflow cut (`ProjectManagement.Project.Maintain`/`.Approve` Duty/Role keys,
  one SoD conflict rule between them).
- **Infrastructure**: `ProjectManagementDbContext` — this module's own **"projectmanagement" Postgres
  schema**, same physical database as every other module (schema-per-module is the boundary, not
  database-per-module). Owns its own `NumberRangeCounterEntity`/`EfCoreNumberRangeService`/
  `EfWorkflowInstanceRepository`, near-duplicates of every other module's own copies for the same "a module
  cannot depend on another module's Infrastructure directly" reason. Number range key `PM-PROJECT`, format
  `PM-PRJ-2026-000001`.
- **Api**: `ProjectsController` at `api/v1/projectmanagement/projects` — Create/Get/List +
  `submit`/`approve`/`reject`.
- **Frontend**: `ProjectsPage.tsx` — list/create/details using the `SplitView` pattern established on
  `PurchaseOrdersPage.tsx`/`BusinessPartnersPage.tsx`. Create form has header fields (Project Name +
  Arabic, Customer, Start/End Date) plus a WBS create grid (Code/Name/Parent-row-picker/three checkboxes,
  Add/Remove Line) — the grid resolves each surviving row's original index to a tempId via a `Map` so
  mid-entry row removal doesn't corrupt parent references (see `ProjectsPage.tsx`'s `survivors`/
  `tempIdByOriginalIndex` construction). Details view has two FastTabs: General and WBS Elements (a
  read-only table showing each element's resolved parent by code+name, and its three flags). Own nav module
  ("Project Management"), not bolted onto Procurement.
- Verified end-to-end: 19 new unit tests (WBS validation incl. duplicate code / parent-not-in-project /
  Draft-only, customer role validation, tempId ordering incl. out-of-order-parent rejection, the full
  Draft→Submit→Approve/Reject lifecycle, SoD/security denial cases) + 3 new integration tests against real
  PostgreSQL (Project+WBS hierarchy round-trips identically including Arabic name and flags, cascade delete
  removes all WBS elements, RowVersion increments across real transitions). 22 tests pass in this module
  alone, zero regressions solution-wide (19 test projects). Live `curl` exercise: created a Project with a
  three-element hierarchy (one root + two children under it) in a single request, confirmed the tempId→Guid
  parent resolution was correct in the response, drove it through Submit→Approve. Live Playwright pass
  (screenshots, zero console errors) on `ProjectsPage.tsx`'s list, details (both FastTabs), and create form,
  in both English and Arabic — full RTL mirroring confirmed including the WBS table's parent column.

## Deferred (disclosed, not hidden)

- **Networks** (SAP Project System's activity/relationship layer — Activities, Relationships,
  scheduling/critical-path, Milestones, resource & equipment allocation) — the roadmap names this module
  "WBS/Networks foundation," but only the WBS half is built. Networks is a materially bigger scheduling
  concern deferred to its own future slice.
- No cost/budget data on WBS elements yet — `IsPlanningElement`/`IsAccountAssignmentElement`/
  `IsBillingElement` are structural flags only; nothing currently posts costs, budgets, or billing events
  against a WBS element. That's Finance's Results Analysis/Settlement depth (already disclosed as deferred
  Finance work in `Modules.Finance/README.md`) plus a future Controlling-style cost-collector concern.
- No WBS element status/lifecycle independent of the Project's own (real SAP allows individual WBS elements
  to be released/closed/blocked separately from the project header) — a WBS element here just exists once
  its parent Project is Approved, with no per-element state machine.
- `Modules.Construction` (the commercial layer referencing WBS — subcontracts, variation orders,
  progress billing) is not started; this slice is purely the cost/schedule backbone it will reference.
- No project template / copy-from-template creation — every project's WBS is entered from scratch.
- No WBS element reordering/renumbering after creation, no moving an element to a different parent.
- Real authentication: `ProjectsController` hardcodes `"system/ui"`/`"system/approver"`, same deferred item
  as every other module's controller.
