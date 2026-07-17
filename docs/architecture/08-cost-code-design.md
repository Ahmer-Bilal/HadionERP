# 08 — Cost Code Design

Cost Codes are named throughout `02-business-object-model.md`, `03-module-boundaries.md`, and
`07-integrated-project-controlling.md` as the dimension that answers "what kind of cost is this" —
independent of "which project is this" (the WBS element's job). This document is the full design for that
dimension itself: not yet built, scoped in `MISSING-FEATURES-AUDIT.md` §18 and the roadmap's Phase 3–4
checkpoint, and this is the spec to build it against once that phase starts.

## Why Cost Codes need to be their own structured list, not a free-text field

A company that only tags costs by WBS element can answer "how much did Project X cost." It cannot answer
"how much did we spend on rebar across every project this year," or "is our labor cost as a percentage of
total cost trending up," without a second, independent classification sitting alongside the project
dimension. That second classification is the Cost Code. Real construction ERPs and accounting standards
(SAP's Cost Elements, most industry Cost Code lists derived from or resembling CSI MasterFormat in the
US, or company-specific equivalents elsewhere) all converge on the same shape: a **hierarchical**, company-
maintained list — not a flat set of tags, and not free text, because free text can't be rolled up
consistently in a report and drifts in spelling/naming over time across different people entering it.

## The Cost Code hierarchy

A Cost Code is structured as a tree, the same general shape as a WBS element or a Chart of Accounts:

```
1000 — Materials
  1100 — Concrete & Aggregates
  1200 — Steel & Reinforcement
    1210 — Rebar
    1220 — Structural Steel
  1300 — MEP Materials
2000 — Labor
  2100 — Direct Site Labor
  2200 — Supervision
3000 — Equipment
  3100 — Owned Equipment (internal usage rate)
  3200 — Rented Equipment
4000 — Subcontracts
5000 — Overheads & Indirect Costs
```

Only **leaf-level** codes should ever actually be posted against by a real transaction — the same
principle a WBS element's `IsAccountAssignmentElement` flag already enforces one dimension over. A parent
code like `1000 — Materials` exists purely to roll its children up in reporting; nothing should be able to
post a cost directly against it. This mirrors the WBS element design deliberately, since it's the same
underlying problem solved the same way.

### Data model
- `CostCode` (master data, lives in Master Data alongside Chart of Accounts and Cost Centers, since it's
  shared reference data every module reads, not something any one operational module owns): `Code`, `Name`,
  `NameArabic?`, `ParentCostCodeId?`, `IsPostable` (the leaf/parent distinction above), `CostCategory`
  (Material/Labor/Equipment/Subcontract/Overhead — a broader grouping above the tree itself, useful for
  high-level reporting cuts that don't care about the detailed hierarchy), `IsActive`.
- Maintained through the same Admin Panel / Lookup-engine pattern already used for other controlled
  vocabularies (`docs/module/master-data.md`), not a hardcoded enum — a company's real Cost Code list
  grows and gets restructured over time, and shouldn't require a code deployment to change.

## Global list, not per-project

The Cost Code list is company-wide, not redefined per project. This is a deliberate choice, not a
simplification: the entire value of Cost Codes is being able to compare "labor cost" across every project
consistently — a per-project Cost Code list would make that comparison impossible, defeating the purpose.
A specific project simply won't use most of the codes in the full list; that's normal and doesn't need
special handling; it's exactly how a company-wide Chart of Accounts already works, and Cost Codes should be
treated the same way.

## How a real cost transaction gets its Cost Code

Per `02-business-object-model.md`'s core rule — transactional documents carry the references, master/
planning objects never reference each other — every future cost-producing document should carry **both** a
`WbsElementId` and a `CostCodeId`, populated as follows:

- **BOQ Line** (Construction): the Cost Code is typically implied by the nature of the line rather than
  separately picked — but since a BOQ line represents *revenue* (what the customer is billed), not cost, it
  doesn't strictly need a Cost Code at all. Cost Codes matter on the *cost* side (§ below), not the revenue
  side — worth being explicit about this so nobody adds a Cost Code field to BOQ Line under the mistaken
  assumption every construction document needs one.
- **Goods Issue** (future Materials/Warehouse module, `07-integrated-project-controlling.md` §2): the Cost
  Code should default from the Item/Material master data itself — a real Item master should carry a
  `DefaultCostCodeId` (e.g. every unit of "20mm rebar" defaults to Cost Code `1210 — Rebar`), so a site
  storekeeper issuing material doesn't have to manually classify every single transaction, only override the
  default in the rare case it's genuinely being used differently than usual.
- **Timesheet Line** (future Labor Costing, same doc §3): Cost Code should default from the employee's Job
  Grade or Trade (a general laborer's time defaults to `2100 — Direct Site Labor`; a site supervisor's
  defaults to `2200 — Supervision`), for the same reason — don't make a site engineer manually classify
  every timesheet line when the classification is almost always predictable from who the person is.
- **Equipment Usage Log** (same doc §4): defaults from the Equipment master record itself (an excavator
  always posts to `3100 — Owned Equipment`).
- **Subcontract payment lines**: default to `4000 — Subcontracts`, or a more specific child code if the
  company wants subcontract cost broken down further (e.g. by trade).

The consistent pattern: **Cost Code is almost always defaulted from whatever master data the transaction is
already referencing, never manually re-entered every time** — the one-time setup cost of putting a default
Cost Code on each Item/Job Grade/Equipment record is what makes day-to-day data entry fast and consistent,
instead of relying on every user to correctly classify every single transaction by hand.

## Cost Codes and Budget Control

Once real Budget Control is built (currently a disclosed pass-through stub, per `MISSING-FEATURES-AUDIT.md`),
it should check budget **per WBS element per Cost Code**, not just per WBS element alone. This is a
meaningfully different design than a simpler "does this WBS element have enough budget left" check: a
project might have plenty of overall budget remaining but be specifically over budget on Steel while under
on Labor, and a real Budget Control check needs to catch that, not just the project-wide total. This means
a project's Budget itself needs to be entered at the WBS-element-by-Cost-Code level, not as one lump number
per WBS element — worth deciding deliberately when Budget Control is actually built, since it changes the
shape of the budget-entry screen too, not just the checking logic.

## Reporting implications

Once Cost Codes exist and are consistently populated, two genuinely different report shapes become
possible from the same underlying transaction data, without needing two different systems to produce them:
a **project view** (group every cost transaction by WBS element, regardless of Cost Code — "how much did
Project X cost so far") and a **cost-type view** (group every cost transaction by Cost Code, regardless of
project — "how much did the company spend on rented equipment across everything this quarter"). This dual
reporting capability is the entire reason Cost Codes are worth building as their own dimension rather than
just adding more detail to the WBS hierarchy itself — a WBS hierarchy can only ever answer the first kind of
question well, never both.

## Build sequencing note

Cost Codes should be built **before** or **alongside** the first real cost-producing document that isn't
already built — Goods Issue, Timesheet, or Equipment Usage Log, whichever comes first in the roadmap's
Phase 3–4 checkpoint — never after, because retrofitting a Cost Code reference onto historical transaction
data once it already exists is far more expensive than including the field from day one. If Goods Issue or
Timesheet gets built without a `CostCodeId` field "to add later," that's a mistake worth catching in review
before it ships, not after.
