namespace Modules.Finance.Application;

/// <summary>
/// The closed set of values <see cref="JournalEntryDto.SourceDocumentType"/> can hold today — every real
/// caller of <see cref="JournalEntryService.CreateAsync"/>/<see cref="JournalEntryService.CreateSystemGeneratedAsync"/>
/// tags itself with one of these. Not an enum on the Domain entity itself: Domain only stores the string
/// (see <c>JournalEntry.SourceDocumentType</c>'s own doc comment) so this list can grow as new document
/// types start raising entries without a Domain-layer change each time. Deliberately doesn't include values
/// the mockup shows but nothing produces yet (Bank Reconciliation, Payroll, Fixed Asset) — see
/// `UI/Finance/FINANCE-MOCKUP-GAP-ANALYSIS.md`.
/// </summary>
public static class JournalEntrySourceDocumentTypes
{
    public const string Manual = "Manual";
    public const string APInvoice = "APInvoice";
    public const string ARInvoice = "ARInvoice";
    public const string Payment = "Payment";
    public const string CustomerReceipt = "CustomerReceipt";
}
