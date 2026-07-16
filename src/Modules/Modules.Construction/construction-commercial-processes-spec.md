# Construction Commercial Process Specification
### How large EPC/construction companies (and SAP-style ERPs) run Contracts → BOQ → Site Progress → IPC → Retention → Variation Orders → Subcontracts

This document extends the `Modules.Construction` README with the missing process detail needed to build the
remaining slices to enterprise standard (SAP RE-FX / PS-CEM / Fiori construction apps, Procore, Oracle
Primavera Unifier, Candy/CCS). It's written as a spec Claude Code can implement against directly.

---

## 1. The overall document chain

```
Customer Contract (BOQ)
   │
   ├──> Subcontracts (procurement, mapped to WBS/BOQ scope)
   │
   ├──> Site Progress / Measurement (physical % or quantity done, per BOQ line, per period)
   │        │
   │        └──> IPC — Interim Payment Certificate (billing document, draws on measured progress)
   │                 │
   │                 ├──> Retention withheld (%, released later)
   │                 ├──> Advance payment recovery (amortized against each IPC)
   │                 └──> Posts to Finance (AR, Results Analysis / WIP)
   │
   └──> Variation Orders (change the Contract's scope/value; feed BOQ + WBS cost/revenue)
```

Everything downstream of the Contract references a **BOQ line** or a **WBS element** — never re-enters
scope from scratch. This is the same principle your README already states for Contract→WBS; it just needs
to propagate downstream.

---

## 2. Site Progress / Measurement

### What it is
The record of **physical work actually done**, captured periodically (usually monthly, sometimes weekly on
fast-track projects). It is the *evidence* an IPC bills against — you cannot bill for work that hasn't been
measured and certified.

### Core data model
- `MeasurementSheet` (header): `ProjectId`, `PeriodStart`, `PeriodEnd`, `MeasuredByUserId`, `Status`
  (Draft → Submitted → Certified/Rejected), `Notes`.
- `MeasurementLine` (child, one per BOQ line touched this period):
  - `BoqLineId` (FK — never re-describes scope)
  - `QuantityThisPeriod` — physical quantity completed in this period
  - `QuantityToDate` — cumulative (system should compute this from history, not accept manual entry, to
    prevent double-billing)
  - `PercentComplete` — derived: `QuantityToDate / BoqLine.Quantity`
  - `Remarks`, optional photo/attachment references (site engineers commonly attach progress photos —
    real systems store these as document attachments linked to the line)

### Workflow (why it's not a simple Draft→Approve like your other BOQs)
Big contractors use a **two-party certification** step, because the customer's Engineer/Consultant must
agree with the contractor's measurement before it can be billed:

1. **Draft** — site QS (quantity surveyor) enters measured quantities
2. **Submitted** — sent to the Client's Engineer/Consultant for review
3. **Certified** — Engineer agrees the quantity (may certify a *lower* quantity than submitted — very
   common; the system should allow `CertifiedQuantity != SubmittedQuantity` per line, not just approve/reject
   the whole sheet)
4. **Rejected/Disputed** — a line can be certified at zero or partial pending resubmission

**Design implication:** `MeasurementLine` needs both `QuantitySubmitted` and `QuantityCertified` — this
delta is a routine, expected occurrence in real projects, not an edge case.

### Guardrails
- A line can never have cumulative `QuantityToDate` exceed the BOQ line's `Quantity` (over-measurement)
  without an approved Variation Order increasing that BOQ line's quantity first.
- Only WBS elements flagged `IsBillingElement` should be eligible (this is exactly the flag enforcement your
  README defers — Site Progress is where it becomes load-bearing, as you already noted).

---

## 3. IPC — Interim Payment Certificate

### What it is
The **periodic billing document** a contractor issues to the customer, built from certified measurement.
It is the construction-industry equivalent of a sales invoice, but with several deductions unique to the
industry. This is what your README calls the "AR / IPC billing" gap.

### The IPC calculation (this exact waterfall is industry-standard — FIDIC-based contracts worldwide use it)

