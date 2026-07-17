# Roadmap

This is the single, current statement of where the project is and what's next. It replaces the old
`ROADMAP.md`, which had grown into a hard-to-follow mix of original phase definitions
and half a dozen dated checkpoints layered on top of each other as audits happened. The actual, always-true
state of what's built lives in `PROGRESS.md`'s Phase Status Summary — read that table for the current
status of any phase before trusting anything below, since this document describes the plan and PROGRESS.md
describes reality. When a phase's status changes, update PROGRESS.md first; this file only needs updating
when the plan itself changes, not every time something gets built.

## Why the phases are ordered this way

No business module was built until the platform kernel — authentication, security, workflow, audit,
localization, number ranges, the Business Object lifecycle engine — was solid, because retrofitting those
concerns into modules built without them is exactly the kind of rework this architecture exists to avoid.
From there, Master Data and Finance's core ledger came first, since every later module either references
Master Data or eventually posts into Finance. Procurement came next because it's the first full multi-step
business process (Requisition through Payment) that exercises everything the kernel and Phase 1 provide.
Construction and Project Management came after that as the industry-specific commercial layer sitting on
top of everything built so far — and partway through that phase, a genuine sequencing correction was made:
Accounts Receivable was pulled forward into this same phase rather than left for later, because a
construction company's AR is not a generic invoice — it's an Interim Payment Certificate measured against a
BOQ, and building BOQ/measurement without AR to bill against, or AR without BOQ/measurement to bill from,
would have meant building half a capability twice.

## Phase 0 — Platform Foundation — Completed

The kernel every later module depends on: the Business Object base class and lifecycle engine, number
ranges, real authentication and role/attribute-based security with a Segregation-of-Duties engine, the
Arabic/English localization pipeline with RTL and calendar support, the eventing and workflow-approval
engines, an immutable audit log, and the shared UI templates (List Report / Object Page, the Apps Shell).
Its exit criteria were proven by scaffolding a trivial, business-meaningless object end to end — created,
submitted, approved through a configured workflow, posted, audited, and rendered bilingually — confirming
the whole kernel actually works before any real business logic touched it.

## Phase 1 — Master Data + Finance Core — Exit criteria met

Business Partners, Chart of Accounts, Items, Cost Centers, Tax Codes, and Number Ranges as real persisted
data — the first business data in the system, replacing the in-memory kernel scaffolding everything before
it ran on. Finance's General Ledger and Accounts Payable both run the full Draft → Submit → Approve → Post
→ Reverse lifecycle with a real audit trail, and Bank Accounts plus AP Payment Recording were added on top
once that gap was identified — a company can now record that an invoice was actually paid, not just that it
was approved. Accounts Receivable, Document Splitting, Parallel Ledgers, Budget Control, and Results
Analysis remain later Finance depth, not required for this phase's own exit bar.

## Phase 2 — Procurement — Exit criteria met

The full procure-to-pay cycle: Vendor Prequalification (the first genuinely multi-step approval workflow in
the system, five independent departments each reviewing a vendor for a specific role and trade rather than
one approver wearing five hats) followed by Purchase Requisition, RFQ, Purchase Order, Goods Receipt, and a
working three-way match against Accounts Payable. Real Budget Control enforcement and a real line-by-line
invoice match are deferred Finance/Procurement depth, not required for this phase's exit bar — the check
that exists today is a deliberate pass-through stub, disclosed rather than hidden.

## Phase 3 — Construction, Project Accounting & Accounts Receivable — In progress

