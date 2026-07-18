# Finance Mockup → Code Gap Analysis

Read-only audit. This document does not change any code — it maps each mockup image in this folder against
what actually exists today (domain object → API → frontend page), so we know exactly what has to be *coded*
before that screen can go live with real data, versus what already has a working object behind it and just
needs the screen itself (re)built.

Legend:
- 🟢 **Object exists, wired** — domain object + API + a working frontend page all exist today.
- 🟡 **Object exists, mockup not linked** — the real Business Object (domain/service/API/DB) exists and
  works end-to-end, but today's frontend page is a plain list/detail screen, not the richer mockup UI (KPI
  strips, document-flow trace, right-rail insights). The data to power the mockup mostly already exists; the
  screen doesn't.
- 🔴 **No object at all** — nothing in the domain, no service, no controller, no DB table. The mockup is
  pure UI; the "link" doesn't exist anywhere to wire to yet.

## Summary table

| # | Mockup screen | File | Domain object | API | Current page | Verdict |
|---|---|---|---|---|---|---|
| 1 | Finance Dashboard | `Finance_Dashboard.png` | — (aggregates others) | — | Generic `HomePage.tsx` (all departments, no KPIs/charts) | 🔴 |
| 2 | Chart of Accounts | `Finance_COA.png` | `GLAccount` (Modules.MasterData) | `GLAccountsController` | `GLAccountsPage.tsx` (plain table) | 🟡 |
| 3 | Journal Entry (detail) | `Finance_Jornal Entry.png` | `JournalEntry` / `JournalLine` | `JournalEntriesController` | `JournalEntriesPage.tsx` details view | 🟡 |
| 4 | Journal Entries (list) | `Finance_Jornal List.png` | `JournalEntry` | `JournalEntriesController` | `JournalEntriesPage.tsx` list view | 🟡 |
| 5 | Bank Reconciliation | `BankRec-.png` panel 3 | none | none | none | 🔴 |
| 6 | Budget Control | `BankRec-.png` panel 4 | none (`IBudgetCheckService` is a stub, see below) | none | none | 🔴 |
| 7 | Accounts Payable (workspace) | `BankRec-.png` panel 6 | `APInvoice`, `Payment` | `APInvoicesController`, `PaymentsController` | `APInvoicesPage.tsx`, `PaymentsPage.tsx` (separate plain tables, no combined workspace) | 🟡 |
| 8 | Accounts Receivable (workspace) | implied by panel 6's lower half | `ARInvoice`, `CustomerReceipt` | `ARInvoicesController`, `CustomerReceiptsController` | `ARInvoicesPage.tsx`, `CustomerReceiptsPage.tsx` (separate plain tables) | 🟡 |
| 9 | Income Statement (P&L) | `111b1373-...png` | none (derivable from `JournalLine` + `GLAccount.AccountType`) | none | none | 🔴 |
| 10 | Petty Cash Management | `27e9d73d-...png` | none | none | none | 🔴 |
| 11 | Trial Balance | `2b2e1bfd-...png` | none (derivable from `JournalLine` + `GLAccount`) | none | none | 🔴 |
| 12 | Cash Flow / Journal / Payment Voucher / Customer Receipt montage | `949844c6-...png` | `JournalEntry`, `Payment`, `CustomerReceipt` (Cash Mgmt overview itself: none) | existing controllers for panels 1,2,4,5; none for panel 3 | existing pages for panels 1,2,4,5; none for panel 3 | 🟡 (panels 1,2,4,5) / 🔴 (panel 3, Cash Management overview) |
| 13 | Period Closing Center | `d1f20165-...png` | none | none | none | 🔴 |
| 14 | Balance Sheet | `db0cd74e-...png` | none (derivable from `JournalLine` + `GLAccount`) | none | none | 🔴 |
| 15 | Financial Statements Dashboard | `f9146bdf-...png` | none (rolls up 9/11/14) | none | none | 🔴 |

## 🟡 Screens where the object exists but the mockup isn't linked

### Chart of Accounts (`Finance_COA.png`)
**Exists:** `GLAccount` (Modules.MasterData/Domain/GLAccount.cs) — `AccountCode`, `AccountName`,
`AccountNameArabic`, `AccountType`, `ParentAccountId` (self-referencing hierarchy), `IsPostable`,
`IsActive`, `NormalBalance` (derived). Full Draft→Submit→Approve lifecycle. `GLAccountsController` +
`GLAccountsPage.tsx` already list/create/browse these.

**Missing to match the mockup:**
- Stat cards (Total/Active/Header/Leaf/Inactive counts) — pure aggregation of data already there, no new
  fields needed.
- "Level" column and "Account Structure" donut (Level 1–4 breakdown) — `ParentAccountId` gives the tree,
  but nothing today walks it to compute a account's depth/level; needs a small computed-level helper.
