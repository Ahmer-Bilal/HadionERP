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
