# Construction

Construction is the commercial layer sitting directly on top of Project Management's WBS elements. It
owns the documents that turn a project's cost structure into real money — Customer Contracts, the Bill of
Quantities priced against that contract, Subcontracts issued to execute portions of that scope, and,
still to be built, Site Progress/Measurement, Variation Orders, Retention, Extension of Time/Claims, and
IPC billing. It never defines its own project-cost structure — every document in this module references a
WBS element it doesn't own, and Project Management's `IProjectLookup` is how it checks that reference is
real, belongs to the right project, and is currently Approved. For the full commercial-process reasoning
behind each of these document types — the actual IPC calculation waterfall, how retention and advance
recovery work, why EOT is a separate document from a Variation Order, and how the whole thing changes shape
once a Subcontractor is involved — see `construction-commercial-processes-spec.md`, which this doc doesn't
repeat, only points to.

## How a Contract actually gets created

A Contract always starts from an already-Approved Project — that's checked at creation, not assumed. Its
header carries the commercial terms: a Contract Type drawn from a proper lookup (Lump Sum, Unit Price,
Cost Plus — the same shared lookup-catalog pattern used everywhere else a controlled vocabulary is needed,
not a free-text field pretending to be one), payment terms (still genuinely free text today — see below
for why), and optional advance-payment and defects-liability terms. Underneath the header sits a Bill of
Quantities: a list of priced line items, each one pointing at a specific WBS element belonging to that same
project. A Contract's total value is never typed in by hand — it's always the sum of its BOQ lines' amounts,
computed the same way a Purchase Order's total is computed elsewhere in this system, so the header number
can never drift out of sync with what the lines actually say.

The one validation worth understanding by name, because it's the thing that keeps two unrelated projects'
finances from ever being able to cross-contaminate: a BOQ line's WBS element must belong to the *same*
project as the Contract itself. A line accidentally pointed at a WBS element from a different project is
rejected outright, not silently allowed through. This is the same discipline Project Management enforces
internally when resolving a WBS hierarchy, just applied one layer up.

A Contract deliberately stops at Approved, with no Post or Reverse step of its own — it isn't a
journal-posting document, it's the commercial agreement that later documents (IPCs, once built) will
actually bill against. And a project is deliberately allowed to have more than one Contract against it over
time — real contracts get amendments and addenda, and forcing a rigid one-contract-per-project rule now
would just mean reworking it the first time a genuine amendment shows up.

## Subcontracts — the same shape, a different party, and its own commercial terms

A Subcontract looks a lot like a Contract on the surface — it has its own line items priced against WBS
elements from the same project, and its own computed total — but it exists for a different purpose and
carries terms a Contract doesn't need. Where a Contract runs between the company and its customer, a
Subcontract runs between the company and a subcontractor: a Business Partner that must specifically hold
the Subcontractor role, not any vendor-family role in general, because a Subcontract is semantically
narrower than a Purchase Order even though both are procurement-shaped documents. A Subcontract can
optionally reference the specific Customer Contract it's fulfilling scope for — useful for back-to-back
traceability when a client's Variation Order needs to flow down to the subcontractor executing that scope
— but this reference is never required, since plenty of subcontracted work exists for scope that was never
itemized in any single customer contract to begin with.

Two things exist on a Subcontract that don't exist on a Contract, because they reflect realities specific
to managing a subcontractor relationship rather than a customer one: retention and mobilization-advance
percentages as explicit commercial terms (a subcontractor's retention terms are commonly stricter than
the main contract's own, exactly the "never better than what flows down" dynamic described in the
commercial-process spec), and Back Charges — costs the company deducts from what it owes the subcontractor,
for things like damage, rework, or supplied materials the subcontractor didn't return. A Back Charge can
only be added once the Subcontract is already Approved, never during Draft, because it represents something
that happened during live execution of the work, not a line item known in advance — trying to add one to a
Subcontract that hasn't been approved yet is rejected. A Subcontract's net payable value is always
computed as its line total minus its accumulated back charges, never entered directly, for the same
reason a Contract's value is never typed in by hand.

