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

## What's still ahead

Everything named in this module's intended scope beyond the Journal Entry itself — Accounts Payable and
Accounts Receivable as their own document types, Fixed Assets, Cash & Bank, Document Splitting, Parallel
Ledgers, Cost Centers/Internal Orders/Profitability Segments as real Controlling objects, Budget Control,
and Results Analysis — is designed but not yet built. Results Analysis in particular shouldn't be attempted
in isolation from the rest of the system: it's a cross-module read (actual project cost from Materials,
Labor, Equipment, and Subcontracts; billed revenue from Construction's IPCs) producing a Finance-only
posting, not something Construction or Project Management should try to calculate themselves. See
`docs/architecture/07-integrated-project-controlling.md` §7 before building it.
