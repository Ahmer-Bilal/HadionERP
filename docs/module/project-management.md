# Project Management

Project Management is the root of everything else in this system. No cost can post anywhere, no BOQ line
can exist, no material can be issued, and no timesheet can be charged, until a project and its Work
Breakdown Structure exist first — every other module (Construction, Finance, Procurement, HR) references
this module's data rather than owning any project-cost structure of its own. If you're reading this to
understand where a new capability belongs, and it touches "which project" or "which cost object" in any
way, it very likely belongs here or references something here — not somewhere else.

## How a project actually comes into being

A project starts as a header only — a name, an optional customer, and a rough start/end date. If a
customer is given, the system checks that customer already exists as an approved Business Partner before
letting the project past Draft; a project can't be attached to a customer that doesn't exist yet or hasn't
been approved. The project cannot be Approved with zero structure underneath it, because a project with no
Work Breakdown Structure has nothing to plan, cost, or bill against — so the Work Breakdown Structure isn't
an afterthought added later, it's created in the same request as the project header itself.

That single-request creation is worth understanding because it solves a real problem: a WBS is a
hierarchy — elements have parents — but none of those elements have a real database identity yet at the
moment someone is filling out the create form. The system handles this with temporary, request-scoped IDs:
the frontend numbers each row it's creating (1, 2, 3...) and says "row 3's parent is row 1," and the
backend resolves all of those temporary references into real identities in one pass, provided every
parent appears before its children in the request — which the UI naturally does since you build a tree
top-down. Once resolved, every WBS element has a real identity and a real parent reference, never a
temporary one.

Each WBS element carries three flags that decide what kind of role it plays in the structure, not just
where it sits: whether it can hold a plan, whether real cost/actuals can be posted directly against it, and
whether it can be billed. A parent-level element is often planning-only, existing purely to roll up its
children's numbers; a leaf-level element is usually the one that actually receives cost and gets billed.
This distinction is what later lets Construction reject a BOQ line pointed at the wrong kind of WBS element
— it's why Site Progress/Measurement (see `construction-commercial-processes-spec.md` §2) is what will
finally make the billing flag load-bearing, once that slice is built.

## Where the project stops, and where other modules pick up

A Project Definition is a planning and organizational object, not a financial document — it stops at
Approved the same way every master-data-shaped business object in this system does, with no Post/Reverse
step of its own, because nothing about "approving a project" is itself a financial transaction. Real cost
and revenue only start moving once other modules act against its WBS elements: Construction's Contract and
BOQ lines reference them (each BOQ line points at a real WBS element belonging to that same project — a
line pointing at a WBS element from a *different* project is rejected, which is what keeps two unrelated
projects' costs from ever being able to bleed into each other by mistake); Finance's Results Analysis will
eventually read actual cost against planned cost per element; Procurement's purchase orders and, once
built, Materials/Warehouse's goods issues and HR's timesheets will all eventually charge cost to a specific
element rather than to the project as an undifferentiated whole. Project Management's own job ends at
defining the structure correctly — it does not try to also own costing, billing, or scheduling logic that
rightfully belongs to the modules built on top of it.

## What exists today versus what's designed but not yet built

What's real right now: creating a project with a full multi-level WBS hierarchy in one request, the
three-flag structural model on each element, and the standard Draft → Submit → Approve/Reject lifecycle
with the same Maintainer/Approver security shape every other module's first-cut business object uses.

What's deliberately not built yet, and shouldn't be assumed to exist by anyone extending this module:
Networks — SAP's term for the activity/relationship/scheduling layer (task dependencies, critical path,
milestones, resource and equipment allocation over time) — is a materially bigger scheduling concern than
the WBS itself and is a separate future phase, not a missing corner of this one. There's also no cost or
budget data actually living on a WBS element yet; the three flags describe *what a WBS element is allowed
to do*, not what has actually happened to it — the real numbers arrive once Finance's Results Analysis and
a future cost-collector concept are built (see `docs/architecture/07-integrated-project-controlling.md`
§7 for how that's designed to work). There's also no per-element lifecycle independent of the project's
own state — in a mature system, an individual WBS element can be released, blocked, or closed separately
from its parent project ever closing; here, an element simply exists once its parent project is Approved,
with no state of its own. And there's no template/copy-from-template project creation yet — every project's
structure is built from scratch each time, which is a reasonable first cut but a genuine time cost for a
company that runs many similarly-shaped projects.

## Where to look before extending this module

If you're about to add cost, billing, or scheduling behavior directly onto `Project` or `WbsElement`,
stop and check `docs/architecture/07-integrated-project-controlling.md` first — the whole reason those
concepts are deliberately absent here is that they belong to whichever module actually produces that cost
or revenue (Construction, Finance, Materials, Labor, Equipment), read *against* this module's WBS elements
rather than stored *on* them. Project Management's role is to be the one place every other module can trust
for "does this WBS element exist, does it belong to this project, and what is it allowed to do" — nothing
more.
