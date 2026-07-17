# 03 — Module Boundaries & the End-to-End Process

This document has two jobs. First, it says which module owns which data and which decisions, so nobody
duplicates something another module already owns. Second — and this is the part worth reading in full even
if you already know your own module well — it walks through the **complete, real-world sequence** a
project actually goes through, department by department, from the moment it's created to the moment a
subcontractor gets paid, so every contributor sees where their own piece fits into the whole rather than
only ever seeing their own module in isolation.

## Who owns what

Project Management owns the Project and its WBS structure — the cost/schedule backbone everything else
references, never duplicates. Master Data owns Business Partners, Chart of Accounts, Items, Cost Centers,
Tax Codes — shared reference data every module reads, never owns a copy of. Construction owns the
commercial documents specific to a construction project: Contracts, BOQ, Subcontracts, and (once built)
Site Progress, IPCs, Retention, Variation Orders, and Claims. Procurement owns the generic buy-side
documents — Vendor Prequalification, Purchase Requisition, RFQ, Purchase Order, Goods Receipt — used by
every department that needs to buy something, construction-specific or not. Finance owns the ledger every
other module eventually posts into, and the judgment calls that are genuinely financial rather than
operational — which revenue-recognition method to use, when a period closes, how a Results Analysis run
values partially-complete work. Identity owns who a real person is and what they're allowed to do. HR owns
who an employee is, their org placement, and their project mobilization. Payroll owns what an employee is
actually paid, kept deliberately separate from what a project is charged for their time (Finance/Costing's
concern, not Payroll's). Reporting owns cross-module read-only views, never a copy of any other module's
data.

No module ever reaches into another module's Domain or Infrastructure directly — every cross-module
reference goes through the owning module's published Contracts interface, per `01-overview.md`.

## The full process, start to finish

A project begins in Project Management: someone creates a Project header, optionally attaching a Business
Partner already holding the Client role (validated through Master Data's own lookup, not re-entered), and
in the same request builds out its Work Breakdown Structure — a hierarchy of WBS elements, each one flagged
for whether it can plan, receive cost, or be billed. The project cannot be Approved with an empty structure,
because nothing downstream has anywhere to attach to until this structure exists.

Once the project is Approved, Construction picks up the thread. A Customer Contract is created against that
project, and its Bill of Quantities is priced line by line, each line pointing at a specific, real WBS
element from that same project — never a free-text description of scope. The Contract's total value is
always the sum of what the BOQ actually says, never a number typed in separately. If part of the work will
be executed by a subcontractor rather than the company's own crews, a Subcontract is created the same way —
its own line items against the same project's WBS elements, its own commercial terms (retention,
mobilization advance), and optionally a traceability link back to the specific Customer Contract it's
fulfilling scope for.

With the Contract and any Subcontracts in place, execution begins, and this is where several departments
start producing cost against the same WBS elements independently, each through their own document type,
each eventually meant to also carry a Cost Code once that dimension exists (`02-business-object-model.md`).
Procurement raises Purchase Orders for materials and services needed on site, gated by whether the vendor
is prequalified for the relevant trade; once goods are received, they land in a warehouse first, not
directly on the project — Materials/Warehouse then issues them out against a specific WBS element only once
they're actually consumed on site, which is what turns a purchase into a real project cost rather than just
inventory sitting in a yard. HR has already handled getting the right people mobilized to this specific
project; Labor Costing captures their time through Timesheets, each line charged to a WBS element at a
costing rate that is deliberately not the same number Payroll actually pays them. Equipment usage — a
company-owned excavator or crane working on this project — gets logged the same way, at an internal usage
rate distinct from what that equipment is actually depreciating at in Finance's Fixed Asset records.

Meanwhile, the customer's Engineer periodically certifies how much physical work has actually been done —
Site Progress/Measurement, recorded per BOQ line, with the contractor's submitted quantity and the
Engineer's certified quantity kept as two distinct numbers, because certified is very often lower than
submitted and that gap is a routine, expected part of the process, not an error condition. Certified
measurement is what Construction's IPC is built from: the exact deduction waterfall (gross value done,
less previous IPCs, less retention, less advance-payment recovery) is spelled out in full in
`construction-commercial-processes-spec.md`, and once an IPC is certified, it's what actually creates the
customer-facing AR posting in Finance — a project doesn't earn recognized revenue by having a Contract, it
earns it by having certified, billed progress. If a Subcontractor is involved, they go through their own
independent measurement-and-IPC cycle against the Main Contractor, on its own timeline, not synchronized to
the customer's own cycle — the two billing relationships run in parallel, connected only by the fact that
they reference overlapping scope, per `construction-commercial-processes-spec.md` §6c.

If the scope of work changes mid-project, a Variation Order is raised against the Contract, adjusting the
relevant WBS element's planned cost and revenue and, if subcontracted, flowing down as a back-to-back
Variation Order on the relevant Subcontract with its own separately negotiated rate. If a delay occurs, an
Extension of Time claim is raised as its own document — never folded into a Variation Order — because a
delay can carry time relief with no cost impact, or cost impact with no time relief, and conflating the two
loses that distinction.

Underneath all of this, Finance is periodically running Results Analysis against every WBS element with
activity — comparing actual cost incurred (from Materials, Labor, Equipment, and Subcontracts) against
budgeted cost, calculating a percentage of completion, and comparing that to what's actually been billed
through IPCs so far — posting the adjusting entry that makes a project's monthly P&L reflect real progress
rather than the accident of when an IPC happened to land. This is a Finance-owned judgment applied to
operational data it reads from everywhere else, never calculated by Construction or Project Management
themselves.

## Why this narrative matters more than the ownership table above

The ownership list tells you what to touch. This narrative tells you what happens *around* whatever you're
about to touch — which is usually the more important thing to understand before changing it, because most
real defects in a system like this don't happen inside one module, they happen at the handoff between two.
Before extending any single module, it's worth re-reading the paragraph above that your change sits inside,
to make sure you're not quietly breaking an assumption the next department downstream is relying on.
