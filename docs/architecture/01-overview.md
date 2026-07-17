# 01 — Architecture Overview

HadionERP is architecturally inspired by two different real products for two different reasons: SAP
S/4HANA for how financial and project accounting actually work under the hood (the Universal Journal, the
WBS-element-as-cost-backbone model, percentage-of-completion revenue recognition), and Microsoft Dynamics
365 F&O for how the application should actually feel to use (navigation, List Report / Object Page screens,
FastTabs, an Action Pane driven by document state). Neither product is being cloned wholesale — the parts
worth borrowing are borrowed deliberately, and where this system's needs genuinely differ (a construction-
specific commercial layer neither product ships out of the box), it's designed fresh rather than forced
into a shape that doesn't fit.

## The layered architecture, and why each layer exists

Every module is built in five layers, always in the same order, because each layer answers a genuinely
different question and mixing them is exactly what makes a codebase hard to change safely later. Domain
answers "what is this thing, and what makes it valid" — the entity itself and its business rules, with no
knowledge of databases, HTTP, or UI. Application answers "how does creating or changing this thing actually
happen" — orchestration, cross-module validation through another module's published Contracts, and calls
into the shared platform services. Infrastructure answers "how does this actually get persisted" — the EF
Core repository and DbContext, scoped to that module's own Postgres schema. Api answers "how does the
outside world reach this" — a thin controller translating HTTP into Application-layer calls. Frontend
answers "how does a person actually see and act on this" — the List/Details screens.

A module's Domain layer never reaches into another module's Domain or Infrastructure directly. This is the
single most important structural rule in the whole system, and it's what keeps every module independently
testable and independently deployable rather than silently coupled through internal implementation details.
When one module genuinely needs something from another — Construction needing to know a Project is
Approved, Procurement needing to know a Business Partner holds a certain role — it goes through that other
module's **Contracts package**: a small, published interface (`IProjectLookup`, `IBusinessPartnerLookup`)
that the owning module implements and everyone else consumes. The owning module can change its internal
Domain and Infrastructure freely as long as it keeps honoring its own Contracts interface; nothing else
breaks.

## Technology stack

Backend: .NET, ASP.NET Core Web API, Entity Framework Core against PostgreSQL. Every module gets its own
Postgres **schema** within one physical database — schema-per-module is the actual boundary, not a
database-per-module split, since these modules genuinely need to be queried together for reporting even
though they must never directly share tables. Frontend: a React single-page application (`Apps.Shell`)
consuming the backend's REST API, built around a shared `SplitView` list-plus-details pattern rather than
each module inventing its own screen shape.

## The platform kernel — shared services, never reimplemented per module

Security, Workflow, Audit, Localization, and Number Ranges are **platform services**, consumed from
`src/Platform/*` by every module, never reimplemented locally inside a module. This is worth stating as an
explicit rule because the temptation to write "just a small local version" of one of these inside a
specific module is real and has a specific cost: it means that module's audit trail, or its approval
routing, silently doesn't match every other module's, and nobody notices until a compliance review asks
why. Full detail on each of these services lives in `04-platform-services.md`.

## What "production-shaped from day one" means in practice

Every phase of work extends the real, running application — never a throwaway prototype meant to be
discarded later. An in-memory reference implementation behind a stable interface (an early
`InMemoryNumberRangeService`, for instance) is fine, because it gets swapped for a real one later without a
rewrite; a demo screen that doesn't follow the real patterns is not. At the end of any unit of work, the
solution must compile, the backend must start without errors, the frontend must start without errors, and
the result must be visible in a real browser — not just covered by a passing test suite. This standing rule
is enforced the same way in `AGENTS.md`; it's restated here because it's as much an architectural principle
as it is a workflow rule.