## Site Progress / Measurement — recording what was actually done

A `MeasurementSheet` records physical progress measured on site during one period (`PeriodStart`/
`PeriodEnd`) against either a Contract's BOQ lines or a Subcontract's own lines — built polymorphic over
"commercial document" (`CommercialDocumentType` + `CommercialDocumentId`) from this first slice, per the
commercial-process spec's §6c/§7/§8 sequencing decision (ROADMAP.md), since a Subcontract needs its own
independent measurement cycle against the main contractor and retrofitting this after a Contract-only build
would have meant reworking every measurement/IPC table later. This is also the first place in the module
where the `IsBillingElement` WBS flag is actually enforced, not just carried — a line referencing a WBS
element that isn't flagged as a billing element is rejected outright.

The workflow reuses the platform's own Draft → Submitted → Approved/Rejected lifecycle unchanged for the
spec's two-party certification process (site QS submits, the Client's Engineer certifies) — "Approved" here
*is* the domain's "Certified," not a new status. Each line carries both `QuantitySubmitted` and
`QuantityCertified` as separate fields, because the Engineer certifying a lower quantity than what was
submitted is routine, not an edge case — the certify action takes an explicit per-line quantity for every
line on the sheet, not a blanket approve. A real guardrail runs at certify time: the cumulative certified
quantity for a given BOQ/Subcontract line, summed across every Approved sheet that has ever measured against
it, can never exceed that line's own `Quantity` — an approved Variation Order (not yet built) would be the
only way to raise that ceiling once it's reached.

## What's still deliberately absent

Everything downstream of "progress has been measured and certified" is still to be built. There's no way yet
to actually bill a customer or pay a subcontractor for work done — IPC (Interim Payment Certificate), which
consumes a certified Measurement Sheet, doesn't exist as a document type yet, so a Contract, Subcontract, and
now a Measurement Sheet all currently describe what was agreed or measured, not what's been paid for.
Retention exists today only as a percentage term recorded on a Subcontract's header; the running
withheld-balance and release-event mechanics described in the commercial-process spec aren't built, and
depend on IPC existing first. Variation Orders and Extension of Time/Claims don't exist as document types
yet at all — a change in scope or a delay currently has nowhere to be formally recorded against a Contract
or Subcontract, and the over-measurement guard above will hard-block legitimate additional scope until a VO
can raise a line's quantity. Cumulative certified-to-date and percent-complete are deliberately not exposed
anywhere on the Measurement Sheet API/UI yet — IPC's own "Gross Value of Work Done to Date" calculation needs
the same cross-sheet aggregation, so it's built once there rather than duplicated now.

A couple of smaller, disclosed rough edges worth knowing about rather than tripping over: `PaymentTerms`
is genuinely free text right now, not sourced from a real Payment Terms field on the Business Partner
master, because that master-data field doesn't exist yet either — so a Contract can't default its terms
from the customer's own record the way a more mature system would. And there's a known cosmetic-only RTL
issue where that same free-text field renders bidi-reversed in the Arabic UI, since it isn't wrapped the
way numeric and code fields elsewhere on the page already are — the underlying stored value is correct, only
the on-screen rendering is affected.

## Where to look before extending this module

Before adding Site Progress, IPC, Retention, Variation Orders, or EOT/Claims, read
`construction-commercial-processes-spec.md` in full first — every one of those document types has a
specific reason for its shape (why EOT can't just be a field on a Variation Order, why Measurement needs
both a submitted and a certified quantity per line, why the IPC waterfall has the exact deduction order it
has) that isn't obvious from the data model alone, and building any of them without that context risks
reproducing a mistake the spec already exists to prevent. And before wiring any of these new document types
against a WBS element, check `docs/architecture/07-integrated-project-controlling.md` for how Construction's
output is meant to connect to Finance's Results Analysis, Materials' Goods Issue, and Labor's Timesheets —
Construction produces the revenue side and the subcontract-cost side of a WBS element's actuals, but it
isn't where the rest of a project's cost comes from.
