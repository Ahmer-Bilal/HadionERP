# Procurement

Procurement owns the full procure-to-pay chain — Purchase Requisition to RFQ to Purchase Order to Goods
Receipt, three-way matched against the resulting AP invoice — gated by Vendor Prequalification and a real
budget check against Finance. Only the first slice, Vendor Prequalification, is built so far; the rest of
the chain is designed but not yet implemented.

## Why Prequalification comes before anything else

A vendor isn't simply "approved" or "not approved" in this module — they're qualified for a specific
combination of role and trade. A company holding both a Subcontractor and a Supplier relationship, or a
Subcontractor qualified for Electrical work but not Concrete work, needs each of those combinations
certified separately, mirroring the same "same role, different trade" shape Business Partner roles already
use in Master Data. This is deliberately the first real multi-step approval workflow anywhere in the
system — every business object built before it used a single approval step, but a real prequalification
needs Commercial, Legal, Technical, HSE, and Quality to each sign off independently, as genuinely separate
departments rather than one approver wearing five hats. One Government Authority partner role is
deliberately excluded from this process entirely, since there's no commercial relationship with a
government body to qualify in the first place.

A prequalification's validity period is set once, at the moment its final approval step completes, from a
configured number of months rather than a hardcoded one — and once set, it's never recalculated later, so
a future change to that configured period never silently shifts an already-approved certificate's expiry
date out from under it.

## What's still ahead

The Purchase Requisition, RFQ, Purchase Order, Goods Receipt, and the three-way match against Accounts
Payable are all designed in the roadmap but not yet built. Two things worth keeping in mind once that work
starts: a Purchase Order is a genuinely different document from a Subcontract even though both are
procurement-shaped and both eventually reference a WBS element — a PO doesn't carry retention or its own
IPC billing cycle the way a Subcontract does, so don't be tempted to unify them into one type. And once
Goods Receipt exists, it's the first half of the Materials/Warehouse flow described in
`docs/architecture/07-integrated-project-controlling.md` §2 — receiving material into a warehouse is a
Procurement-owned event, while issuing that material out to a specific project's WBS element is a separate,
later event that a future Warehouse module owns, not Procurement itself.