Project Management's WBS foundation is built — a project and its hierarchical Work Breakdown Structure,
each element carrying the planning/account-assignment/billing flags that decide what role it plays.
Construction's Customer Contract and Bill of Quantities are built on top of it, followed by Subcontracts
with their own retention, mobilization-advance, and back-charge terms, and then Site Progress/Measurement —
a Measurement Sheet recording certified quantities against a Contract or Subcontract's lines, built
polymorphic over "commercial document" from day one and the first place the `IsBillingElement` WBS flag is
actually enforced. What's still ahead in this phase: IPC billing (which Measurement now unblocks),
Variation Orders, real Retention withholding and release, and Extension of Time/Claims on the Construction
side; and on the Finance side, the Accounts Receivable depth this phase was deliberately
expanded to include — Interim Payment Certificates as their own document type distinct from a plain AR
invoice, Fiscal Year/Period management, a real Budget Check replacing today's pass-through stub, and a
generic Statement pattern (opening balance → transactions → running balance → aging) built once and reused
for both customers and suppliers rather than rebuilt per module later. The detailed process design for
everything still ahead in this phase — the exact IPC calculation, the two-party measurement certification
workflow, the Client↔Main Contractor↔Subcontractor billing hierarchy, Extension of Time as its own document
type — lives in `construction-commercial-processes-spec.md` and
`docs/architecture/07-integrated-project-controlling.md`; read both before starting any of it.

This phase's exit criteria: a project can be set up with a BOQ, subcontracted, progress-measured on site,
and have variation orders flow through approval into cost; an Interim Payment Certificate can be submitted,
certified, and billed to the customer through a real AR posting, with aging visible on that customer's
Statement.

## Checkpoint — between Phase 3 and Phase 4 (not yet started)

A cluster of capabilities that all depend on real WBS cost postings existing first, and share the same
WBS-facing design, so they're grouped together rather than scattered across later phases: Treasury and Cash
Management; Fixed Assets, built with time-dependent WBS cost-object assignment from the start rather than a
simple asset register, since equipment depreciates on a fixed schedule but is used across multiple projects
over its life; Equipment and Fleet cost allocation (the internal usage-rate mechanism, distinct from
depreciation); Plant/Equipment Maintenance; Inventory/Warehouse Management, including the Goods Issue event
that finally makes material cost postable to a specific project rather than just purchased; Cost Codes;
WIP/percentage-of-completion revenue recognition (Results Analysis); and Multi-Company/Legal Entity
structure. The full design reasoning for the Materials, Equipment, and Finance-integration pieces of this
checkpoint is in `docs/architecture/07-integrated-project-controlling.md`.

## Phase 4 — HR & Payroll — Not started

Wider in scope than a generic HR/Payroll module: Employee Financial Management (salary advances/loans,
End-of-Service Benefit, vacation liability, ticket encashment — legally mandated or contractually standard
for a Saudi expatriate construction workforce, not optional extras), HR document-expiry monitoring (Iqama,
visa, passport) wired into the same alerting surface Finance uses, and project mobilization/demobilization
tracking as a first-class link between an employee and a project, since site labor is routinely hired for a
specific project rather than a permanent department assignment. Field-Level Security should land in this
phase too, applied to its first genuinely sensitive fields — salary, IBAN, national ID — since this is the
first phase where such fields actually exist. The reasoning behind why this module looks different from a
generic office-HR system is in `docs/architecture/07-integrated-project-controlling.md` §5, and the module's
own current scope is tracked in `docs/module/hr.md` and `docs/module/payroll.md`.

## Phase 5 — Reporting, Analytics & Mobile — Not started

Statutory and management reporting, built once enough of the modules above are producing real numbers to
report on — deliberately sequenced after project costing exists rather than before, since a reporting
module built ahead of its data sources would need reworking once they catch up. Notifications and Output
Management should land before the statutory-report generation work in this phase, since it's the more
foundational of the two and report delivery depends on it existing.

## Phase 6 — Extensibility Ecosystem & Advanced Capabilities — Not started

External integration and extensibility framework — currently placeholder-only in the platform layer.

## Deliberately deferred, not urgent

Multi-currency, Withholding Tax as a concept distinct from VAT, Document Control/Drawings/RFIs/Site
Diary/HSE Incident Tracking, and wiring the already-built-but-unused ZATCA e-invoicing and Hijri calendar
services into the screens that should use them — all real, disclosed gaps, none of them blocking any phase
above.

## Open questions — need a direct answer before being scoped into a phase

Real Estate/Site-Land Management, and Joint Venture/Consortium accounting (which needs Multi-Company to
exist first) — both depend on how this specific company actually operates rather than a generic industry
assumption, and shouldn't be guessed into a phase without asking first.
