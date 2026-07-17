# Agent Instructions

This repository is **HadionERP** — an enterprise ERP for construction and finance companies in Saudi
Arabia, architecturally inspired by SAP S/4HANA (financial/project accounting) and Microsoft Dynamics 365
F&O (navigation and usability). This file is read automatically by AI coding agents at the start of every
session (Claude Code, Codex, and others all follow this convention) and is the single rulebook for anyone —
human or AI — working on this codebase. There used to be a separate `CLAUDE.md` pointing back to this file;
it's gone now, folded in here, so there's exactly one place these rules live.

## This is a multi-session, multi-agent project with no memory except what's written down

Different tools and different people work on this repository across different sessions, and none of them
share memory with each other except what actually exists on disk. That's the reason the documentation
structure below is not optional decoration — it's the only thing standing between this project and the
exact mess it was in before (three overlapping audit files, a README that half-matched the code, an
architecture doc split awkwardly across two files). Read `PROGRESS.md` first, every session, before doing
anything else — its Phase Status Summary and most recent entries tell you what's actually been done, since
someone else may have already started, finished, or deliberately reversed the thing you're about to do.
Never delete or rewrite a past `PROGRESS.md` entry; add a new one.

## Where to read before you build anything

Documentation in this repository is organized by what question it answers, and you should read in this
order depending on what you're doing. If you're touching cross-module or project-costing logic — anything
that moves cost or revenue between departments — read `docs/architecture/07-integrated-project-controlling.md`
first; it's the document that explains how Materials, Labor, Equipment, HR, Assets, Finance, Procurement,
and Legal all actually connect around a project, and building any of that logic without reading it first
risks reproducing a design mistake the document already exists to prevent. If you're building or extending
a specific module, read that module's own doc in `docs/module/` — `construction.md`, `finance.md`,
`procurement.md`, `master-data.md`, `project-management.md`, `identity.md`, `hr.md`, `payroll.md`,
`reporting.md` — before writing code, since it tells you what's actually built today versus what's only
designed, and where the module deliberately stops so you don't rebuild logic that belongs somewhere else.
If you're wondering whether something is a known gap or a genuine surprise, check
`MISSING-FEATURES-AUDIT.md` — the single consolidated gap list — before assuming you've found something
new. And if you're building Construction's remaining commercial documents specifically (Site Progress, IPC,
Retention, Variation Orders, EOT/Claims), `construction-commercial-processes-spec.md` has the detailed
reasoning behind each one's shape that isn't visible from the data model alone.

## How a slice actually gets built — the layer order and the Business Object shape

Every new capability in this system is built in the same order, for the same reason each time: Domain
first (the entity and its business rules — what makes a Contract valid, what a computed total actually
computes), then Application (the service that orchestrates creation, validates cross-module references
through a published `Contracts` package, and calls the platform services), then Infrastructure (the
EF Core repository and DbContext, in that module's own Postgres schema), then Api (the controller), then
Frontend (the list/details screens following the established `SplitView` pattern, added *inside* that
module's existing left-navigation group — never as a new top-level nav entry — per
`docs/architecture/09-navigation-and-ui-standard.md`, which also covers the department-card landing page and
icon conventions). Building in this order
isn't a formality — it means the business rules exist and are testable before any UI decision gets made
around them, which is what keeps a screen from silently encoding a rule that should have lived in the
Domain layer instead.

Every real Business Object in this system follows the same shape, and a new one should too unless there's
a specific, disclosed reason not to: it stops its lifecycle at Draft → Submit → Approve unless it's
genuinely a financial posting document, in which case it continues to Post → Reverse — reversal, never
edit-in-place, is how a posted document gets corrected, so the audit trail stays intact. Any total or
computed value (a Contract's value, a Subcontract's net payable, a Journal Entry's balance check) is always
computed from its lines, never entered by hand, so the header can never drift out of sync with what the
lines actually say. Every Business Object registers its own Security (a Maintainer duty and, where the
document has real financial or compliance weight, a separate Approver duty, plus the Segregation-of-Duties
conflict rule between them) and its own Workflow (at minimum a one-step Any-quorum approval; multi-step
only where the real business process genuinely has multiple independent reviewers, the way Vendor
Prequalification's five-department review does). A module never reaches into another module's Domain or
Infrastructure directly — cross-module references always go through the referenced module's published
Contracts package, the same pattern `IProjectLookup` and `IBusinessPartnerLookup` already establish, because
this is what keeps modules independently deployable and testable rather than silently coupled.

Business rules that are genuinely company policy — approval thresholds, tax rates, validity periods,
posting rules — belong in configuration, not hardcoded, so a different company or a future rule change
doesn't require a code change. Rules that are genuinely definitional rather than policy — a journal entry's
debits must equal its credits — are the exception, and should stay hardcoded, since making them
"configurable" would just be adding a knob nobody should ever actually turn.

## Verifying a slice before calling it done

The application must always run at the end of any unit of work — the solution compiles, the backend starts
without errors, the frontend starts without errors, and the work is actually visible in a browser, not just
covered by an automated test suite. Build the real thing every time, not a throwaway demo meant to be
discarded later — an in-memory reference implementation behind a stable interface is fine, since it can be
swapped for a real one later without a rewrite, but a prototype is not. Before reporting anything done,
actually start both processes and look at it. A slice is verified the way every existing module's own
history in `PROGRESS.md` already shows: real unit tests against the Domain and Application layers, real
integration tests against actual PostgreSQL (not a mock), and a live end-to-end pass — both a `curl`
exercise of the API and a Playwright pass of the actual UI in both English and Arabic, checking RTL
rendering explicitly rather than assuming it works because LTR did.

## When documentation gets written — after, never before

A module's doc in `docs/module/` gets written or updated **only once a slice is actually built, tested,
and verified** — never before, and never left stale afterward. Writing documentation ahead of the code is
exactly what produced the three overlapping, contradictory audit files this repository used to have; each
was accurate at the moment it was written and wrong within a week, because none of them were tied to a
specific, verified state of the code. When you finish a unit of work, do two things in order: add an entry
to `PROGRESS.md` (its own template, following its existing pattern, never rewriting a past entry), then
update the relevant `docs/module/*.md` file to reflect what's actually built now — described in prose,
the way the existing module docs already read, not as an ever-expanding bullet list. If you're stopping
mid-task rather than finishing it, mark the `PROGRESS.md` entry `Blocked` or `In Progress` and say why, and
leave the module doc alone rather than documenting something that isn't actually finished yet.

## A short list of things never to do, because they've each caused a real problem before

Don't add business logic to one module's Domain layer that reaches into another module's Domain or
Infrastructure directly. Don't hard-delete any record that has left Draft status — use the reversal
pattern. Don't build a one-off screen instead of the established List Report / Object Page pattern without
flagging it as a deliberate exception. Don't implement localization, security, workflow, or audit logic
inside a business module — these are platform services, consumed from `src/Platform/*`, never
reimplemented locally. And don't contradict a documented architecture decision without raising it with the
user first — if you believe a decision is wrong, add a correction the way past entries in `PROGRESS.md`
already model: a new entry explaining what changed and why, never a silent rewrite of what came before.
