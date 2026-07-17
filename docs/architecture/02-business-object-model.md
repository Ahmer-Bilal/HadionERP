# 02 — Business Object Model

Every document type in this system — a Project, a Contract, a BOQ line, a Purchase Order, a Journal Entry —
is a **Business Object**, and every Business Object follows the same underlying rules regardless of which
module it lives in. This document defines those rules once, so no module has to redefine them, and so a
developer or an AI agent moving between modules already knows the shape of whatever they're looking at.

## The lifecycle every Business Object follows

A Business Object always starts in Draft, moves to Submitted when its owner believes it's ready, and to
Approved once whoever holds the required approval role has signed off — this three-step
Draft→Submit→Approve lifecycle is the baseline every object in the system uses, down to the master-data-
shaped ones like Business Partner or GL Account. A Business Object that represents an actual financial
posting — a Journal Entry, an AP Payment — continues past Approved into Post and, if the posting needs to
be undone, Reverse. Posting is never edited or deleted after the fact; correcting a posted document always
means reversing it and, if needed, posting a new correct one, so the audit trail stays a complete and
honest history of what actually happened rather than a record of what it was edited to look like
afterward.

Not every object needs the Post/Reverse extension — a Contract, a Project, a Business Partner are
organizational or commercial-agreement objects, not journal-posting documents, and correctly stop at
Approved. Deciding whether a new object type needs Post/Reverse comes down to one question: does approving
this object, by itself, represent money actually moving? If yes, it needs Post/Reverse. If it's an
agreement or a plan that only *causes* money to move once something else references it, Approved is the
right stopping point.

## Computed values are never entered by hand

Any total or aggregate value on a Business Object — a Contract's `ContractValue`, a Subcontract's
`NetPayableValue`, a Journal Entry's balanced-or-not check — is always computed from its own child lines,
never typed in directly by whoever is creating the document. This is a hard rule, not a style preference:
the moment a header total can be entered independently of its lines, the two can drift apart, and nothing
in the system can then tell you which one is actually correct. A header value should always be able to be
deleted and regenerated from its lines and come back identical.

## Security and Workflow — registered per object, never assumed

Every Business Object registers its own Security (at minimum a Maintainer duty for create/edit/submit;
where the document carries real financial or compliance weight, a separate Approver duty, plus the
Segregation-of-Duties conflict rule between the two, since the same person creating and approving the same
document is the textbook SoD violation every audit checks for first) and its own Workflow (at minimum a
single Any-quorum approval step; a genuinely multi-step review — the way Vendor Prequalification runs five
independent departmental reviews — is only justified when the real-world process actually has that many
independent reviewers, not added by default for objects that don't need it).

## How Business Objects reference each other — the WBS/BOQ/Cost Code chain

This is the mechanism worth understanding precisely, since it's the backbone of how a project's cost
structure actually gets built up across several different documents, potentially owned by different
modules.

A **WBS Element** (Project Management) is the root reference point. It is not itself a cost — it's the
address that cost and revenue get posted *to*. Each WBS element carries three flags deciding its role:
whether it can hold a plan, whether real cost/actuals can post directly to it (an account-assignment
element), and whether it can be billed. A parent element in the hierarchy is often planning-only, rolling
up its children's numbers; a leaf element is usually the one that actually receives cost.

A **BOQ Line** (Construction, living inside a Contract or a Subcontract) doesn't describe scope in free
text — it holds a real foreign key to a specific WBS Element belonging to the same project the Contract or
Subcontract itself references. This is what makes the earlier-mentioned cross-project rejection possible: a
BOQ line's `WbsElementId` must resolve to an element inside that same project's own hierarchy, checked
against Project Management's `IProjectLookup`, not against a copy of the data Construction keeps itself. A
BOQ line's `Amount` (`Quantity × Rate`) is the planned revenue value that, once the still-to-be-built
Results Analysis run exists, becomes part of that WBS element's planned revenue.

A **Cost Code** — not yet built, scoped in `MISSING-FEATURES-AUDIT.md` §18 and the roadmap's Phase 3–4
checkpoint — is a different dimension from a WBS element, not a replacement for one. A WBS element answers
*which project and which part of it* a cost belongs to; a Cost Code answers *what kind* of cost it is —
material, labor, subcontract, equipment, overhead — independent of which project it's on. The two are
meant to be used together: a real cost line (a Goods Issue, a Timesheet entry, a Subcontract payment)
should carry both a `WbsElementId` and a `CostCodeId`, so a company can ask "how much labor cost across all
projects this month" (group by Cost Code) exactly as easily as "how much did Project X cost" (group by WBS
element) from the same underlying data, rather than needing two separate systems to answer two versions of
the same question. When Cost Codes are built, every future cost-producing document — Goods Issue, Timesheet
Line, Equipment Usage Log, Subcontract payment line — should carry a Cost Code reference from day one,
because retrofitting this dimension onto historical cost data after the fact is far more expensive than
including it from the start.

**Material Master Data** (an Item, in Master Data) sits one level further out again — it doesn't belong to
any project or carry any cost-code assignment itself, because it's shared reference data: a "20mm rebar"
Item is the same reference record whether Project A or Project B is using it. An Item only becomes
project-specific and cost-code-specific at the moment it's actually consumed — a future `GoodsIssue` line
(see `07-integrated-project-controlling.md` §2) is what carries the `MaterialId`, the `WbsElementId` it's
being issued against, and the `CostCodeId` classifying it as material cost, all three together. The Item
itself never carries any of those three references — it's reused across every project that ever consumes
it, and the linking happens on the transactional document, not the master data.

The general principle underneath all of this: **master data and planning objects (Item, WBS Element, Cost
Code) never reference each other directly** — they're independent dimensions. **Transactional documents
(BOQ Line, Goods Issue, Timesheet Line) are what actually link them together**, by carrying foreign keys to
whichever combination of dimensions that specific transaction actually needs. This is the same pattern SAP
itself uses (a WBS element, a cost element/G-L account, and a material are three independent master
objects; a real posting document is what ties a specific combination of them together for one transaction),
and it's why no dimension in this system should ever be modeled as a field *on* another dimension — always
as a reference living on the transactional document that connects them.

## Extending this model

A new Business Object type should default to everything above unless there's a specific, disclosed reason
to differ — and if it does differ, the module's own doc in `docs/module/` should say so explicitly, the
way Construction's docs disclose that a Contract deliberately allows more than one per project. Silent
deviation from this model is what makes a codebase inconsistent across modules built by different
contributors at different times; a disclosed, reasoned deviation is a legitimate design decision.
