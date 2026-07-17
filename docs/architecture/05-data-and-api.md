# 05 — Data & API Standards

## Database conventions

One physical PostgreSQL database, one Postgres schema per module (`masterdata`, `finance`, `procurement`,
`projectmanagement`, `construction`, and so on) — this is the actual enforcement mechanism behind the
module-boundary rule in `01-overview.md`: a module's EF Core `DbContext` is only ever configured against its
own schema, so it is structurally incapable of querying another module's tables directly, even by mistake.
Cross-module reads always go through the other module's Contracts interface, which internally may query
that module's own schema, but the calling module never sees or constructs that query itself.

Every table backing a Business Object carries the standard audit columns (created/modified timestamps and
actor) and a `RowVersion` for optimistic concurrency — a concurrent edit conflict is a real, expected
occurrence in a multi-user system and should fail loudly and specifically, not silently overwrite one
person's change with another's. Child collections (BOQ Lines under a Contract, Addresses under a Business
Partner) cascade-delete with their parent at the database level, consistent with the rule that a Business
Object and its owned children are one unit, never independently deletable.

Records are never hard-deleted once they've left Draft status. The reversal pattern —
`02-business-object-model.md`'s Post/Reverse lifecycle — is how a posted document gets corrected; for
non-posting objects, a status like Rejected or Inactive represents "no longer valid" without destroying the
history of what happened. Hard deletion is reserved for genuinely transient Draft-only data that was never
real in the first place.

## API conventions

Every module's API surface follows the same URL shape: `api/v1/{module}/{resource}`, e.g.
`api/v1/construction/contracts`, `api/v1/projectmanagement/projects`. Standard verbs on the resource root
handle Create/Get/List; lifecycle transitions are their own sub-routes on a specific record —
`POST .../{id}/submit`, `.../approve`, `.../reject`, and where applicable `.../post`, `.../reverse` — rather
than overloading a generic update endpoint to also mean "change status." A Business Object's lines are
almost always created together with its header in one request (a Contract and its BOQ lines, a Project and
its WBS hierarchy) rather than as separate follow-up calls, since most of these documents don't make sense
to exist header-only even momentarily.

Where a header's child hierarchy needs to reference itself before any row has a real database identity — a
WBS structure where a child references its own not-yet-created parent — the request uses the temporary-ID
resolution pattern described in `docs/module/project-management.md`: caller-assigned temporary integers,
resolved into real identities server-side in one forward pass, rather than requiring the client to create
rows one at a time and stitch real IDs back together itself.

A validation failure that the client should recover from programmatically (a WBS element belonging to the
wrong project, an unknown lookup value) returns a 400 with a specific, structured reason — never a generic
500 or an opaque message a frontend has to string-match against. An authorization failure returns 403; a
Segregation-of-Duties conflict on role assignment returns 409, with the conflicting Duty pairs and reasons
structured for the frontend to actually display, not just a bare status code.

## Cross-module Contracts packages

A module's `Contracts` package is its only public surface to the rest of the system — small, focused
interfaces (`IProjectLookup`, `IBusinessPartnerLookup`, `IGLAccountLookup`) exposing exactly what other
modules legitimately need, never a general-purpose query surface onto the owning module's internal data
model. Adding a new cross-module capability means adding a new, specific method to the relevant Contracts
interface — not widening an existing method's return shape to smuggle extra data through, and not reaching
past the interface into the owning module's Infrastructure directly, even temporarily "just to get
something working."

## Lookups and controlled vocabularies

Any field that represents a fixed but company-configurable set of values (Contract Type, Unit of Measure,
Address Type) goes through the shared Lookup engine (`LookupType`/`LookupValue`, administered through the
Admin Panel) rather than a hardcoded enum — this is what lets a Contract Type list grow or change per
company without a code deployment, and it's the same reasoning behind treating business-rule values as
configuration in `04-platform-services.md`. A hardcoded C# enum is only appropriate for a value set that is
genuinely fixed by the software's own logic, never by business policy — a Business Object's own lifecycle
states (Draft/Submitted/Approved) being the clear example, since no company configuration should ever be
able to add a sixth lifecycle state.