```
1. Gross Value of Work Done to Date
   = Σ (BoqLine.Rate × MeasurementLine.QuantityCertified, cumulative to date)

2. Less: Value of Work Done in Previous IPCs
   = Gross Value to Date (previous IPC)
   ────────────────────────────────────
   = Gross Value THIS PERIOD

3. Add: Approved Variation Orders value (this period, if not already folded into BOQ)

4. Less: Retention
   = Gross Value to Date × Retention% (per Contract terms — commonly 5–10%, often capped,
     e.g. "10% retained until 50% completion, then 5% until Substantial Completion")

5. Less: Advance Payment Recovery
   = (Advance Payment / Contract Value) × Gross Value this period
     — i.e. the mobilization advance is recovered pro-rata against each IPC, not
     in one lump sum

6. Less: Other deductions
   = Liquidated damages (if any), materials-on-site advances being recovered,
     back-charges, previous over-certifications being corrected

7. = NET PAYABLE THIS IPC
```

### Data model
- `Ipc` (header): `ContractId`, `IpcNumber` (sequential per contract), `PeriodStart/End`,
  `MeasurementSheetId` (source), `Status` (Draft → Submitted → Certified → Paid), `GrossValueToDate`,
  `GrossValuePreviousIpc`, `RetentionAmount`, `AdvanceRecoveryAmount`, `OtherDeductions`, `NetPayable`.
- `IpcLine`: mirrors `MeasurementLine` but carries the money math per BOQ line.

### Workflow
Draft (contractor prepares) → Submitted (to Engineer) → **Certified** (Engineer issues a Payment
Certificate — this is the document that legally obligates the customer to pay; contracts often specify
"payment due N days after certification," e.g. FIDIC's 28/56-day cycle) → Paid (cash received, closes the
loop into Finance/AR).

### Posting to Finance
This is the piece your README flags as still-open in `Modules.Finance`. On **Certified**, the IPC should:
- Raise an **AR invoice** (or a WIP/unbilled-revenue entry if certification and invoicing are separate
  legal steps in your target market — in many GCC/FIDIC contracts they are)
- Feed the WBS element's actual revenue, which is exactly the trigger your README says drives the next
  Results Analysis run in Finance
- Retention amounts should NOT hit revenue — they go to a **Retention Receivable** control account until
  released (see §5)

---

## 4. Variation Orders (VOs)

### What it is
A formally agreed change to scope, which changes a WBS element's planned cost and/or revenue, and — since
BOQ lines map to WBS elements — usually adds/changes BOQ lines too.

### Data model
- `VariationOrder` (header): `ContractId`, `VoNumber`, `Reason`/`Description`, `Type` (Additive/Omission/
  Both), `Status` (Draft → PricedByContractor → SubmittedToClient → Approved/Rejected), `TotalValueImpact`.
- `VariationOrderLine`: either
  - references an **existing BOQ line** with a `QuantityDelta` and optional `RateDelta` (rate changes are
    common — VO rates are often negotiated separately from the original BOQ rate), or
  - is a **new BOQ line** (`NewBoqLine` payload) for scope that didn't exist in the original contract

### Why this matters for your architecture
Your README already says a Variation Order "adjusts a WBS element's planned cost/revenue, which feeds the
next Results Analysis run in Finance" — that's correct and matches how SAP PS handles it (a VO posts to
the WBS element's *planned* values, not actuals). The missing piece is that an **Approved VO must also
write through to the Contract's BOQ**, because:
- Site Progress measures against BOQ lines — if a VO adds scope, a new/adjusted BOQ line must exist before
  that scope can ever be measured or billed
- `Contract.ContractValue` (computed as sum of BOQ line amounts, per your README) must reflect approved VOs

### Workflow nuance
Real projects run VOs at **two speeds**:
1. **Instructed but not yet priced** — the Engineer instructs the change verbally/by site instruction
   before price agreement (contractor is often obligated to proceed regardless, under FIDIC-style clauses)
2. **Priced and approved** — the value gets formally agreed

Many ERPs model this as a status like `Instructed` before `PricedByContractor`, so cost can start accruing
against the WBS element even before the commercial value is locked. Worth deciding deliberately whether
your Phase-4 slice needs this two-speed model or can start with a simpler Draft→Approved flow and add it
later — flag it as a deferred decision if you skip it, the way the rest of this README does.

---

## 5. Retention

### What it is
A percentage of each IPC withheld by the customer as security, released later — **not a separate document
type**, it's a calculated field living on the Contract (terms) and each IPC (amount withheld), plus a
release event.

