# Finance

Finance is built around a single idea SAP calls the Universal Journal: General Ledger, Accounts Payable,
Accounts Receivable, Fixed Assets, and Cash & Bank are not separate sub-ledgers that need reconciling
against each other — they're all just different document types producing lines into the same underlying
store. This module also owns the things that make project accounting real once they're built: Document
Splitting, Parallel Ledgers (IFRS and Saudi statutory/Zakat basis posted simultaneously, not as a
translation step after the fact), Controlling objects, Budget Control, and Results Analysis — the
percentage-of-completion revenue recognition run described in full in
`docs/architecture/07-integrated-project-controlling.md` §7, which is what eventually turns Construction's
IPCs and every other module's actual project cost into a monthly P&L that actually means something on a
long-running project.

## What a Journal Entry actually enforces

The one Business Object built so far is the GL Journal Entry, and it's worth understanding because it's
also the first place in this system a full Draft → Submit → Approve → Post → Reverse lifecycle actually
runs end to end — every module before this one stopped at Approved. A journal entry is a header plus a set
of lines, each line touching a G/L Account and optionally a Cost Center, with exactly one of Debit or
Credit populated per line. Whether the entry balances — total debits equal total credits — is never stored,
because it isn't a fact about the entry that needs persisting, it's a computed check run every time: an
entry either satisfies the accounting identity double-entry bookkeeping is built on or it doesn't, and
`Post()` simply refuses to post one that doesn't. This is one of the very few places in the system where a
business rule is deliberately hard-coded rather than made configurable, because it isn't a company policy
that could reasonably vary — it's the definition of what a balanced journal entry is.

