# Platform.Localization

Implements docs/architecture/03-platform-services.md #1: AR/EN translation resolution with a tenant-
override fallback chain, RTL/LTR direction, the Hijri (Umm al-Qura) ↔ Gregorian dual calendar, SAR
currency/number formatting, and ZATCA Phase 1 (simplified tax invoice) QR code generation.

**Design rule enforced throughout this module**: business/formatting logic never contains a literal
display string — it looks text up by resource key through `ITranslationService`. The only file allowed to
contain literal Arabic/English display text is `LocalizationDefaults.cs` (the shipped default content,
equivalent to SAP's default OTR text entries or Dynamics 365's default `.resx` label values). Fixed
technical constants that don't vary by tenant/customer (e.g. the Arabic-Indic Unicode digit table in
`ArabicIndicDigits.cs`, BCP-47 language codes) are not subject to this rule — they're not translatable
content, they're structural facts.

**Built but not yet load-bearing anywhere** (found by `ARCHITECTURE-AUDIT.md`'s 2026-07-15 audit, §8/§9 —
noted here so this README doesn't read as more complete than it is): `Calendar/IHijriCalendarService`/
`UmAlQuraHijriCalendarService` and `Zatca/ZatcaSimplifiedInvoiceFields`/`ZatcaSimplifiedInvoiceQrBuilder` are
real, working code, but neither is actually *called* from anywhere outside this module yet — no UI date
field in `Apps.Shell` uses the Hijri service (every date input is plain Gregorian HTML), and
`Modules.Finance.Domain.APInvoice`/`APInvoiceService` never calls the ZATCA QR builder despite that entity's
own doc comments mentioning ZATCA-compliant VAT — no real invoice ever gets a QR code today. The mechanics
are proven correct in isolation; wiring them into a real consumer is the remaining work, tracked as roadmap
checkpoint items (see `docs/architecture/06-roadmap.md`'s "Architecture Gap Audit & Platform Hardening"
section).

**Deferred to later, once the modules that need them exist**:
- ZATCA Phase 2 (integrated e-invoicing: XML/UBL 2.1, digital signing, cryptographic stamp, clearance API)
  — needs a live ZATCA API integration and a real Finance module to generate invoices from.
- WPS (Wage Protection System) payroll file export, GOSI integration, Saudization/Nitaqat reporting —
  these are Payroll/HR module concerns (Phase 4), not general localization; they'll live in
  `Platform.Integration` adapters called from those modules.
- VAT tax code configuration (the 15% rate itself) — that's MasterData configuration, not a localization
  service; this module only provides the QR-code and formatting mechanics tax documents need.
- Translation *administration* (a non-developer UI for editing text) — that's a Platform.UI/Configuration
  concern; this module only provides the resolution engine underneath it.
