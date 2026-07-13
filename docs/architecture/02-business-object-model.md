# 02 — Business Object Model, Record Form Standard, Navigation & UI

> **Correction note (2026-07-13)**: the first draft of this doc used "Object Page" and "List Report" for the
> UI standard. Those are **SAP Fiori/UI5** terms, not Dynamics 365 terms — an inconsistency, since the UI/
> navigation layer of this platform is explicitly modeled on **Dynamics 365 Finance & Operations**, while
> SAP is the reference for the financial/project data model (see
> [doc 07](07-project-accounting-and-financial-architecture.md)). §2–3 below are rewritten to use Dynamics
> 365's actual, documented UI vocabulary: **Workspace**, **Navigation Pane**, **List Page**, **Details
> (Master) form**, **Action Pane**, **FastTabs**. Sources: Microsoft Learn — see links in §2.

Every business entity in the platform — a Purchase Order, a Journal Entry, a Subcontract, a Payroll Run — is
a **Business Object (BO)**. BOs are the unit of design in this platform: one shape, generated tooling, one UI
pattern, one lifecycle. This is the single most important standard in the whole architecture — get this
right once and every module reuses it. The BO's structural shape (header/status/number range/lifecycle) is
itself SAP-influenced — SAP's document header + status + number range ("Nummernkreis") pattern is a
70-year-proven shape for enterprise documents — while its on-screen rendering follows Dynamics conventions.

## 1. Business Object Base Model

Every BO is composed of the same structural parts (`Platform.Core.BusinessObject<T>`):

| Part | Purpose |
|---|---|
| **Header** | Identity (BO number from a Number Range), key attributes, status, owning company/branch, dates |
| **Lines/Items** | 0..n child collections (e.g. PO Lines), each with their own status where relevant |
| **Status** | Current lifecycle state — see FSM below — always platform-typed, never a free-text field |
| **Number Range** | Document numbering rule (per company/branch/fiscal year), configured not coded |
| **Extension Fields** | Named, typed custom fields (JSONB-backed) added by extensions without schema migration of core tables |
| **Attachments** | Files linked via `Platform.Core.Attachment`, virus-scanned, stored in blob storage |
| **Comments/Notes** | Threaded internal notes, independent of workflow comments |
| **Change Log** | Field-level before/after history — surfaced from `Platform.Audit` |
| **Workflow Instance** | Link to the running (or completed) approval workflow instance, if the BO type has one configured |
| **Print/Output** | One or more registered report layouts (e.g. PO print, ZATCA invoice XML) |
| **Relations** | Typed links to other BOs (e.g. PO → PR it was created from, GRN → PO) — drives "Related Objects" navigation |

Every BO class implements `IBusinessObject`, giving it, for free, from the kernel:
- CRUD via a generic repository
- Optimistic concurrency (RowVersion)
- Soft lifecycle (no hard deletes for anything posted — see §2)
- Audit trail hook-in
- Workflow hook-in
- Extension field storage
- Standard REST endpoints (List Report + Object Page CRUD, per `Platform.Api` conventions)

### 1.1 Standard BO Lifecycle (Finite State Machine)

All BOs use the **same** state machine shape; not every BO uses every state, but no BO invents new state
names:

```
Draft ──submit──▶ Submitted ──▶ InApproval ──approve──▶ Approved ──post──▶ Posted/Active
  │                                  │                                         │
  │                               reject                                    reverse/cancel
  ▼                                  ▼                                         ▼
Deleted (draft only,             Rejected ──resubmit──▶ Draft            Cancelled / Reversed
 hard delete allowed)
```

Rules:
- **Draft** is the only state where hard delete is allowed. Everything past Draft is corrected by
  **reversal**, never edited-in-place and never deleted — this is what makes the Audit Framework
  trustworthy and is standard SAP/Dynamics financial discipline.
- **Posted/Active → Reversed** always creates a new BO instance (a reversal document) linked via
  Relations to the original; the original is marked `Reversed`, its financial/quantity effect is negated,
  never edited.
- Every transition is a **guarded command** (`ISubmit`, `IApprove`, `IPost`, `IReverse`) — the guard checks
  are business rules living in the Domain layer, and every transition emits a domain event
  (`{BOName}{Transition}Event`) automatically, which is what lets Workflow, Audit, and other modules react
  without coupling.

## 2. Record Form Standard (Dynamics 365 F&O pattern)

Every BO gets exactly one **List Page** and one **Details (Master) form**, generated from BO metadata by
`tools/object-page-gen` (tool name kept; output changed — renaming the CLI is a later cleanup, not a
functional change). Individually hand-built pages require a documented architecture exception.

Per Microsoft's own current guidance, the List Page and Details Master form are **merged into a single
scrolling form** (this became the recommended pattern from release 10.0.25 onward, replacing the older
"navigate to a separate details page" flow, precisely to cut the extra round trip between list and detail).
We adopt the merged pattern as the default for new BOs.

### 2.1 Merged List + Details form (entry point to any BO type)

```
┌───────────────────────────────────────────────────────────────┐
│ Navigation Pane (left, persistent) │ Action Pane (command bar)  │
│                                     │  New | Edit | Submit |     │
│                                     │  Approve/Reject | Post |   │
│                                     │  Cancel | Print | Options  │
├───────────────────────────────────────────────────────────────┤
│ Filter pane (collapsible)  │  Grid: sortable/filterable list,    │
│                             │  status column, saved views        │
├───────────────────────────────────────────────────────────────┤
│ Selected record — FastTabs (vertically stacked, collapsible,      │
│ several open at once, NOT the old fixed horizontal tab strip):    │
│  • General          (header fields)                                │
│  • Lines            (child grid, e.g. PO lines)                    │
│  • Financial dimensions (Cost Center / WBS / Profit Center / etc.  │
│    — see doc 07 §6, Controlling objects)                           │
│  • Attachments (document handling)                                 │
│  • Related documents (source doc → this → next, via Relations)     │
│  • Workflow history (approval timeline)                            │
│  • Change history (audit trail, field-level diff)                  │
│  • Notes                                                            │
└───────────────────────────────────────────────────────────────┘
```

