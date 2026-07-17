# 04 — Platform Services

These are the services every module consumes and none reimplements — the shared kernel referenced
throughout `01-overview.md` and `02-business-object-model.md`. Living in `src/Platform/*`, they exist
precisely so that "how does approval work" or "how does the audit trail work" has exactly one true answer
across the whole system, not a slightly different answer per module.

## Security — authentication, authorization, Segregation of Duties

Real authentication (Identity, `Modules.Identity`) resolves who a person actually is; everything downstream
of that identity flows through `Platform.Security`. Authorization is duty-and-role based: a Business Object
registers Duties (e.g. `Construction.Contract.Maintain`, `Construction.Contract.Approve`), Duties are
grouped into Roles, and a real user is assigned Roles — never authorized by checking their identity
directly against a hardcoded rule. Row-level and field-level security exist as concepts in the platform
today but aren't yet wired into any module's actual read/write path — see `MISSING-FEATURES-AUDIT.md` §4
before assuming either is enforced anywhere yet.

Segregation of Duties is the platform's own conflict-detection engine, checking whether a proposed Role
assignment would let one person hold two Duties that shouldn't coexist — the same person creating and
approving the same type of document being the textbook example every module's own SoD rule guards against.
A genuine conflict is rejected by default; an explicit override, permanently logged with a stated reason,
is the only way past one — never a silent allow, never an unconditional block.

## Workflow — approval routing

`Platform.Workflow` is what actually moves a Business Object through Submit→Approve. A module registers an
approval definition — at minimum one Any-quorum step naming which Role can decide it; a genuinely
multi-step definition (Vendor Prequalification's five-department review) when the real process needs
independent sign-off from more than one party. The engine also supports condition-gated steps — a step that
only applies above a certain document value, for instance — a capability that exists and is proven (used to
gate Prequalification's steps by role) but hasn't yet been pointed at document *amount* anywhere, which is
exactly the "amount-conditioned approval matrix" gap named in `MISSING-FEATURES-AUDIT.md` §5.

## Audit — the immutable trail

`Platform.Audit` records every creation, field update, and status transition through `IAuditRecorder` — a
module calls it at the moment something audit-relevant happens, and the actual capture/storage logic lives
entirely inside the platform service, never reimplemented locally. This is what makes "who did this, and
when" answerable across the whole system consistently, rather than only in whichever modules happened to
remember to log it themselves.

## Localization — Arabic/English, RTL, calendars

Every user-facing string in this system exists in both English and Arabic, and every screen mirrors
correctly under RTL layout — this isn't an add-on translation pass done at the end, it's a first-class
requirement checked, per `AGENTS.md`, with a real Playwright pass in both languages before any slice is
considered done. Hijri calendar support exists as a platform service but isn't yet wired into any actual
date field in a module's UI (`MISSING-FEATURES-AUDIT.md` §9) — don't assume a date field renders in Hijri
just because the underlying service exists.

## Number Ranges

Every Business Object gets a real, sequential document number on creation (`CON-CONTR-2026-000001`,
`PM-PRJ-2026-000001`) rather than exposing its internal database identifier to a user. Each module owns its
own number-range counter and service implementation, backed by its own schema — not because the logic
differs per module, but because a module cannot depend on another module's Infrastructure directly, so even
this genuinely generic-looking piece of plumbing gets its own copy per module rather than one shared table
that would violate the module-boundary rule.

## Configuration — where business rules that vary by company actually live

`Platform.Configuration` is where a business rule that's genuinely company policy — a prequalification
validity period, an approval threshold, a tax rate — lives, overridable at the Tenant or Company level,
rather than hardcoded into a module. The test for whether something belongs here versus being legitimately
hardcoded: could a different company reasonably want a different value for this? If yes, it's
configuration. If it's a definitional rule that isn't really a policy choice at all — a journal entry's
debits must equal its credits — hardcoding it is correct, and making it "configurable" would just be adding
a control nobody should ever actually turn.

## What to check before assuming a platform service is fully wired

Several of these services have a real implementation that isn't yet called from every place it eventually
should be — Row/Field-Level Security and the Hijri calendar service being the clearest examples. Building
against a platform service means confirming it's actually invoked in the code path you're touching, not
just that the service exists somewhere in `src/Platform/*` — `MISSING-FEATURES-AUDIT.md` Part 1 is the
current, honest list of which platform capabilities are real versus structural-but-unused.