### Data model
- On `Contract`: `RetentionPercentage`, optionally a **tiered rule** (e.g. 10% until 50% complete, then 5%
  thereafter — model this as a small `RetentionTier` list rather than a single flat percentage if you want
  to match real contract terms), `RetentionReleaseTerms` (free text or structured — e.g. "50% on Taking
  Over Certificate, 50% on Defects Liability Certificate")
- A `RetentionRelease` document (header + optional lines per IPC being released from): `ContractId`,
  `ReleaseDate`, `AmountReleased`, `TriggerEvent` (Taking Over / Defects Liability expiry / manual)

### Why it's its own concept, not just an IPC deduction
Retention **accumulates as a liability owed back to the contractor** — it needs to be trackable as a
running balance (`Total Retention Withheld to Date − Total Retention Released to Date`), independent of
any single IPC, because release usually happens in one or two lump-sum events tied to project milestones
(Taking Over Certificate, Defects Liability Period expiry — which your `Contract` already has a field for:
`DefectsLiabilityPeriodMonths`) rather than IPC-by-IPC.

---

## 6. Subcontracts

### What it is
Procurement documents (your README already correctly frames this) — but the construction-specific part is
that a Subcontract usually mirrors a **slice of the main Contract's BOQ**, and the subcontractor gets its
own IPC cycle against the main contractor.

### Data model
- `Subcontract` (header): `MainContractId` (or `ProjectId` — decide whether subcontracts must always tie
  to a specific customer Contract, or can exist project-wide for scope not yet billed to a customer),
  `SubcontractorId` (Business Partner), `ContractType`, `RetentionPercentage` (subcontracts have their own,
  usually higher, retention terms than the main contract), `Status`.
- `SubcontractBoqLine`: same shape as `BoqLine`, each optionally linked to the **main contract's** BOQ line
  it fulfills (`MainContractBoqLineId?`) — this lets you later report "how much of this BOQ scope is
  subcontracted out."
- Subcontractors get **their own Site Progress / Measurement / IPC cycle** — same three documents, same
  shapes, scoped to `SubcontractId` instead of `ContractId`. This is a strong argument for designing
  Measurement and IPC as **polymorphic over "commercial document"** (Contract or Subcontract) rather than
  hard-coding `ContractId` — worth deciding before you build the next slice, since retrofitting this later
  means touching every measurement/IPC table.

### Back-to-back flow-down
Large contractors deliberately mirror terms downstream: if the customer's contract has 10% retention and
56-day payment terms, the contractor typically imposes similar-or-worse terms on subcontractors (never
better) to protect cash flow. Not something to hard-enforce in software, but worth surfacing as a default/
warning when a subcontract's terms are more favorable to the subcontractor than the main contract's terms
are to the main contractor.

---

## 6b. Extension of Time (EOT) — the piece that's usually missing entirely

### Why it's separate from a Variation Order
A VO changes **scope and value**. An EOT changes **time only** — the contractor's obligation to finish by
a certain date. They're related but distinct, and contracts (FIDIC Clause 20/8.4/8.5, and most GCC/local
equivalents) treat them as separate claim types with separate substantiation requirements:

- A VO can carry its *own* EOT entitlement (extra scope usually = extra time), but
- An EOT can also arise with **zero** value impact (e.g. exceptionally adverse weather, a delay caused by
  the Engineer's late instructions, force majeure) — pure time relief, no money
- Conversely, some claims are **money without time** (e.g. cost of prolongation/idle resources during a
  delay that's already been absorbed in the program's float — the contractor wants compensation but isn't
  asking to move the completion date)

This is why real systems keep `Claim` (or `EotClaim`) as its own document type, not a field on
`VariationOrder`.

### Data model
- `Claim` (header): `ContractId`, `ClaimNumber`, `ClaimType` (EOT-only / Cost-only / EOT+Cost),
  `CauseOfDelay` (Employer-caused / Neutral event e.g. weather / Contractor-caused — this classification
  usually drives entitlement: contractor delays don't get EOT, Employer-caused delays get EOT + cost,
  neutral events often get EOT only, no cost), `DaysClaimed`, `DaysGranted`, `CostClaimed`, `CostGranted`,
  `Status` (Notified → Substantiated/Submitted → Assessed → Approved/Rejected/PartiallyApproved).
- `ClaimNotice` — many contracts have a strict **notice period** (e.g. "contractor must notify within 28
  days of becoming aware of the delay event or lose entitlement" — FIDIC's infamous condition-precedent
  clause). Worth a `NoticeDate` field and a computed `IsWithinNoticePeriod` flag against the contract's
  configured notice window, because missing this deadline is one of the most common ways contractors lose
  claims that were otherwise valid — the system should surface it as a warning, not silently allow late
  claims to look normal.
- Effect on the program: `RevisedCompletionDate = OriginalCompletionDate + Σ(DaysGranted for Approved
  claims)`. Whether you also integrate with an actual scheduling tool (Primavera P6/MS Project) for delay
  analysis (critical path impact, concurrent delay) is a bigger scope decision — most ERPs don't try to
  replicate P6's delay analysis, they just record the *outcome* (days granted) and link a supporting
  document/attachment (the delay analysis report) rather than modeling the schedule itself. Recommend the
  same here: `Claim` records the negotiated outcome, not the schedule mechanics.

### Where it touches what you've already built
- An **approved EOT** should push out the Contract's completion-related dates (relevant to
  `DefectsLiabilityPeriodMonths` timing and to whether Liquidated Damages apply — see below)
