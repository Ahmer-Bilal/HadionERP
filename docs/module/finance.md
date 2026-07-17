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

## What's still ahead

Fixed Assets, Cash & Bank depth beyond what `BankAccount`/`Payment` already cover, Document Splitting,
Parallel Ledgers, Cost Centers/Internal Orders/Profitability Segments as real Controlling objects, Budget
Control, and Results Analysis — is designed but not yet built. A Customer Receipt Business Object (the AR
mirror of `Payment`) is the natural next AR-side piece — it's what would let `OutstandingBalance` actually
mean something and close the loop the IPC→AR wiring above opens. Results Analysis in particular shouldn't be
attempted in isolation from the rest of the system: it's a cross-module read (actual project cost from
Materials, Labor, Equipment, and Subcontracts; billed revenue from Construction's IPCs/AR invoices)
producing a Finance-only posting, not something Construction or Project Management should try to calculate
themselves. See `docs/architecture/07-integrated-project-controlling.md` §7 before building it.
