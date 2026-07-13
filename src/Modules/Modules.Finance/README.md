# Modules.Finance

One Universal-Journal-style line-item store covering General Ledger, Accounts Payable, Accounts Receivable,
Fixed Assets, and Cash & Bank (these are document types producing lines into the same store, not separate
sub-ledgers). Also owns: Document Splitting (real-time dimensional balancing), Parallel Ledgers (IFRS +
Saudi statutory/Zakat basis, posted simultaneously), Controlling objects (Cost Centers, Internal Orders,
Profitability Segments), Budget Control, and **Results Analysis (percentage-of-completion revenue
recognition) + Settlement to CO-PA** run against WBS elements each period-close.

See `docs/architecture/07-project-accounting-and-financial-architecture.md` for the full, source-checked
rationale (SAP-referenced).
