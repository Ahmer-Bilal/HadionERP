# Construction

Construction is the commercial layer sitting directly on top of Project Management's WBS elements. It
owns the documents that turn a project's cost structure into real money — Customer Contracts, the Bill of
Quantities priced against that contract, Subcontracts issued to execute portions of that scope, Site
Progress/Measurement, IPC billing, Variation Orders, and Retention withholding/release. Extension of
Time/Claims is still to be built. It never defines its own project-cost structure — every document in this
module references a WBS element it doesn't own, and Project Management's `IProjectLookup` is how it checks that reference is
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

## IPC — turning certified progress into an actual bill

An `Ipc` is generated from exactly one Approved (Certified) `MeasurementSheet` — never from caller-supplied
lines, since every figure on an IPC is derived, not entered by hand (`02-business-object-model.md`'s
"computed values are never entered by hand" rule applies to whole documents here, not just header totals).
One IPC per Measurement Sheet is enforced (`IIpcRepository.ExistsForMeasurementSheetAsync`), so the same
certified period can never be billed twice. Same polymorphism as Measurement (Contract or Subcontract), for
the same reason — a Subcontract runs its own independent IPC cycle against the main contractor, not a child
of the main Contract's IPC (spec §6c).

The money waterfall (spec §3) is computed, not stored by hand, from each `IpcLine`'s snapshotted `Rate` and
two quantities: `QuantityThisPeriod` (the source sheet's own certified quantity) and `QuantityToDate` (the
cumulative certified quantity across every Approved sheet ever measured against that line — the same
cross-sheet aggregation `MeasurementSheetService`'s over-measurement guard already computes, reused here
rather than duplicated). Retention and Advance Recovery are both a straight percentage of *this period's*
certified value — snapshotted from the Contract/Subcontract's own header percentages at IPC creation time,
not read live, so an IPC's arithmetic stays stable even if those header terms could theoretically change
later. `OtherDeductions` (liquidated damages, back-charges, prior over-certification corrections) is
genuinely manual entry this slice — none of those source mechanisms exist yet.

Same lifecycle convention as Measurement: Draft → Submitted → Approved/Rejected, where Approved *is*
"Certified" (the Engineer issuing the Payment Certificate that legally obligates payment). PROGRESS.md's
2026-07-16 "does certification raise an AR invoice immediately, or a WIP step first" question is now
resolved: for a Contract-type IPC, certification automatically raises a real **Draft** AR Invoice via
`Modules.Finance.Contracts.ICustomerInvoicingService` — the first cross-module *write* Contracts call in
this system (every earlier one, `IProjectLookup`/`IBusinessPartnerLookup`/`IAPInvoiceLookup`/
`IBudgetCheckService`, is read-only). The invoice is left in Draft, not auto-posted — a human in Finance
still submits/approves/posts it through the normal AR Invoice lifecycle, preserving Segregation of Duties
between "Construction certifies the work" and "Finance actually books the receivable." This only applies to
a Contract IPC (billing the Customer); a Subcontract IPC represents a payable *to* a subcontractor, a
separate, still-not-built AP-side integration, and needs no billing accounts at all. Because raising the
invoice needs real Revenue/Receivable GL accounts (and optionally a Tax Code/VAT account), a Contract IPC
now requires those to be chosen at Draft creation time — `IpcService.CreateAsync` rejects a Contract IPC
without them, and separately rejects one whose Project has no Customer set, since there'd be nobody to bill.
The IPC itself still stops at Approved, deliberately short of the spec's further "Paid" step — that needs a
real Customer Receipt Business Object (the AR mirror of `Payment`), not yet built.

## Retention — a running balance, released against real IPCs, not just a per-IPC deduction

`Contract` now carries its own `RetentionPercentage` (mirroring `Subcontract.RetentionPercentage`, which
already existed) — closing a real gap the IPC section above doesn't mention: before this, `IpcService`'s
own commercial-document snapshot always passed `null` for a Contract's retention percentage, so a
Contract-type IPC never actually withheld any retention at all, only a Subcontract-type one did. Every
Approved IPC still only computes *this period's* retention deduction (spec §5's "withheld this period"
figure, unchanged from the IPC section above) — what's new is `RetentionRelease`, the document that
finally tracks the *cumulative* balance and pays it back. `RetentionReleaseService.GetRetentionBalanceAsync`
sums every Approved IPC's own `RetentionAmount` for a commercial document, subtracts every prior Approved
release's `AmountReleased`, and that's the real running "Total Retention Withheld to Date − Total Retention
Released to Date" balance the spec calls for — computed fresh each time, never stored as a separate figure
that could drift out of sync. `RetentionReleaseService.CreateAsync` validates a new release's amount against
that same balance and rejects anything that would over-release. `RetentionRelease` is polymorphic over
Contract/Subcontract exactly like `Ipc`, deliberately a single header amount rather than a collection of
per-IPC release lines (the spec itself describes retention release as "one or two lump-sum events tied to
project milestones," not a line-by-line reconciliation), and reuses the same certify-then-bill pattern:
approving a Contract-type release raises a real Draft AR Invoice, approving a Subcontract-type release
raises a real Draft AP Invoice, both through the same `ICustomerInvoicingService`/`IVendorInvoicingService`
pair `Ipc` already uses. `TriggerEvent` (Taking Over Certificate / Defects Liability Expiry / Manual) is
recorded but not yet wired to actually *fire* automatically off a real Taking-Over or DLP-expiry date —
that's still a manually-triggered release for this slice.

## What's still deliberately absent

Everything downstream of "an IPC has been certified and its AR Invoice raised" beyond Retention is still to
be built. There's no way yet to record that a customer actually paid an AR Invoice, or that a subcontractor
was paid against their own IPC — the latter needs the AP-side integration mentioned above, the former needs
a Customer Receipt Business Object (both now actually built too — see `docs/module/finance.md` — this
paragraph is only about what's still missing). Extension of Time/Claims doesn't exist as a document type at
all yet — a delay currently has nowhere to be formally recorded against a Contract or Subcontract, and
Liquidated Damages (which depend on it) aren't modeled either.

A couple of smaller, disclosed rough edges worth knowing about rather than tripping over: `PaymentTerms`
is genuinely free text right now, not sourced from a real Payment Terms field on the Business Partner
master, because that master-data field doesn't exist yet either — so a Contract can't default its terms
from the customer's own record the way a more mature system would. And there's a known cosmetic-only RTL
issue where that same free-text field renders bidi-reversed in the Arabic UI, since it isn't wrapped the
way numeric and code fields elsewhere on the page already are — the underlying stored value is correct, only
the on-screen rendering is affected.

## Where to look before extending this module

Before adding Extension of Time/Claims (the one piece of §7's build order still not built), read
`construction-commercial-processes-spec.md` in full first — every one of those document types has a
specific reason for its shape (why EOT can't just be a field on a Variation Order, why Measurement needs
both a submitted and a certified quantity per line, why the IPC waterfall has the exact deduction order it
has) that isn't obvious from the data model alone, and building any of them without that context risks
reproducing a mistake the spec already exists to prevent. And before wiring any of these new document types
against a WBS element, check `docs/architecture/07-integrated-project-controlling.md` for how Construction's
output is meant to connect to Finance's Results Analysis, Materials' Goods Issue, and Labor's Timesheets —
Construction produces the revenue side and the subcontract-cost side of a WBS element's actuals, but it
isn't where the rest of a project's cost comes from.
