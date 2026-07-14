# Modules.Finance

One Universal-Journal-style line-item store covering General Ledger, Accounts Payable, Accounts Receivable,
Fixed Assets, and Cash & Bank (these are document types producing lines into the same store, not separate
sub-ledgers). Also owns: Document Splitting (real-time dimensional balancing), Parallel Ledgers (IFRS +
Saudi statutory/Zakat basis, posted simultaneously), Controlling objects (Cost Centers, Internal Orders,
Profitability Segments), Budget Control, and **Results Analysis (percentage-of-completion revenue
recognition) + Settlement to CO-PA** run against WBS elements each period-close.

See `docs/architecture/07-project-accounting-and-financial-architecture.md` for the full, source-checked
rationale (SAP-referenced). That document describes where this module is going; this README tracks what's
actually built today.

## What's built (Phase 1, slice 1: GL Journal Entry)

- **Domain**: `JournalEntry` — the first Finance Business Object, and the piece the Phase 1 exit criteria
  names directly: "post/reverse a GL journal ... with full audit trail" (docs/architecture/06-roadmap.md).
  `PostingDate`, `Description`, a child collection of `JournalLine`s (GLAccountId, optional CostCenterId,
  DebitAmount, CreditAmount, LineDescription — exactly one of Debit/Credit must be positive per line, the
  standard double-entry convention). `TotalDebits`/`TotalCredits`/`IsBalanced` are computed, never stored —
  a journal entry balances or it doesn't, there's nothing to persist. This is **the first real use anywhere
  in this codebase of the full Draft → Submit → Approve → Post → Reverse lifecycle**
  (`Platform.Core.Lifecycle.LifecycleEngine` supported Post/Reverse since Phase 0, but every Modules.MasterData
  slice stops at Approved — nothing had exercised that path until now).
  - **The balance check is not a configurable business rule** — CLAUDE.md's "don't hard-code business
    rules... that should be configuration" doesn't apply here. Total debits equal total credits is the
    accounting identity double-entry bookkeeping is built on; `Post()` refuses to post an unbalanced entry,
    and `JournalEntryService.SubmitAsync` checks the same thing earlier so an entry that can never post
    doesn't waste an approver's time.
  - **Reversal** does two things, not one: the original entry transitions Posted → Reversed (it is never
    edited or deleted — full audit trail, same "correct by reversal" principle as everywhere else in this
    platform), and a brand-new mirror entry is created with every line's Debit/Credit swapped, so the
    ledger's actual balance is undone, not just the original's status flag. Building that mirror (a new
    document number, its own audit trail) is orchestration, so it lives in
    `JournalEntryService.ReverseAsync`, not on the `JournalEntry` entity itself — see that method's doc
    comment for why the mirror is driven straight to Posted without a second human approval (same
    reasoning as SAP's FB08 "reverse document").
- **Application**: `JournalEntryService` validates every line's G/L Account (and optional Cost Center)
  reference through **`Modules.MasterData.Contracts`** — `IGLAccountLookup`/`ICostCenterLookup` — before
  the line is ever added, never through Modules.MasterData's own Domain/Infrastructure/Application
  directly (docs/architecture/01-architecture-foundation.md §3.2's Contracts-package rule, actually
  exercised for the first time by this slice — every prior module was Modules.MasterData talking to
  itself). Same Security (Maintainer vs. Approver + SoD conflict rule) and Workflow (one-step Any-quorum)
  wiring pattern as every Master Data slice; `Post` and `Reverse` both require the Approver privilege
  (posting is the point of financial effect, at least as sensitive as approving).
- **Infrastructure**: `FinanceDbContext` — this module's own **"finance" Postgres schema**, physically
  enforcing the module-boundary rule the same way `MasterDataDbContext` enforces "masterdata". Owns its
  own copies of `EfCoreNumberRangeService`/`EfWorkflowInstanceRepository` (near-duplicates of
  Modules.MasterData's own classes, backed by `FinanceDbContext` instead) rather than sharing MasterData's
  tables — a module cannot depend on another module's Infrastructure directly, so number ranges and
  workflow-instance persistence, both genuinely per-module concerns despite looking like generic kernel
  plumbing, get their own table in each module's own schema. See `NumberRangeCounterEntity`'s doc comment
  for the fuller version of this reasoning.
- **Api**: `JournalEntriesController` at `api/v1/finance/journal-entries` — CRUD-ish (Create/Get/List, no
  Update — an unbalanced Draft is fixed by re-creating, not editing lines after the fact in Phase 1) +
  `submit`/`approve`/`reject`/`post`/`reverse`.
- **Frontend**: `JournalEntriesPage.tsx` — list/create/details. The create form is a real line-item entry
  grid (G/L Account dropdown sourced from the existing Chart of Accounts list, Debit/Credit/Line
  Description per row, an "Add Line" button, and a live balanced/not-balanced indicator that disables
  Create until the entry actually balances) — the first screen in this application with a genuine
  multi-row child-entity input form, not just a flat field list. Own nav module ("Finance"), not bolted
  onto Master Data.
- Verified end-to-end: 24 new unit tests (balance validation, line-addition rules, the full
  Draft→Submit→Approve→Post→Reverse path, cross-module reference validation via fake lookups, SoD/security
  denial cases, the reversal mirror-entry mechanics) + 4 new integration tests against real PostgreSQL
  (entry+lines round-trip, RowVersion increments across three real transitions, cascade delete, a
  reversal's link to its original persists). Live `curl` exercise: created an unbalanced entry (allowed as
  Draft, correctly rejected on Submit with the exact imbalance amounts in the error), created a balanced
  entry and drove it through Submit → Approve → Post → Reverse, confirmed the mirror entry
  (`FIN-JE-2026-000003`) had Debit/Credit correctly swapped from the original and linked via
  `reversalOfEntryId`. Live Playwright pass in both English and Arabic (full RTL mirroring including the
  line-item grid's column order).

## What's built (Phase 1, slice 2: AP Invoice — Phase 1 Finance Core exit criteria complete)

- **Domain**: `APInvoice` — the other half of the Phase 1 exit criteria ("post/reverse a GL journal **and
  an AP invoice** end-to-end with full audit trail"). Vendor reference (validated as an actually-Approved
  Vendor/Both partner via `IBusinessPartnerLookup` — an invoice can't be raised against an unapproved or
  non-vendor partner), the vendor's own `VendorInvoiceNumber`, `NetAmount`, and an explicitly-chosen
  Expense account + Payable account (rather than a configured "AP control account" default — see the
  class doc comment for why: a real default needs an admin config screen this application doesn't have
  yet, and a guessed default with no admin behind it would be worse than making the choice explicit). An
  optional Tax Code **snapshots** the code's rate into `TaxRate` at creation — a later rate change never
  retroactively changes an already-created invoice, the standard "freeze financial facts at the moment of
  the transaction" pattern. `TaxAmount`/`GrossAmount` are computed, never stored, same "computed, not
  persisted" choice as `JournalEntry.IsBalanced`.
- **Application**: `APInvoiceService.PostAsync` generates a **real, separate, linked G/L Journal Entry**
  (Dr Expense, Dr VAT if any, Cr Payable — always balanced by construction since Gross ≡ Net + Tax) by
  calling `JournalEntryService.CreateSystemGeneratedAsync` — the exact same shared method
  `JournalEntryService.ReverseAsync`'s own mirror-entry logic was refactored into, once AP Invoice needed
  the identical "construct → validate lines → number → drive lifecycle, skip human approval" sequence a
  second time. `ReverseAsync` reverses the linked entry (via `JournalEntryService.ReverseAsync`) and the
  invoice itself — two independently-audited documents linked by `LinkedJournalEntryId`, not one document
  pretending to be both.
- **Api**: `APInvoicesController` at `api/v1/finance/ap-invoices` — Create/Get/List +
  `submit`/`approve`/`reject`/`post`/`reverse`, same pattern as `JournalEntriesController`.
- **Frontend**: `APInvoicesPage.tsx` — list/create/details, with a vendor dropdown (filtered to
  Vendor/Both partners), G/L account dropdowns for Expense/Payable/VAT, a Tax Code dropdown with a live
  Net/Tax/Gross preview as amounts are entered. Own nav Area under Finance, alongside Journal Entries.
- Verified end-to-end: 19 new unit tests (vendor/account/tax-code validation, tax-rate snapshotting, the
  full lifecycle, the generated posting's line count and balance with/without tax, reversal cascading to
  the linked entry) + 2 new integration tests against real PostgreSQL. Live `curl` exercise: rejected an
  invoice against an unapproved vendor, approved the vendor, created an invoice with 15% VAT
  (`FIN-AP-2026-000001`), drove it through Submit → Approve → Post, confirmed the generated
  `FIN-JE-2026-000004` had exactly Dr Expense 1000 / Dr VAT 150 / Cr Payable 1150 (balanced), reversed the
  invoice and confirmed the linked journal entry was reversed too, and created a second invoice with no
  tax code to confirm the two-line (no-VAT-line) posting path. Live Playwright pass in both English and
  Arabic.
- **This closes out the Phase 1 exit criteria for Finance Core** — a company can now maintain its chart of
  accounts and vendors, and post/reverse a GL journal and an AP invoice end-to-end with full audit trail,
  exactly as docs/architecture/06-roadmap.md's Phase 1 exit criteria states.

## Deferred (disclosed, not hidden)

- Document Splitting, Parallel Ledgers, Controlling objects beyond a flat Cost Center reference, Budget
  Control, Results Analysis/Settlement to CO-PA, AR/Cash-Bank — all real, all in
  docs/architecture/07-project-accounting-and-financial-architecture.md, all genuinely later work. Phase 1's
  exit bar (GL journal + AP invoice, both post/reverse-able with full audit trail) is now met; the full
  module vision in that document is not, and isn't meant to be yet.
- No dual-approval matrix (e.g. a second approval step for entries/invoices above a threshold) — Phase 1
  uses one Any-quorum step everywhere; a real matrix is configuration once Finance has more than one
  approval path to choose between.
- No period-close / posting-period lock — an entry or invoice can be posted with any date, past or future,
  with no check against an open/closed fiscal period. Real for Phase 1's exit bar, not for a production
  close process.
- No configured default AP control/VAT accounts — the invoice creator explicitly picks the Expense/Payable/
  VAT accounts on every invoice (see `APInvoice`'s own doc comment). Revisit once a real admin
  configuration screen exists to safely set a Platform.Configuration default without guessing at a GUID no
  admin actually chose.
- No duplicate vendor-invoice-number check (a real AP process would flag "this vendor's invoice #456 was
  already entered" to prevent double payment) — genuinely useful, genuinely deferred, not built because it
  wasn't asked for and Phase 1's exit bar doesn't require it.
- G/L Account/Cost Center hierarchy cycle validation still doesn't exist (see Modules.MasterData/README.md's
  own deferred item) — a cyclic parent chain would affect any future roll-up reporting Finance builds on
  top of the chart, not anything in this slice today.
- Real authentication: both Finance controllers hardcode `"system/ui"`/`"system/approver"`, same deferred
  item as every Modules.MasterData controller.