- "Top Account Categories" donut and "Recently Updated Accounts" panel — category breakdown is derivable
  from `AccountType`; "recently updated" needs an `UpdatedAt`/`UpdatedBy` audit field (check
  `BusinessObject` base — if it doesn't track last-modified, that's a base-class gap, not just this screen).
  Cost Center / Currency columns shown in the mockup table aren't on `GLAccount` at all today.
- "Document Flow" right-rail (Create → Classify → Structure → Review → Active) — purely a UI presentation
  of the existing lifecycle states, no new object needed.

### Journal Entry detail (`Finance_Jornal Entry.png`)
**Exists:** `JournalEntry` (Modules.Finance/Domain/JournalEntry.cs) — full header + `JournalLine[]`
(`GLAccountId`, optional `CostCenterId`, Debit/Credit, line description), `IsBalanced`, `ReversalOfEntryId`,
full Draft→Submit→Approve→Post→Reverse lifecycle. `JournalEntriesController` + current
`JournalEntriesPage.tsx` details tab already render header fields + the line table + balanced/unbalanced
check.

**Missing to match the mockup:**
- "Document Flow" panel (PR → RFQ → Quotation → PO → Goods Receipt → Supplier Invoice → Payment) —
  `JournalEntry` has no `SourceDocumentType`/`SourceDocumentId` field at all today. The entry doesn't know
  it was raised *from* an AP Invoice, a Payment, an IPC, etc. — only the reverse link exists (`APInvoice.
  LinkedJournalEntryId`, `Payment.LinkedJournalEntryId`, `CustomerReceipt.LinkedJournalEntryId`). To render
  this panel for real, either add a back-reference field on `JournalEntry`, or the UI has to look the
  relationship up from whichever module owns it (AP/AR/Construction) — a design decision, not just a UI
  task.
- "Related Documents" / "Attachments" / "Notes" tabs — no attachment or notes entity exists anywhere in
  Finance yet (or, as far as this audit found, anywhere in the platform). Pure UI chrome today with no data
  behind it.
- Project column on journal lines (`PRJ-0001` in the mockup) — `JournalLine` only carries `GLAccountId` +
  `CostCenterId`, no `ProjectId`.
- "Created By" avatar + Posted By/Approved By people shown with names+roles — the audit trail exists as
  actor strings on the lifecycle transitions; needs surfacing, not new code.

### Journal Entries list (`Finance_Jornal List.png`)
**Exists:** same `JournalEntry` object; `JournalEntriesController.List` and the current list view already
page through entries with Document Number/Date/Description/Total/Status.

**Missing to match the mockup:**
- Status-count strip (All/Draft/Submitted/Posted/Reversed/Recurring) — pure aggregation of existing
  `Status`, except **Recurring has no backing concept at all** — no recurrence field/schedule anywhere on
  `JournalEntry`.
- "Source" column (Accounts Payable / Payment / Bank Reconciliation / Manual / Construction Billing /
  Payroll / Fixed Asset) — same gap as the Document Flow panel above: nothing on `JournalEntry` records
  what created it. Several of those sources (Bank Reconciliation, Payroll, Fixed Asset) also don't exist as
  objects yet (see below), so they can't produce entries yet regardless.
- Filters (Reference, Created By, date range beyond what's queried today), "Quick Reports" (Journal Entry
  Register / General Ledger / Trial Balance / Audit Trail) — General Ledger and Trial Balance are reports
  that don't exist yet (see Trial Balance below).

### Accounts Payable / Accounts Receivable workspaces (`BankRec-.png` panels 6 and the AR half)
**Exists:** `APInvoice` and `Payment` (AP side), `ARInvoice` and `CustomerReceipt` (AR side) — all full
Draft→Submit→Approve→Post→Reverse Business Objects with their own controllers and list/detail pages today
(`APInvoicesPage.tsx`, `PaymentsPage.tsx`, `ARInvoicesPage.tsx`, `CustomerReceiptsPage.tsx`).

**Missing to match the mockup:**
- The mockup shows AP and AR each as one combined *workspace* (Invoices + Payments tabs, aging summary,
  top suppliers/customers, outstanding totals) rather than today's four separate flat pages. This is
  screen composition over existing data, not a new object — `APInvoice.GrossAmount` minus its `Payment`
  allocations gives "outstanding," bucketed by `InvoiceDate` gives aging.
- "Overdue"/"Due This Week"/"Due This Month" status badges — needs a due-date field. Neither `APInvoice`
  nor `ARInvoice` currently stores a payment due date (only `InvoiceDate`) or credit terms to derive one
  from.

### Payment Voucher / Customer Receipt (`949844c6-...png` panels 4–5)
**Exists:** `Payment` and `CustomerReceipt` — both full objects with allocation children, linked
`JournalEntry` on posting, existing pages.

**Missing to match the mockup:**
- "Payment Flow" / "Receipt Flow" right-rail (Invoice → Approval → Bank Reconciliation) — same
  source-document-trace gap as the Journal Entry screen; also depends on Bank Reconciliation existing to
  show that final step meaningfully.
