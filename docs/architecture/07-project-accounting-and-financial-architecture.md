# 07 — Project Accounting & Financial Architecture (SAP-Referenced)

This document is the correction and deep-dive that supersedes the shallow "GL/AP/AR/Assets module" framing
in earlier drafts. Project Management is this platform's highest-priority capability, and the hardest part
of a construction ERP is not tracking a budget — it is getting **revenue recognition, multi-dimensional
profitability, and audit-defensible cost/revenue postings** right. SAP S/4HANA's Finance + Project System
(PS) + Controlling (CO) architecture is the proven reference for this, and this doc adopts its structure
deliberately rather than inventing a simplified alternative.

Sources consulted: SAP Help Portal and SAP-PRESS documentation on the Universal Journal, Document Splitting,
Parallel Ledgers, Project System (WBS/Networks), and Results Analysis/Settlement (see links at the end of
each section).

## 1. The Universal Journal — one ledger, not four

**What SAP actually does**: since S/4HANA, General Ledger, Controlling, Asset Accounting, and Profitability
Analysis (CO-PA) all write to a single line-item table (`ACDOCA`) instead of four separate reconciled
tables. This is *the* defining architectural decision of modern SAP Finance — it eliminates the
reconciliation problem between "what the GL says" and "what Controlling says" because there is only one
place where a financial fact is stored.

**What our earlier draft got wrong**: `Modules.Finance` was described as GL + AP + AR + Assets as
effectively separate, siloed sub-ledgers that "post into" a GL. That reintroduces the exact reconciliation
problem the Universal Journal exists to eliminate.

**Corrected design**: `Modules.Finance` owns one **Journal Line Item** store. Every financial fact — a
vendor invoice, a customer invoice, a depreciation run, a payroll posting, a project cost, a profitability
segment result — is a row in the same store, carrying:

| Dimension on every line | Purpose |
|---|---|
| GL Account | Classical chart-of-accounts posting |
| Company / Ledger | Which legal entity and which accounting principle (see §3) |
| Cost Center | Where the cost/revenue is organizationally owned |
| Internal Order (optional) | Temporary cost collector (e.g. a marketing campaign, an internal project) |
| WBS Element (optional) | The project cost/revenue object (see §4) |
| Profitability Segment | Customer / product / project / region combination for margin reporting (CO-PA) |
| Functional Area | Cost-by-function classification (production, admin, selling) for functional P&L |
| Document type & reference | Origin document (PO, GRN, Invoice, Payroll Run, Settlement run…) |

`AP`, `AR`, `Assets`, `Cash/Bank` are not separate ledgers in this design — they are **sub-processes that
produce journal lines** into the same store (an AP invoice is a document type, not a separate table of
truth). This is what makes trial balance, project cost report, and profitability report all reconcile by
construction instead of by month-end reconciliation effort.