`JournalEntry` also now knows what raised it: `SourceDocumentType`/`SourceDocumentId` (per
`UI/Finance/FINANCE-MOCKUP-GAP-ANALYSIS.md`'s build-order item 3), set once while Draft via
`MarkSourceDocument` and never changed afterward — a document's origin is a fact about its creation, not
something that should ever drift. `JournalEntryService.CreateAsync` (a human, through
`JournalEntriesController`) tags every entry `"Manual"`; `CreateSystemGeneratedAsync` — the one method
`APInvoiceService`/`ARInvoiceService`/`PaymentService`/`CustomerReceiptService`'s own `PostAsync` all call to
raise their linked posting — takes an optional `sourceDocumentType`/`sourceDocumentId` pair and each of
those four callers passes its own type constant (`JournalEntrySourceDocumentTypes`) and its own document's
`Id`. A reversal's mirror entry inherits the *original* entry's source rather than getting a `"Reversal"`
type of its own — a reversal of an AP-Invoice-raised entry still originated from that AP Invoice; `
ReversalOfEntryId` already answers "is this a reversal," so `SourceDocumentType` stays answering "what
business event caused this," not "how did this specific row come to exist." Live-verified over real HTTP:
created and posted a real AP Invoice, confirmed its linked Journal Entry carries
`sourceDocumentType: "APInvoice"` and `sourceDocumentId` equal to the invoice's own Id, then reversed the
invoice and confirmed the mirror entry inherits the same two fields. Entries created before this field
existed simply read back `null` — not backfilled, since there's no way to know their real origin after the
fact. The Journal List's Source column and the Journal Entry detail's real Document Flow rail (built in a
later slice, see the section below) both surface this.

## Journal Entry, fully rebuilt to match its mockups — the real "automatic vs. manual" distinction, and a
## real Document Flow chain

`UI/Finance/Finance_Jornal List.png` and `Finance_Jornal Entry_Object.png` are now built for real, not a
scoped-down slice — status-count strip, filters (date range/search/status/source/created-by), bulk
approve/reverse, and a genuinely rich detail view (Overview/Line Items/Attachments/Notes/History/Related
tabs), all working against real data, per an explicit user instruction to match 100% rather than the usual
disclosed-simplification default this codebase otherwise follows.

**Automatic vs. Manual is now real two levels deep, not just at the Journal Entry.** `APInvoice`/`ARInvoice`
gained their own `SourceDocumentType`/`SourceDocumentId` (mirroring `JournalEntry`'s own field exactly) —
`"Manual"` (an AP/AR clerk keyed it directly), `"PurchaseOrder"` (a real procurement-driven vendor invoice;
`CreateAPInvoiceRequest.PurchaseOrderId` lets a clerk reference the PO they're matching against — Finance
never validates that PO exists, since Finance is upstream of Procurement and must never depend on it; the
frontend's own already-published Procurement APIs are the only thing that ever resolves it), or `"Ipc"`/
`"RetentionRelease"` (Construction's own certification raised it automatically). `ICustomerInvoicingService`/
`IVendorInvoicingService` (the two cross-module write Contracts interfaces `IpcService`/
`RetentionReleaseService` call) now carry this origin through explicitly — every real caller states its own
source rather than the adapter guessing.

**`JournalEntryDocumentFlowService`** (new) resolves a Journal Entry's real chain — Source Document → this
entry → Reversal (if any) → Payment/Customer Receipt settlement (if the source is an invoice) — using
repository methods that already existed (`IPaymentRepository.ListByInvoiceAsync`,
`ICustomerReceiptRepository.ListByInvoiceAsync`). It deliberately stops at Finance's own module boundary: an
invoice's own `SourceDocumentType` (Ipc/RetentionRelease/PurchaseOrder) comes back as a raw type+id pair,
never resolved to a friendly document number/status server-side — resolving that would mean Finance reading
Construction's or Procurement's own data, which the module graph forbids in that direction. The **frontend**
resolves that one extra hop itself, calling Construction's (`ipcApi`/`retentionReleaseApi`) and Procurement's
(`purchaseOrderApi`/`requestForQuotationApi`/`purchaseRequisitionApi`/`goodsReceiptNoteApi`) own
already-published APIs directly — for a real Purchase Order, walking the full PO → RFQ → Purchase
Requisition chain upstream and finding any Goods Receipt Note downstream, all client-side composition, zero
new backend cross-module dependency. Live-verified over real HTTP: created an AP Invoice with a real
`purchaseOrderId`, posted it, and confirmed `/journal-entries/{id}/document-flow` returns exactly
`[PurchaseOrder → APInvoice(Posted) → JournalEntry(current, Posted) → Payment(Pending)]`.

**Attachments/Notes/History are real for the first time anywhere in this platform.** `Platform.Attachments`/
`Platform.Notes` were built in Phase 0 but never actually wired to a single screen until now —
`AttachmentsController`/`NotesController`/`AuditHistoryController` (new, in `Gateway.Api/Controllers/`
alongside `SystemController`, not inside any one module's own Api project, since these are generic
Platform-level services keyed by a flat `(businessObjectType, businessObjectId)` pair, not owned by
Finance) expose them for any Business Object, starting with Journal Entry. Live-verified: uploaded a real
PDF, listed it, downloaded it back byte-for-byte; added a real note; confirmed the History tab shows the
real audit trail entry `JournalEntryService.CreateAsync` already recorded.

**Deliberately not built this slice, disclosed rather than silently skipped:** the AP Invoice create
screen itself has no Purchase Order picker UI yet (the field works end-to-end via the API, just not yet
exposed as a dropdown); Reference is not a real separate field on `JournalEntry` today (the mockup shows
one distinct from Description) — search matches Description/Document Number instead of fabricating a
column with no backing data; and the List's own "Construction Billing" detection only happens in the
per-entry Document Flow (an N-query-per-row cost the flat list can't absorb for hundreds of rows), so the
List's own Source column shows the immediate source (Accounts Payable/Receivable/Payment/Customer Receipt/
Manual), not the deeper Ipc/RetentionRelease origin.

Reversal is worth understanding too, because it does two things, not one. The original entry moves from
Posted to Reversed — it is never edited or deleted, so the audit trail stays intact — and a brand-new
mirror entry is created alongside it with every line's Debit and Credit swapped, which is what actually
undoes the ledger's balance rather than just changing a status flag on the original. That mirror is driven
straight to Posted without a second human approval, the same way SAP's own document-reversal transaction
works — reversing something that was already properly approved once doesn't need a second approval to undo.

## AR Invoice — the customer-billing mirror of AP Invoice

`ARInvoice` closes the "Accounts Receivable as its own document type" half of this module's original gap —
same Draft → Submit → Approve → Post → Reverse lifecycle as `APInvoice`, same "pick the accounts explicitly,
no hidden defaults" design, same tax-rate-snapshot-at-creation pattern. Posting generates a real linked
Journal Entry, but the debit/credit direction mirrors AP's exactly: Dr Receivable (Gross), Cr Revenue (Net),
Cr VAT Output (Tax) if a Tax Code applies — the opposite ledger direction from AP's Dr Expense/Dr VAT
Input/Cr Payable, since AR credits the government VAT liability while AP debits the recoverable input VAT.
Only a Business Partner holding the `Client` role is eligible, the exact mirror of AP's vendor-family role
list (which itself excludes `Client`).

`OutstandingBalance` is simpler than AP's — it's just Gross Amount once Posted, with no reduction mechanism,
because there's no Customer Receipt Business Object yet to record that a customer actually paid (the same
gap `Payment` closed for AP, not yet closed on the AR side).

This module now **is** wired to Construction's IPC, closing PROGRESS.md's 2026-07-16 open question: this
`Contracts` package publishes `ICustomerInvoicingService`, implemented by
`Infrastructure.ArInvoiceCustomerInvoicingService` as a thin adapter around `ARInvoiceService.CreateAsync`
(so every validation rule — Client role, Approved status, active/postable accounts, VAT-account-required-
with-a-Tax-Code — lives in exactly one place, not duplicated for the cross-module caller). Construction's
`IpcService` calls this the moment a Contract-type IPC is certified, raising a real AR Invoice in Draft —
left there deliberately, not auto-posted, so a human in Finance still reviews/submits/approves/posts it.
This is the first cross-module *write* Contracts interface in the system; every earlier one
(`IAPInvoiceLookup`, `IBudgetCheckService`, and every other module's `I*Lookup`) is read-only.
`RetentionReleaseService` is now this interface's (and its AP-side mirror `IVendorInvoicingService`'s)
second caller — approving a retention release raises exactly the same kind of Draft invoice an IPC does,
same reasoning, just for the amount being released back rather than the amount currently billed.

## Financial statements — Trial Balance, Income Statement, Balance Sheet

Three read-only reports (`ReportsController` under `api/v1/finance/reports`), built per
`UI/Finance/FINANCE-MOCKUP-GAP-ANALYSIS.md`'s suggested build order (items 1–2, the cheapest gaps to close
since both needed nothing new in the domain — just a query over data `JournalEntry`/`GLAccount` already
produce). None of the three is its own Business Object; `TrialBalanceService` is the one real query
(`IJournalEntryRepository.ListPostedAsync` — every Posted entry with `PostingDate` on or before the report's
end date, Draft/Submitted/Approved/Rejected excluded since they never hit the ledger), and
`IncomeStatementService`/`BalanceSheetService` both call it directly as an intra-module dependency rather
than re-querying, the same "one module-internal caller reuses another service directly" precedent
`APInvoiceService`/`ARInvoiceService` already set. Trial Balance rolls each account's own Opening/Period
Debit-Credit totals up through `ParentAccountId` (deepest accounts folded into parents first, one pass, no
recursion) and also returns each account's hierarchy `Level`, the same computation `GLAccountsPage.tsx`
independently runs client-side for the Chart of Accounts screen's own Level column — two implementations of
the same algorithm, not one shared one, since one runs in C# over `IGLAccountLookup.ListAllAsync` and the
other in TypeScript over data the page already has in hand; if that duplication ever drifts, treat the
backend one as authoritative for report figures. Income Statement nets Revenue/Expense accounts to their
`NormalBalance` side for a period and totals to `NetProfit`; Balance Sheet nets Asset/Liability/Equity
accounts as of a date and — since there's no Period Closing Center yet to post a real closing entry into a
Retained Earnings account — computes cumulative Revenue-minus-Expense itself and appends it to
`EquityLines` as an unbacked line (`AccountId: null`), which is exactly the figure the accounting equation
(Assets = Liabilities + Equity) requires to balance for any correctly-posted ledger, not a fabricated
plug. Both statements accept an optional compare period/date and return a `Variance`/`VariancePercent` per
line when one is given. Live-verified against this dev database's real posted entries: Balance Sheet's
`TotalAssets` equals `TotalLiabilitiesAndEquity` exactly (13,045 = 13,045 as of this writing), confirming the
retained-earnings plug is computed correctly rather than just present.

## Chart of Accounts screen — closing the mockup gap

`GLAccountsPage.tsx` was rebuilt from a plain table into the mockup's richer workspace (per the gap
analysis's 🟡 COA entry): stat cards (Total/Active/Header/Leaf/Inactive), a hierarchy Level column and
Account Structure breakdown (computed client-side, see above), a Top Account Categories donut
(`AccountType` distribution), a Recently Updated panel, and the Document Flow rail as a pure presentation of
the existing Draft→Submit→Approve lifecycle. "Recently Updated" needed `GLAccount.ModifiedAt`/`ModifiedBy`
surfaced through `GLAccountDto` — the fields already existed on the `BusinessObject` base class (every
document has always tracked its own last-modified actor/timestamp) but `GLAccountService` never returned
them until now. A hard-delete action was also added (`GLAccountService.DeleteAsync`), gated on the same
Approve privilege (not just Maintain) this codebase already uses for Approve/Reject — a Maintainer alone can
never delete something they created — and only permitted while `CanHardDelete` (Draft, never submitted),
matching the platform's "correct by reversal, not by deletion" rule everywhere else; `UpdateAsync`'s
`IsActive` toggle remains the equivalent action once an account is Approved.

## Budget — real Cost Center/fiscal-year spending ceilings, closing the audit's own worked example

`Budget` (Draft → Submit → Approve/Reject, no Update — see its own doc comment for why a revision means
Reject-then-recreate rather than editing an approved ceiling in place) finally backs
`Modules.Finance.Contracts.IBudgetCheckService`, the exact cross-module contract call
`docs/architecture/01-overview.md` §3.2 names as its own worked example ("Procurement asks Finance's
IBudgetCheckService before releasing a PO") and which the gap analysis called "the single clearest
wire-exists-nothing-on-the-other-end case in the whole audit." `RealBudgetCheckService` (replaces the
deleted `PassThroughBudgetCheckService` stub) looks up the one Approved `Budget` for a Cost Center and the
current calendar year; no budget on file for that combination is opt-in-not-configured, so it's allowed
through, the same way an unset Cost Center on a Journal Line is legal — budget control only activates once
Finance has actually defined a ceiling there. One `Budget` per Cost Center/fiscal year is enforced at create
time (a Rejected one frees the combination up again).

Deliberately scoped to "does this one amount exceed the cost center's total approved annual ceiling," not
cumulative committed-vs-actual tracking (SAP's own Assigned/Committed/Actual availability control) — that
would need either Finance reading Procurement's own running committed-PO total (a new cross-module read
contract reversing the established one-directional Procurement-depends-on-Finance.Contracts boundary) or
`CheckAsync` becoming stateful (consuming budget as a side effect of a passing check, which would need
transactional care across `PurchaseOrderService.SubmitAsync`'s own per-cost-center loop to avoid partial
consumption on a later group's failure). Both are real design decisions, deliberately left for a later slice
rather than picked silently here. Live-verified over real HTTP against a running backend: approved a 50,000
SAR budget for a real Cost Center/2026, then confirmed a 100,000 SAR Purchase Order against that same cost
center is rejected at submit with the exact amount/ceiling in the error message, and a 10,000 SAR one
submits normally.

## Fiscal Year/Period and the Period Closing Center — real per-person duties, not a decorative checklist

`FiscalYear` (Draft-free, immediate-effect — same reasoning as `Budget`/`LookupType`) opens a calendar year
and auto-generates its 12 real `FiscalPeriod` children (one per month, `IsOpen` true by default). This isn't
decorative: `JournalEntryService.EnsurePeriodIsOpenAsync` is the one choke point every real posting in this
platform goes through — a manual entry, or a system-generated one from AP/AR Invoice, Payment, or Customer
Receipt posting — so closing a period genuinely blocks new postings into it, live-verified by closing a real
period, confirming a real Journal Entry's `Post` fails with a 409 naming the exact closed period, then
reopening the period and confirming the same entry posts normally. No `FiscalYear` on file for a posting
date is opt-in-not-enforced, the same reasoning `RealBudgetCheckService` uses for "no budget on file."

`ClosingActivity` (`UI/Finance/d1f20165-...png`, the Period Closing Center mockup) is the real per-person
checklist the mockup shows, not the shallow version originally scoped — see the design discussion that
preceded this: the user asked for 100% mockup fidelity, flagged that the mockup's own Overall Progress
numbers (14/4/0/1 = 19) don't reconcile against its 10-row table, and chose real per-activity sub-items over
a simpler top-level-only count. Ten fixed activities (`ClosingActivityCatalog`, the mockup's own list and
order) are generated per period. Three are auto-tracked from real data — Accounts Payable/Receivable get one
step per invoice still pending closure (`InvoiceDate` in the period, not yet Posted/Reversed) and Journal
Review gets one step per Manual-sourced entry (reusing this same doc's own `SourceDocumentType` trace) — and
self-complete the moment the underlying document reaches a resolved status, checked fresh on every read, no
background job. The other seven (Bank Reconciliation — one step per Active `BankAccount`, and
Inventory/Payroll/Fixed Assets/Tax Validation/Cost Allocation/Management Review, none of which have a real
underlying module yet) get manually-toggled steps. An activity's own `Status` auto-derives from its steps
(0 steps or all complete → Completed, some → InProgress, none → NotStarted) except `Blocked`, which is
always an explicit, never-derived override — the actual mechanism behind "every person has its own duties":
only the activity's own assignee, or someone holding `FiscalYearSecurity.AdministerPrivilegeKey`, may assign
it, toggle its steps, or block/unblock it (`Modules.Identity.Contracts.IUserLookup`, a new cross-module
read contract, resolves and validates the assignee). Live-verified end to end against real dev data: the
Accounts Payable activity generated real steps for real pending AP invoices (including ones IPC certification
itself raised), and posting one of those invoices for real auto-completed its step and moved the activity to
InProgress — not a demo, the actual cross-module pipeline built earlier this session working together.

Real, disclosed simplifications rather than silent gaps: the Closing Insights panel is rule-based over real
blocked/overdue state, not an actual AI call (this codebase has no LLM integration anywhere); Completion
Trend is reconstructed from each step's own real `CompletedAt` timestamp, so a freshly opened period shows a
sparse or flat line until it's actually used, not a fabricated smooth curve; the Closing Timeline's five
phases are a frontend-only grouping of the ten real activities, no new backend concept; and of the mockup's
five tabs, only Closing Checklist has real content — Posting Status/Reconciliation Status/Journal
Summary/Period History are placeholders, each its own separate report-building effort.

## What's still ahead

Fixed Assets, Cash & Bank depth beyond what `BankAccount`/`Payment` already cover, Document Splitting,
Parallel Ledgers, Cost Centers/Internal Orders/Profitability Segments as real Controlling objects beyond
Budget (see above), and Results Analysis — is designed but not yet built. Results Analysis in particular
shouldn't be attempted in isolation from the rest of the system: it's a cross-module read (actual project
cost from Materials, Labor, Equipment, and Subcontracts; billed revenue from Construction's IPCs/AR
invoices) producing a Finance-only posting, not something Construction or Project Management should try to
calculate themselves. See `docs/architecture/07-integrated-project-controlling.md` §7 before building it. On
the mockup-gap side (see `UI/Finance/FINANCE-MOCKUP-GAP-ANALYSIS.md` for the full picture): the Journal
Entry list and detail mockups are now built in full (see the section above) — Bank Reconciliation and Petty
Cash Management are the two remaining concepts that don't exist at all yet (Period Closing Center now does
— see above); the AP/AR workspace screens
are still today's four separate flat pages (`APInvoicesPage`/`PaymentsPage`/`ARInvoicesPage`/
`CustomerReceiptsPage`), not the mockup's combined tabbed workspace with aging; and `BudgetsPage.tsx` itself
is a plain functional list/detail screen, not a rebuild of the mockup's own Budget Control panel (which was
one small panel inside a larger montage image, not a fully detailed screen to replicate).
