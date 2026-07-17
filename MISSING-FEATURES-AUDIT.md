# HadionERP — Consolidated Gap Audit

This is the single source of truth for "what is HadionERP still missing, compared to a real deployment of
SAP S/4HANA or Microsoft Dynamics 365 F&O." It replaces three earlier files — `ARCHITECTURE-AUDIT.md`,
`HadionERP_Missing_Features_Audit_V1.0.md`, and `HadionERP_Missing_Features_Audit_V1.1.md` — which had
grown into three overlapping, partially-superseded documents written at different points in the project's
history. Their findings have been merged, deduplicated, and re-verified where possible; nothing below is
new speculation, it's a reconciliation of what those three files had already established, corrected for
what's since been resolved.

**How this file stays a single source of truth going forward:** it is a *living* document, unlike
`PROGRESS.md`'s append-only log. When a gap is closed, its status line is updated in place with a
resolution date and a pointer to the `PROGRESS.md` entry that closed it — the finding's original reasoning
stays, only the status changes. Do not create a new dated audit file when the next review happens; update
this one. If a whole new category of gap is discovered, add it as a new numbered section at the end of the
relevant part, don't start a new document.

---

## How to read this

Each finding is written as a short narrative, not a checklist: what the real product (SAP or Dynamics)
actually does, what HadionERP currently does instead, why the difference matters in practice, and roughly
which phase of work should close it. Severity is marked inline as **Blocking** (a real company cannot go
live without this), **Structural** (code exists but nothing actually uses it — zero real value today
despite looking built), **Depth** (today's version is a legitimate first cut, not a defect — more
capability is a natural later phase), or **Missing** (doesn't exist at all, not even a stub).

---

## Part 1 — Platform-level gaps (identity, security, compliance, infrastructure)

### 1. Authentication & Identity — ✅ RESOLVED (2026-07-15)
Real username/password authentication with JWT bearer tokens, a persisted Users admin surface, and a
global default-deny authorization policy now exist. Every controller resolves the real logged-in user
through `PlatformApiController.CurrentActor` rather than the three hardcoded actor literals
(`"system/ui"`, `"system/approver"`, `"system/startup"`) that every earlier audit flagged as the top
blocking issue in the whole system, since every audit trail and approval decision downstream of identity
was only as trustworthy as those literals. What's still deliberately deferred, disclosed rather than
hidden: real OIDC/SSO federation, multi-factor authentication, and refresh-token rotation — none of these
block a first pilot, but should be revisited before a production go-live with external users.

### 2. Multi-Company / Legal Entity Structure — Missing
A real SAP or Dynamics deployment separates data by legal entity (company code / legal entity) from day
one — a single database can serve multiple companies, each with its own chart of accounts, fiscal year,
and reporting currency, while still allowing consolidated group reporting. HadionERP currently has no
concept of a legal entity at all; every module implicitly assumes one company. This is fine for a
single-entity pilot but becomes a real blocker the moment a holding company with two or more subsidiaries
wants to run on this system, because retrofitting a company-code dimension after the fact means touching
every table with financial or project data. Worth deciding deliberately whether this is needed before or
after the first pilot, since the earlier it's added the cheaper the retrofit.

### 3. Segregation of Duties — ✅ RESOLVED (2026-07-15)
The SoD engine (`ISodEngine.FindConflicts`) is now load-bearing — it evaluates real conflict rules against
real user-to-role assignments (made possible once Authentication was resolved), rather than running
against a hardcoded role dictionary with no real users behind it.

### 4. Row-Level & Field-Level Security — Structural (dead code)
Both concepts have interfaces and partial implementations in the codebase, but nothing in any controller
or service actually calls them at read or write time — meaning a user with access to a module can see and
edit every record and every field in it, regardless of what row- or field-level rules exist on paper. Real
systems use this to restrict, for example, a project manager to only their own projects, or hide a
salary field from anyone outside HR/Payroll. Needs to be wired into the actual query and serialization
paths of at least Finance and HR before either module can be considered production-ready.

### 5. Amount-Conditioned Approval Matrices — Depth gap
Approval workflows exist and work (Draft → Submit → Approve is implemented consistently across every
module's business objects), but they don't yet vary by document value — a $500 purchase order and a
$5,000,000 one currently follow the identical single-approver path. Real ERPs route larger amounts through
progressively more senior approvers. This is a natural depth increase on top of what's already built,
not a structural gap.

### 6. Delegation & Escalation — Missing / Structural
No mechanism exists for an approver to delegate their approval authority while on leave, or for an
unactioned approval to automatically escalate after a time limit. Both are standard in SAP's workflow
engine and Dynamics' Power Automate approvals. Not blocking for a small pilot team, but becomes painful
quickly once approval chains involve people who take vacation.

### 7. Notifications & Output Management — Missing
No email/SMS/in-app notification fires when something needs a user's attention (an approval waiting, a
document rejected, an IPC certified). Users currently have to actively check the system rather than being
told when something needs them. This is a significant day-to-day usability gap once real users are on the
system, even though it has zero impact on data correctness.

### 8. ZATCA E-Invoicing — Structural (stub, unwired)
Saudi Arabia's mandatory e-invoicing (ZATCA Phase 2, QR codes and XML reporting to the tax authority) has
a stub in the codebase but isn't wired into the actual invoice-issuing flow. This is a genuine legal
compliance blocker for any Saudi entity going live, not just a nice-to-have — invoices issued without
ZATCA compliance are not legally valid in KSA.

### 9. Hijri Calendar — Structural (stub, unwired)
Similarly stubbed but unused. Lower urgency than ZATCA, but relevant for any date field a Saudi user
expects to see or enter in Hijri.

### 10. Multi-Currency — Missing
Every amount in the system is currently assumed to be a single currency. Real construction companies
routinely deal with contracts in USD, subcontracts in local currency, and equipment purchases in EUR —
all needing to roll up into one reporting currency. This is a foundational Finance capability, not an
edge case, and like Multi-Company, is much cheaper to add early than to retrofit later.

### 11. Fiscal Year / Period Management — Missing
No concept of an open/closed accounting period exists yet — meaning nothing currently stops a transaction
from being posted into a month that's already been closed and reported on. This is a basic financial
control in any real ERP and should be treated as a Finance-module prerequisite, not an optional add-on.

### 12. Reporting, Analytics & Dashboards — Missing (as scoped)
No reporting layer exists yet beyond the raw transactional screens. Expected at this stage of the build —
flagged here so it doesn't get silently assumed to exist once modules mature.

### 13. Extensibility & External Integration — Missing (as scoped)
No integration framework (webhooks, external API consumers, import/export beyond what each module builds
ad hoc) exists yet. Also expected at this stage.

### 14. Attachments — blob storage & virus scanning — Depth gap
File attachment fields exist on several business objects, but there's no real blob storage backing them
yet and no virus scanning on upload — both necessary before accepting real user-uploaded files (site
photos, signed contracts, invoices) in production.

---

## Part 2 — Core data-model gaps

### 15. Business Partner — missing master-data fields — ⚠️ PARTIALLY RESOLVED (2026-07-16)
The Business Partner entity (the shared vendor/customer/subcontractor master) was missing several fields
real companies rely on. Some have since been added — check `PROGRESS.md`'s most recent Business Partner
entry for exactly which fields landed and which remain open before assuming this is fully closed.

### 16. AP Payment Recording & Cash/Bank Management — ✅ RESOLVED (2026-07-16)
Previously the single biggest finding in the whole audit — there was no way to actually record that an
invoice had been paid, or which bank account paid it. Real payment recording and cash/bank management now
exist; see the matching `PROGRESS.md` entry for the verification evidence.

### 17. GL Account — no Document Type concept — Missing, undisclosed
The chart of accounts has no document-type dimension (SAP distinguishes an AP invoice from a manual
journal entry from a payment, at the GL posting level, purely by document type — this drives which
approval rules, number ranges, and reversal rules apply). Without it, every posting looks the same to the
ledger regardless of its business origin, which becomes a real problem the moment financial reporting
needs to distinguish, audit, or selectively reverse postings by their source document type.

### 18. Cost Center / Controlling — Profit Center & Internal Order — Missing, partially subsumed
Some of what SAP's Cost Center Accounting and Profit Center Accounting do is already partially covered by
the WBS-element-based project costing this system is built around (see
`docs/architecture/07-integrated-project-controlling.md`) — but pure overhead cost centers (e.g. "Head
Office Admin," not tied to any project) and Internal Orders (short-lived cost collectors for things like a
marketing campaign or an internal capital project) have no home yet.

### 19. Withholding Tax & Tax Jurisdiction Code — Missing, low priority
Not currently modeled. Lower priority than the ZATCA gap above, but related — worth scoping together when
Finance's tax handling gets its next pass.

### 20. Fixed Assets — Missing, entire module absent
No Fixed Asset entity, depreciation engine, or asset lifecycle (acquisition → transfer → disposal) exists
anywhere in the codebase or the roadmap. This directly blocks accurate equipment costing for Construction
— see §6 of `docs/architecture/07-integrated-project-controlling.md` for the full design of how this
should work once built.

### 21. Inventory / Warehouse Management — Missing, entire module absent
No warehouse, stock, goods-issue, or goods-return concept exists. Directly blocks accurate material
costing for Construction — see §2 of the same integrated-project-controlling document for the full design.

### 22. Plant/Equipment Maintenance — Missing, entire module absent
No preventive-maintenance scheduling or maintenance-cost tracking exists for owned equipment. Related to,
but distinct from, the Fixed Assets gap above — an asset can be perfectly recorded for depreciation
purposes while still having zero maintenance history tracked.

### 23. Real Estate / Site-Land Management — Open question, not a confirmed gap
Whether this company's operations actually need site/land management as its own capability (as opposed to
treating a project's site simply as an attribute of the Project itself) hasn't been confirmed either way.
Not counted as a gap until that's actually decided.

---

## Part 3 — Construction-industry-specific gaps

This part exists because Parts 1 and 2 above are gaps against a *generic* SAP/Dynamics deployment — but a
construction/EPC company needs an additional layer that neither platform provides out of the box either
(both need construction-specific add-ons in the real world too, e.g. SAP's own Real Estate/PS-CEM add-ons
or a RIB/Candy integration). This section is that layer.

### 24. Named in the README, not yet built
Subcontracts, Site Progress/Measurement, Variation Orders, Retention, and IPC billing are all named as
this module's intended scope in `docs/module/construction.md`, but only Contract+BOQ has actually been
built so far. This isn't a hidden gap — it's the explicitly disclosed, planned next phase of work, fully
specified in `construction-commercial-processes-spec.md`. Listed here only so this consolidated audit is
genuinely complete, not because it's a surprise finding.

### 25. Genuine blind spots for a construction ERP
A few things a construction company needs that weren't named anywhere in the original scope at all:
Extension of Time / delay claims as their own document type (distinct from Variation Orders — see §6b of
the integrated-project-controlling document for why they must stay separate); Labor cost via timesheets
tied to WBS elements, with a costing rate distinct from the payroll rate; owned-equipment costing via
usage logs distinct from rented equipment (which is just a normal Procurement flow); and the Materials/
Warehouse gap already covered in §21 above, which is really a construction-costing blocker as much as it
is a generic ERP gap.

---

## Part 4 — Fixed Assets & depreciation-to-project cost allocation (why this needs its own deep-dive)

This deserves more than a one-line finding because it's easy to under-scope. The real-world problem: a
company owns an excavator. That excavator depreciates on a fixed monthly schedule regardless of how much
it's used, but it's also used on several different projects over its life, and each project needs to see
its fair share of that cost. SAP solves this with Asset Accounting (FI-AA) posting depreciation centrally
every month, while Controlling (CO) and Project Systems (PS) separately allocate an *internal usage rate*
to whichever project actually used the equipment that month — the two numbers are related but calculated
independently, on different schedules, for different purposes. Dynamics 365 F&O follows the same pattern
via its own Fixed Assets module plus Project cost allocation. The distilled pattern, and what it means for
HadionERP specifically, is worked through in full in §4 and §6 of
`docs/architecture/07-integrated-project-controlling.md` — this entry exists mainly to flag that Fixed
Assets (§20 above) and Equipment Costing (§25 above) are two halves of one design, not two unrelated gaps,
and should be scoped together rather than built independently by different people at different times.

---

## Consolidated priority view

Reading everything above together, the gaps that actually block a first real pilot (as opposed to things
that are fine to defer past pilot) are: Multi-Company (§2) and Multi-Currency (§10) if more than one legal
entity or currency is involved from day one; Fiscal Year/Period Management (§11) as a basic financial
control; ZATCA (§8) as a legal requirement, not a preference, for any Saudi-registered entity actually
issuing invoices; and Row/Field-Level Security (§4) before any user outside a small trusted pilot team gets
access. Everything else in Parts 1–3 is real, disclosed, and worth planning for, but doesn't block getting
a first project running end-to-end on the system.