*Ref: [SAP Universal Journal (ACDOCA)](https://blog.sap-press.com/what-is-saps-universal-journal),
[Universal Journal overview](https://community.sap.com/t5/enterprise-resource-planning-blog-posts-by-members/all-you-need-to-know-about-universal-journal-acdoca-sap-s-4-hana-2020/ba-p/13545279)*

## 2. Document Splitting — real-time balancing by dimension

**What SAP actually does**: when a document is posted (e.g. a vendor invoice with one expense line touching
Profit Center A), the tax and payable lines don't naturally carry a profit center. Document Splitting is a
rule-based engine that derives/splits those "non-assigned" lines automatically so that **every line of every
document balances by any reporting dimension** (profit center, segment) — so a balance sheet can be produced
per profit center/segment/branch in real time, not through period-end allocation runs.

**Corrected design**: `Modules.Finance` includes a **Posting/Splitting Engine** (`Platform.Core`'s generic
posting pipeline + Finance-specific splitting rules) that runs on every journal line write: it inspects the
"lead" dimension of a document (e.g. the WBS element or cost center on the expense line) and derives the same
dimension onto tax/payable/receivable lines per configured splitting rules. This is a **platform posting
service**, not something each module (Procurement, Construction, Payroll) reimplements — any module that
produces a financial document calls the same posting API and gets balanced-by-dimension output for free.

*Ref: [Document Splitting — SAP Help](https://help.sap.com/docs/SAP_S4HANA_CLOUD/0fa84c9d9c634132b7c4abb9ffdd8f06/4911c9cc2a934a18e10000000a42189b.html)*

## 3. Parallel Ledgers — one posting, multiple accounting principles

**What SAP actually does**: a single business transaction is posted once, but the system maintains a
**leading ledger** (typically the group/IFRS view) and one or more **non-leading ledgers** for other
accounting principles (local statutory GAAP, tax basis), each able to hold different valuations
(depreciation methods, provisions, revenue recognition timing) for the same underlying transaction.

**Why this matters for KSA specifically**: many construction/finance groups here need **IFRS** books for
group/lender reporting and **Saudi statutory/Zakat-basis** books for ZATCA — today often done as an
Excel-based bridge outside the ERP. Modeling this natively means:

- Every company code is assigned a leading ledger (IFRS) and can be assigned additional non-leading ledgers
  (Zakat/statutory basis) at setup time — configuration, not a code fork.
- Ledger-specific postings (e.g. a different depreciation charge or a different revenue-recognition
  adjustment under local vs IFRS rules) are separate, ledger-scoped journal lines against the *same*
  underlying business transaction, not a separate reconciliation spreadsheet.

*Ref: [Parallel Ledgers — SAP Help](https://help.sap.com/docs/SAP_S4HANA_ON-PREMISE/651d8af3ea974ad1a4d74449122c620e/f94fd7531a4d424de10000000a174cb4.html)*

## 4. Project Structure — WBS and Networks (replaces the earlier ad-hoc "Contracts/BOQ" list)

**What SAP actually does** (Project System, PS): a project is a **Project Definition** containing a
hierarchical **Work Breakdown Structure (WBS)**. Each **WBS element** is simultaneously:
- a node in the cost/schedule hierarchy, and
- a full **Controlling object** in its own right — meaning cost, budget, revenue and (optionally) commitment
  can post directly against it, the same way they can against a cost center.

Each WBS element carries one or more flags:
| Flag | Meaning |
|---|---|
| Planning element | Costs/revenues can be *planned/budgeted* against it |
| Account assignment element | Actual costs can be *posted* against it |
| Billing element | Revenue/billing is recognized against it (usually higher-level WBS nodes) |

**Networks** are a separate structure describing the *time sequence and dependencies* of activities
(scheduling, resource assignment, material procurement triggers) and are linked to WBS elements — a WBS
element can have many network activities from possibly different networks, but each activity belongs to
exactly one WBS element. This cleanly separates "what does this cost and who owns it" (WBS/Controlling) from
"when does it happen and what does it depend on" (Networks/scheduling) — exactly the split
`Modules.ProjectManagement` (scheduling) vs `Modules.Construction` (commercial layer) should mirror.

**Corrected module boundary**:

| Module | Owns (corrected) |
|---|---|
| **ProjectManagement** | Project Definition, **WBS Elements** (the controlling/cost-revenue backbone — shared by every project-based module), **Networks/Activities/Milestones** (scheduling, dependencies, resource & equipment allocation) |
| **Construction** | The construction-industry commercial layer that *references* WBS elements: Customer Contracts, BOQ (bill of quantities mapped onto WBS elements), Subcontracts (procurement documents assigned to WBS elements), Site Progress/Measurement, Variation Orders (which modify BOQ/WBS budget), Retention terms |
| **Finance** | Results Analysis and Settlement (§5) — the period-end process that turns WBS actual/planned cost into recognized revenue, COGS, and reserves, posted into the Universal Journal and Profitability Analysis |

This means a WBS element, once created by ProjectManagement, is the *single* object every other module
posts cost/revenue against — Procurement posts PO/GRN cost to a WBS element, Payroll posts labor cost to a
WBS element, Construction's Variation Orders adjust a WBS element's budget — instead of each module
inventing its own "project cost" concept.

*Ref: [WBS and Network Structures in SAP PS](https://blog.sap-press.com/wbss-and-network-structures-in-saps-project-system),
[PS – WBS element, SAP Help](https://help.sap.com/docs/SAP_S4HANA_ON-PREMISE/d3a3eb7caa1842858bf0372e17ad3909/00f269f55c834e659b4e038f2e3831ed.html)*

## 5. Results Analysis (Percentage-of-Completion) and Settlement — the core construction capability

This is the single most important financial process for a construction ERP, and was entirely missing from
the earlier draft.

**What SAP actually does**:
1. **Results Analysis (RA)** runs periodically (usually monthly) per WBS element. Using the cost-based
   percentage-of-completion method: `POC = Actual Cost / Planned Cost`. RA then calculates, per WBS element,
   for the period: valuated actual revenue, cost of sales (COGS), and — if the project is expected to make a
   loss — a **reserve for imminent loss**.
   - Where planned cost/POC cannot be reliably estimated (common on early-stage or disputed contracts), the
     platform must support the **cost-recognized method** (recognize revenue equal to recoverable cost
     incurred) as required under **IFRS 15** for such cases — not just the POC method.
2. **Settlement** then takes the RA results and:
   - posts revenue/COGS/reserves to the **Universal Journal** (Finance, §1) and Profit Center Accounting, and
   - settles the profitability result (by project/customer/segment) to **CO-PA** (Profitability Analysis),
   so that the profit shown in Finance and the profit shown in project/customer profitability reporting are
   *guaranteed to reconcile* — they are two views over the same settled amounts, not two independently
   computed numbers.

**Corrected design**:
- `Modules.Finance` owns a **Results Analysis engine** (configurable RA method per project type: cost-based
  POC, or cost-recognized for high-uncertainty contracts) that runs against WBS elements' actual vs. planned
  cost/revenue data (sourced from ProjectManagement/Construction).
- `Modules.Finance` owns a **Settlement engine** that posts RA output into the Universal Journal (revenue,
  COGS, WIP/reserve balance sheet lines) and into **CO-PA** (profitability segment = project × customer ×
  region, at minimum), each period-close.
- Retention (commonly 5–10% withheld per certified payment in KSA construction contracts) is modeled as a
  Finance AR/AP sub-process against the same WBS/contract billing element, not a bolt-on field on an invoice.
- Variation Orders (Construction) change a WBS element's planned cost/revenue, which directly feeds the next
  RA run — this is why Variation Orders must be WBS-aware, not a free-standing document.

*Ref: [Results Analysis, SAP Learning](https://learning.sap.com/courses/project-financials-control-in-sap-s-4hana/performing-results-analysis),
[POC Method Based on Project Progress](https://blogs.sap.com/2021/01/25/results-analysis-method-7-poc-method-based-on-project-progress-value-determination/),
[Project Settlement in S/4HANA Cloud](https://community.sap.com/t5/enterprise-resource-planning-blog-posts-by-sap/project-settlement-in-s-4hana-cloud-public-edition/ba-p/13692866)*

## 6. Controlling (CO) Objects — a cross-cutting concept, not a Finance-only concern

Cost Centers, Internal Orders, WBS Elements, and Profitability Segments are collectively **Controlling
objects**: anywhere the platform lets a user "assign a cost," the assignable target is one of these, and the
assignment is validated by `Platform.Core`'s posting API (§2) — every module (Procurement PO line, Payroll
run line, Construction subcontract line) assigns to a Controlling object, it does not invent its own
"which project/department does this belong to" field shape per module.

## 7. Summary of what changed from the original draft

| Original draft | Corrected (this doc) |
|---|---|
| Finance = separate GL/AP/AR/Assets modules that "post into" a GL | Finance = one Universal Journal (line-item store); AP/AR/Assets are document types producing lines into it |
| No mention of multi-dimensional real-time balancing | Document Splitting: every posting balances by profit center/segment in real time |
| Single accounting basis implied | Parallel Ledgers: IFRS + Saudi statutory basis posted simultaneously, natively |
| Construction module = flat list of documents (Contracts, BOQ, Subcontracts, Variation Orders, Retention) | ProjectManagement owns WBS (controlling backbone) + Networks (scheduling); Construction owns the commercial layer referencing WBS elements |
| No revenue recognition process described | Results Analysis (cost-based POC, with IFRS 15 cost-recognized fallback) + Settlement to Universal Journal and CO-PA, run per WBS element each period |
| Project profitability implied as "a report" | Profitability is a **settled, reconciled** result (CO-PA), not an independently computed report — it *is* the same number as what hits Finance |

This is the model the rest of the architecture (module boundaries in doc 01, Finance/Construction/
ProjectManagement scaffolding) is corrected to match.