- The **Action Pane** is a single command bar at the top of the form; its buttons apply to the whole
  record (not to one FastTab) and are **driven by the current FSM state + the user's security role/
  privilege** — never hard-coded per screen. Adding a new transition to a BO surfaces its button everywhere
  automatically. This matches Microsoft's own definition: "Actions on the Action Pane should be related to
  the whole [record], not a specific section of it."
- **FastTabs**, not tabs: several can be expanded simultaneously, the form scrolls vertically through them —
  this is a deliberate Microsoft UX change (replacing the older "Panorama" control) and we inherit it rather
  than reinventing a tab strip.
- Header KPIs (e.g. PO "Total Value", "Received %", "Invoiced %") render as a summary strip above the
  FastTabs, declared in BO metadata (`[HeaderKpi]` attribute) the same way regardless of which FastTab is
  open.

*Ref: [Details Master form pattern](https://learn.microsoft.com/en-us/dynamics365/fin-ops-core/dev-itpro/user-interface/details-master-form-pattern),
[Simple List and Details form pattern](https://learn.microsoft.com/en-us/dynamics365/fin-ops-core/dev-itpro/user-interface/simple-list-details-form-pattern),
[User interface elements](https://learn.microsoft.com/en-us/dynamics365/fin-ops-core/fin-ops/get-started/user-interface-elements)*

### 2.2 Workspaces (role-based landing pages)

A **Workspace** is a role-oriented landing page (e.g. "Project Manager Workspace", "AP Clerk Workspace")
combining KPI tiles and embedded lists (e.g. "My Approvals", "Projects Over Budget", "Overdue Invoices").
Per Microsoft's current workspace form pattern, workspace content sections are also FastTabs — stacked
vertically and collapsible — for the same consistency reason as record forms.

*Ref: [Workspace form pattern](https://learn.microsoft.com/en-us/dynamics365/fin-ops-core/dev-itpro/user-interface/workspace-form-pattern),
[Build operational workspaces](https://learn.microsoft.com/en-us/dynamics365/fin-ops-core/dev-itpro/user-interface/build-workspaces)*

## 3. Navigation Model

```
App Shell (Apps.Shell)
 └─ Navigation Pane: Modules → Areas → menu items (persistent left nav, Dynamics 365 pattern)
     └─ Workspace          role-based landing page, reached from a module/area entry
         └─ List + Details form   merged grid+record view per BO type (§2.1)
             ├─ Drill-down          click a linked field (e.g. vendor name on a PO) to open that
             │                      record's own List+Details form — hyperlink navigation, not a
             │                      Fiori-style quick-view popover, matching actual Dynamics behavior
             └─ Quick Create        modal dialog to create a related record without leaving the page
```

- **Navigation Pane structure**: Modules (e.g. Procurement) → Areas (e.g. Purchase orders) → menu items
  (e.g. "All purchase orders", "Purchase order workspace") — this is the actual Dynamics 365 F&O navigation
  concept, not a flat tile launchpad.
- **Deep linking**: every List+Details form URL is shareable/bookmarkable and reproduces the exact filtered/
  selected record state.
- **Global search** is federated across modules via each module's registered search provider — not a giant
  shared index owned by one module.
- We keep one SAP-flavored addition on top of this Dynamics-accurate base: a **stable intent id** per
  navigable target (`{BusinessObject}-{action}?key={id}`), resolved by an `Apps.Shell` router, so modules
  never hardcode routes to each other. This is a defensible deviation from pure Dynamics form-name
  navigation, needed because our modules are independently deployable (doc 01 §3) — Dynamics F&O is a single
  deployable unit and doesn't have this problem, so it doesn't need this indirection; we do.

*Ref: [Navigation concepts](https://learn.microsoft.com/en-us/dynamics365/fin-ops-core/dev-itpro/user-interface/page-navigation),
[Page layout in the web client](https://learn.microsoft.com/en-us/dynamics365/fin-ops-core/dev-itpro/user-interface/page-layout)*

## 4. UI Framework

- **Stack**: React 18 + TypeScript, built against `Platform.UI` — an in-house design system, not a grab-bag
  of third-party components, so bilingual/RTL and record-form behavior is consistent everywhere.
- **Design tokens**: color, spacing, typography defined once; **theme-able** (light/dark, and per-tenant
  branding) without code changes.
- **Bilingual & RTL**: every component is authored logical-direction-aware (`start`/`end`, not `left`/`right`)
  so flipping `dir="rtl"` for Arabic requires zero component-level changes. Arabic is a first-class layout
  direction, not a mirrored afterthought.
- **Accessibility**: WCAG 2.1 AA baseline (keyboard nav, ARIA roles, contrast) enforced by automated checks
  in CI, because government/enterprise procurement in KSA increasingly requires it.
- **Responsiveness**: List+Details form templates adapt from desktop (site engineers/accountants at
  desks) down to tablet (site supervisors in the field); a native-feeling mobile shell is a Phase 5+ concern
  (see Roadmap), not a v1 requirement.
- **Micro-frontend packaging**: each `Apps.X` is independently buildable/deployable and composed at runtime
  by `Apps.Shell`, so a Construction-module release never forces a redeploy of Finance's frontend.