- Liquidated Damages (LDs): if the contractor finishes late **without** a corresponding EOT covering the
  delay, the customer is usually entitled to deduct LDs (often a daily/weekly rate, sometimes capped as a
  % of Contract Value) — this is exactly the "Other deductions" line in the IPC waterfall (§3, step 6).
  Model `LiquidatedDamages` as its own small entity (`ContractId`, `DailyRate`, `CapPercentage`,
  `DaysLate`, `AmountAssessed`) rather than a manual number typed into an IPC, so it's auditable.

---

## 6c. Real-world billing hierarchy: Client ↔ Main Contractor ↔ Subcontractor

This is the part that's easy to get wrong in a first design, because it looks like "just do the same IPC
thing twice," but the **timing and cash-flow direction** matter a lot in practice.

### The two IPC cycles run independently, not in lockstep
```
Client  ──IPC (Main Contract)──>  Main Contractor  ──IPC (Subcontract)──>  Subcontractor
  ↑ pays in ~28–56 days                                ↑ pays in ~30–60+ days after THAT
```

- The **Main Contractor's IPC to the Client** is driven by the *Client's Engineer* certifying the *Main
  Contractor's* measured progress against the *main Contract's* BOQ.
- The **Subcontractor's IPC to the Main Contractor** is driven by the *Main Contractor's own* QS/Engineer
  certifying the *Subcontractor's* measured progress against the *Subcontract's* BOQ.
- These two cycles are **not required to be on the same dates**, and in practice rarely are — a
  subcontractor's cut-off date is commonly set a week or two *before* the main contractor's cut-off, so the
  main contractor has time to consolidate subcontractor measurements into their own IPC to the client.

**Design implication:** don't model Subcontract IPC as a "child" of the main Contract's IPC. They're
siblings that both reference the same underlying WBS/measurement reality but run on independent
`PeriodStart/End` cycles, as already noted in §6.

### "Pay-when-paid" / "pay-if-paid" — the clause that actually governs subcontractor cash flow
Extremely common in real contracts (and heavily regulated/restricted in some jurisdictions — worth a
disclaimer that this varies by local law, e.g. many US states and UK's Construction Act restrict or ban
strict pay-if-paid clauses):
- **Pay-when-paid**: the main contractor must pay the subcontractor within N days of being paid by the
  client (or a fallback long-stop date regardless) — a timing mechanism, not a condition of payment.
- **Pay-if-paid**: the main contractor's obligation to pay is *conditional* on receiving payment from the
  client at all — much more contractor-favorable, and unenforceable outright in several jurisdictions.

**Data model implication:** `Subcontract.PaymentLinkageType` (PayWhenPaid / PayIfPaid / Independent) and,
if PayWhenPaid, a `PaymentLinkageDays` — and the Subcontract IPC's due date should reference the linked
Main Contract IPC's actual receipt date, not just a flat N-days-from-certification rule like the main
contract typically uses. This is a genuinely different due-date calculation from the main Contract IPC, not
a copy-paste of the same formula.

### Variation Orders flow the same direction, with a margin step in between
When the Client issues a VO on the main Contract, and the scope is subcontracted:
1. Client VO priced/approved on the main Contract (§4)
2. Main Contractor issues a **back-to-back VO on the relevant Subcontract(s)** for the portion of that
   scope the subcontractor will execute
