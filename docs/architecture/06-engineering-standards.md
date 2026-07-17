# 06 — Engineering Standards

This document covers naming and testing conventions that apply across every module — the concrete,
day-to-day rules underneath the bigger architectural principles in `01`–`05`.

## Naming

A Business Object's name is the plain business term, not a technical abbreviation — `Contract`, not
`Ctrct`; `PurchaseOrder`, not `PO` at the type-name level, even though `PO` is fine as a document-number
prefix or in conversation. Service classes are named `{Object}Service` (`ContractService`,
`SubcontractService`); repository interfaces `I{Object}Repository`; Contracts-package lookup interfaces
`I{Object}Lookup` (`IProjectLookup`, `IBusinessPartnerLookup`). A module's own copies of generic-looking
infrastructure (its number-range service, its workflow-instance repository) keep the same class names other
modules use for the equivalent piece, rather than inventing module-specific names for something that plays
an identical structural role everywhere — consistency here is what lets a contributor recognize the pattern
instantly in a module they've never touched before.

Arabic-equivalent fields follow one consistent suffix pattern: `{FieldName}Arabic` (`NameArabic`,
`DescriptionArabic`), always nullable/optional at the Domain level even where the English equivalent is
required, since not every record will need Arabic content immediately.

## Testing standard

Every slice ships with three layers of verification, matching what every completed module's own
`PROGRESS.md` entries already demonstrate. Unit tests cover the Domain layer's business rules directly (a
Contract's computed value, a WBS element rejecting a duplicate code, a Journal Entry refusing to post
unbalanced) and the Application layer's orchestration and validation (rejecting a WBS element from the
wrong project, rejecting an unapproved Project). Integration tests run against a real PostgreSQL instance,
not a mock or an in-memory provider — round-tripping a full object graph including child collections and
Arabic fields, confirming cascade delete actually removes children, and confirming `RowVersion` increments
across real lifecycle transitions. And a live end-to-end pass — a real `curl` exercise of the API proving
the full lifecycle works outside the test suite's own assumptions, plus a Playwright pass of the actual
frontend screens in both English and Arabic, checking RTL rendering explicitly rather than assuming it
mirrors correctly because the LTR version did.

A slice isn't done when its tests pass — it's done when all three layers pass **and** the running
application has actually been opened and looked at, per the standing rule in `AGENTS.md`.

## Comments and self-documentation

A non-obvious business rule — why a check exists, why a value is computed one way rather than another, why
a document deliberately doesn't have a Post/Reverse step — gets a comment explaining the *reason*, not a
comment restating what the code already says. The module's own `docs/module/*.md` file is where the
broader design reasoning lives; a code comment is for the specific, local "why," not a duplicate of the
module doc.

## What "done" means before moving to the next task

A unit of work is complete when: the code exists across all five layers it needs; all three testing layers
above pass; the running application starts and shows the work in a browser; `PROGRESS.md` has a new entry
describing what changed; and, only after all of that, the relevant `docs/module/*.md` file is updated to
match — never before, per the documentation-timing rule in `AGENTS.md`. Skipping any one of these and
calling the work "done" is what produces the exact gap between documentation and reality this whole
documentation rewrite existed to fix.
