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

## Deferred (disclosed, not hidden)

- AP Invoice — the other half of the Phase 1 exit criteria ("...and an AP invoice end-to-end") — next.
- Document Splitting, Parallel Ledgers, Controlling objects beyond a flat Cost Center reference, Budget
  Control, Results Analysis/Settlement to CO-PA — all real, all in
  docs/architecture/07-project-accounting-and-financial-architecture.md, all genuinely later work. Phase 1's
  exit bar is "post/reverse a GL journal and an AP invoice end-to-end with full audit trail," not the full
  module vision in that document.
- No dual-approval matrix (e.g. a second approval step for entries above a threshold) — Phase 1 uses one
  Any-quorum step for every entry, same as every Master Data slice; a real matrix is configuration once
  Finance has more than one approval path to choose between.
- No period-close / posting-period lock — an entry can be posted with any `PostingDate`, past or future,
  with no check against an open/closed fiscal period. Real for Phase 1's exit bar, not for a production
  close process.
- G/L Account/Cost Center hierarchy cycle validation still doesn't exist (see Modules.MasterData/README.md's
  own deferred item) — a cyclic parent chain would affect any future roll-up reporting Finance builds on
  top of the chart, not anything in this slice today.
- Real authentication: `JournalEntriesController` hardcodes `"system/ui"`/`"system/approver"`, same
  deferred item as every Modules.MasterData controller.