3. The subcontract VO's rates are **not** simply the main contract VO's rates passed straight through — the
   main contractor typically strips out their own margin/overhead before passing a rate down (or applies a
   different agreed subcontractor rate entirely, sourced from the subcontract's own rate schedule where one
   exists for that scope)

**Design implication:** a `VariationOrderLine` on a Subcontract can optionally reference the
`MainContractVariationOrderId` it flows from (traceability/audit), but must carry its **own** negotiated
rate/value — never a formula-derived pass-through by default. If you want a "suggest subcontract VO value
from main VO value minus margin %" convenience feature, treat it as a default the user can override, not an
enforced calculation.

### Claims flow the same way, and this is where most contractor cash-flow pain actually happens
A delay caused by the Client (e.g. late site handover, late instructions) → Main Contractor claims EOT +
cost from Client → if that delay also impacted a subcontractor's work, the Main Contractor should in turn
grant/support a back-to-back EOT claim from that Subcontractor. In practice, many main contractors are slow
or reluctant to pass this through (it costs them money/time to concede), which is exactly why real systems
track claims **per commercial document** (Contract or Subcontract) independently — the two claims are not
guaranteed to be granted the same days/amount, and a report showing the *gap* between what the Main
Contractor claimed from the Client vs. what they've conceded to Subcontractors is a genuinely useful piece
of enterprise reporting (it's a real, common source of disputes and margin erosion).

---

## 7. Suggested build order (next slices)

Given everything above references BOQ lines and the Contract you've already built, a sensible sequence is:

1. **Site Progress / Measurement** — nothing else can be built without it; also finally makes the
   `IsBillingElement` WBS flag load-bearing, closing a deferred item. Build it polymorphic over "commercial
   document" (Contract or Subcontract) from day one — see §6c — or you'll be reworking it when Subcontracts
   arrives at step 6.
2. **IPC** — directly consumes Measurement; unblocks the Finance AR gap your README flags. Same
   polymorphism note applies.
3. **Retention** — small, mostly derived from IPC + Contract terms; low effort once IPC exists.
4. **Variation Orders** — can technically be built in parallel with (1)–(3) since it modifies BOQ/WBS
   rather than depending on measurement, but sequence it after IPC so you can decide the "Instructed vs
   Priced" question with real IPC behavior in front of you.
5. **Extension of Time / Claims** — build alongside or right after Variation Orders; both are
   "contract-change" documents with a similar approval workflow, and EOT's Liquidated-Damages linkage needs
   IPC's deduction line (§6b) to already exist.
6. **Subcontracts** — reuses the Measurement/IPC shapes from (1)–(2) and the VO/Claims shapes from (4)–(5),
   so building it last means you're generalizing proven patterns (and wiring the back-to-back linkage in
   §6c) rather than guessing upfront whether they need to be polymorphic.

---

## 8. Open decisions to make explicit (don't let Claude Code guess silently)

- Does Measurement/IPC apply to **all** WBS elements or only `IsBillingElement`-flagged ones? (§2)
- Flat retention % vs tiered retention rules? (§5)
- Two-speed VO workflow (Instructed → Priced → Approved) or simple Draft → Approved? (§4)
- Are Measurement/IPC/Retention modeled against `ContractId` only, or polymorphically against any
  "commercial document" so Subcontracts can reuse them without duplication? (§6)
- Does IPC certification create the AR invoice immediately, or a separate WIP/unbilled-revenue step first?
  (§3 — depends on which market/accounting standard you're targeting)
- Is EOT/Claims a separate document type from Variation Order, or folded into it as a claim type? (§6b —
  recommend separate, per the reasoning given)
- Are Measurement/IPC modeled polymorphically over Contract-or-Subcontract from the start, so Subcontracts
  (§6) doesn't require reworking them later? (§6c)
- What payment linkage does your target subcontracts use — pay-when-paid, pay-if-paid, or independent — and
  is this configurable per subcontract or a fixed company policy? (§6c — also check what's actually
  enforceable in your target jurisdiction before defaulting to pay-if-paid)

Each of these is the kind of thing your README already handles well elsewhere (explicitly disclosing
deferred decisions) — worth resolving or explicitly deferring before implementation starts, rather than
letting an LLM pick silently mid-build.
