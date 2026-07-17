# Glossary

Plain-language explanations of the business terms you'll encounter in HadionERP, especially the
construction-industry terms that aren't obvious if you're coming from a general business background. This
is written for end users — for the technical/data-model meaning of any of these terms, see the matching
`docs/module/*.md` file instead.

**Project** — the overall piece of work agreed with a customer; everything else (contracts, costs,
progress, billing) happens underneath a project.

**WBS (Work Breakdown Structure)** — how a project is broken down into smaller, manageable parts
internally, arranged as a hierarchy (for example, a project might break down into Substructure and
Superstructure, each of those breaking down further into specific elements). You generally won't create
WBS elements yourself unless you're setting up a new project — but every cost, every BOQ line, and every
progress measurement you see elsewhere in the system ultimately points back to a specific WBS element,
which is what lets the system tell you exactly where a cost or a piece of progress belongs.

**BOQ (Bill of Quantities)** — the priced, itemized list of everything a Contract or Subcontract covers —
each line has a description, a quantity, a rate, and an amount (quantity × rate). This is the detailed
pricing breakdown behind a contract's total value.

**Contract** — the commercial agreement with your customer for a project, including its BOQ, payment
terms, and key dates.

**Subcontract** — the equivalent agreement with a subcontractor who will execute some or all of the actual
work on your behalf, with its own pricing, retention, and payment terms — generally stricter than what your
own customer contract gives you, since a company typically doesn't pass better terms downstream than it
received itself.

**Site Progress / Measurement** — the record of how much physical work has actually been completed,
checked and agreed period by period (usually monthly) between your team and the customer's Engineer or
Consultant. This is the evidence behind every bill you send — you can't invoice for work that hasn't been
measured and agreed first.

**IPC (Interim Payment Certificate)** — your periodic invoice to the customer (or, for a subcontractor,
their invoice to you), calculated from certified progress, minus retention, minus recovery of any advance
payment already received. This is how a construction company actually gets paid over the life of a project,
rather than one lump sum at the end.

**Retention** — a percentage of each payment your customer holds back as security until the work is fully
and satisfactorily complete (and, typically, again until the Defects Liability Period has passed without
issues). It isn't lost money — it's released back to you later, usually in one or two milestone payments.

**Advance Payment** — money your customer pays you upfront (often to help mobilize onto the project), which
then gets gradually deducted back from your regular IPCs over the life of the project, rather than repaid
in one go.

**Defects Liability Period** — the window of time after the work is substantially finished during which
you remain responsible for fixing any defects that appear, before the project is considered fully closed
out and final retention is released.

**Variation Order (VO)** — a formally agreed change to the scope of work, which changes what the contract
is worth and, often, how long it will take.

**Extension of Time (EOT)** — a formal claim for additional time to finish the project, due to a delay that
wasn't your company's fault (bad weather, late instructions from the customer, and so on). This is kept
separate from a Variation Order because a delay can grant extra time without any extra money, or extra
money without any extra time — they're genuinely different things even though they can happen together.

**Back Charge** — a cost deducted from what you owe a subcontractor, for example because they damaged
something or didn't return unused materials — recorded against their Subcontract and reflected in what
they're actually paid.

**Draft / Submitted / Approved** — the standard journey almost every document in this system goes through:
you're still working on it (Draft), you've asked someone to review it (Submitted), and it's been formally
signed off (Approved) or sent back to you with a reason (Rejected).
