# 09 — Navigation & UI Standard

This document exists because of a specific, real problem: as modules get added, the left-hand navigation
has been growing into a single flat, ever-expanding list rather than an organized structure — which is
exactly what Dynamics 365 and SAP Fiori deliberately avoid. This is the standard the navigation must
follow going forward, and it applies retroactively — existing nav entries should be reorganized to match
this, not just new ones added correctly from here on.

## The core rule: navigation is grouped by department, never a flat list

The left panel is organized into **Modules** (matching your company's actual departments — Project
Management, Construction, Procurement, Finance, HR, and so on, exactly the same grouping
`03-module-boundaries.md` uses), each one collapsible, each containing the **Areas** that belong to it. A
new document type never gets bolted onto the nav as a new top-level entry — it goes inside its owning
module's existing group. This is the single rule that prevents the flat, endlessly-scrolling list: the
nav's total top-level item count is bounded by the number of departments, which grows slowly, not by the
number of document types, which grows constantly.

```
Project Management
  Projects
Construction
  Contracts
  Subcontracts
  (Site Progress, Variation Orders, IPCs — added here as each is built, never as new top-level entries)
Procurement
  Vendor Prequalification
  Purchase Requisitions
  ...
Finance
  Journal Entries
  AP Invoices
  Payments
  ...
```

Every module gets exactly one entry in the top-level nav, established the moment that module's first real
screen ships — `docs/module/*.md`'s own "Frontend" section for each module should confirm the nav group it
registered under, so this is checkable, not assumed.

## A landing page, not a blank first screen

Logging in should land on a real home/dashboard page, organized the same way as the nav — one section or
card per department, each showing a short heading, an icon representing that department, and a small
summary or shortcut into that department's most-used screen (e.g. Construction's card might show "Contracts
Pending Approval: 3" and link straight into the Contracts list). This is what Dynamics 365's own landing
workspace does, and it's the piece currently missing that's causing "one page and left bar, no departments
arranged" — the fix isn't just reorganizing the side nav, it's giving the front page the same
department-first structure the nav has, so a user's very first impression of the system is "here are the
departments I work in," not a blank screen with a long list on the side.

## Icons — consistent, not decorative

Each department gets one consistent icon, used in both the nav group header and its landing-page card —
the same icon in both places, never a different one per screen. Icons should be simple, universally
recognizable symbols for the department's function (a document/contract icon for Construction, a
truck/box icon for Procurement, a coin/ledger icon for Finance) rather than decorative or overly literal —
consistency of meaning matters more than visual flourish here.

## Within a department's Area — the List + Details pattern, not a new shape each time

Every document-type screen inside a module follows the same `SplitView` list-plus-details pattern already
established (`01-overview.md`), regardless of which department it belongs to — a list on one side, a
details view with FastTabs on the other, the same create/submit/approve action pattern everywhere. A new
screen should never invent its own layout; if an existing document type's screen doesn't fit the pattern
well, that's a signal to revisit the pattern, not to build a one-off exception, per the existing rule in
`AGENTS.md` against one-off screens.

## What this fixes, concretely

Before this standard: every new Business Object shipped, its screen got added directly to the nav's root
list, and the nav grew by one flat entry every time — which is exactly the "keeps expanding" complaint.
After this standard: a new Business Object's screen is added *inside* its module's existing nav group, the
nav's top-level structure stays stable at "one entry per department," and a new user's mental model stays
"the system is organized like my company" rather than "the system is organized like a list of every
feature ever added."