- Approval Status panel with named approver/approved-by — same audit-trail-surfacing gap as above (data
  exists on the lifecycle transitions, isn't rendered anywhere yet).

## 🔴 Screens with no object at all

These need real domain design (entities, invariants, lifecycle) before any frontend work is worth doing —
building the screen first would just be a mockup wired to nothing, which is explicitly what this document
is trying to avoid repeating.

- **Bank Reconciliation** (`BankRec-.png` panel 3) — needs a `BankReconciliation`/`BankStatementLine`
  concept: importing/entering a bank statement's lines and matching them against `Payment`/`CustomerReceipt`
  postings to the same `BankAccount`. Nothing today represents "a bank statement" or "a match" at all.
- **Budget Control** (`BankRec-.png` panel 4) — `IBudgetCheckService` (Modules.Finance/Contracts) exists as
  a *contract* Procurement already calls before releasing a PO, but `PassThroughBudgetCheckService`
  (Infrastructure) is a stub that unconditionally returns `Allowed = true` — there is no `Budget` entity,
  no per-cost-center/per-fiscal-year amounts, nothing to actually check against. This is the single
  clearest "wire exists, nothing on the other end" case in the whole audit — the plug is there, waiting.
- **Petty Cash Management** (`27e9d73d-...png`) — no Cash Management concept beyond `BankAccount`/`Payment`.
  Needs a `PettyCashBox` entity (opening/current balance, responsible employee, per-project) and an
  `EmployeePettyCash` allocation concept, both currently absent.
- **Trial Balance** (`2b2e1bfd-...png`) — no new entity needed, but no report/service exists either. This
  is a pure query over existing data: sum `JournalLine.DebitAmount`/`CreditAmount` grouped by `GLAccountId`
  for Posted `JournalEntry` rows in a period, then roll up through `GLAccount.ParentAccountId`. Cheapest
  🔴 to close, since both source tables already exist.
- **Income Statement / Balance Sheet** (`111b1373-...png`, `db0cd74e-...png`) — same story as Trial
  Balance: no new entity, but the reporting service that classifies `GLAccount.AccountType` into
  Revenue/COGS/OpEx (P&L) or Assets/Liabilities/Equity (Balance Sheet) and nets/rolls them up per period
  doesn't exist. Depends on Trial Balance's aggregation existing first, effectively — same underlying query,
  different presentation.
- **Cash Flow Statement** (visible inside `f9146bdf-...png`, Financial Statements Dashboard) — same
  aggregation family, additionally needs Operating/Investing/Financing *classification* per account or
  transaction type, which nothing today captures.
- **Financial Statements Dashboard** (`f9146bdf-...png`) and **Finance Dashboard** (`Finance_Dashboard.png`)
  — both are rollups of the reports above (plus AP/AR aging, bank balances, budget usage). Cannot be built
  for real before Trial Balance/Income Statement/Balance Sheet/Budget Control exist; today's generic
  `HomePage.tsx` is department-agnostic tile navigation, not a Finance-specific KPI dashboard.
- **Period Closing Center** (`d1f20165-...png`) — needs a `FiscalPeriod`/`PeriodClose` concept: period
  open/locked state, a closing checklist with per-activity owner/status, and the ability to block postings
  into a locked period. Nothing today models a fiscal period at all — `JournalEntry.PostingDate` is a free
  date with no period concept enforcing it.

## Suggested build order

Purely a sequencing suggestion based on dependencies found above — not started, no code touched.

1. **Trial Balance** — no new entity, pure query over `JournalLine`/`GLAccount` already in the DB. Cheapest
   win, and everything else reporting-related depends on this aggregation existing.
2. **Income Statement + Balance Sheet** — same aggregation, add `AccountType` classification/period
   comparison. Natural follow-on to #1.
3. **Source-document trace field** on `JournalEntry` (or an application-layer lookup across AP/AR/
   Payment/Receipt) — unblocks the Document Flow panel on Journal Entry, Journal List's Source column, and
   Payment/Receipt Flow panels, all at once.
4. **Budget Control** — the contract (`IBudgetCheckService`) already exists and is already called by
   Procurement; only the real `Budget` entity + replacing `PassThroughBudgetCheckService`'s body is needed.
5. **Bank Reconciliation** — needed before Cash Management overview, Payment/Receipt Flow's final step, and
   before the Finance Dashboard's "Bank reconciliation pending" alert can mean anything real.
6. **AP/AR workspace composition** — no new backend, just combining existing `APInvoice`/`Payment` (and
   `ARInvoice`/`CustomerReceipt`) pages into the mockup's tabbed workspace, plus adding a due-date field for
   the aging buckets.
7. **Petty Cash Management** and **Period Closing Center** — newest concepts, most domain design needed,
   least depended-upon by anything else. Lowest urgency.
8. **Finance Dashboard + Financial Statements Dashboard** — build last; they're rollups of everything above
   and would otherwise just be more mockup wired to nothing.
