namespace Platform.Localization.Zatca;

/// <summary>The five fields ZATCA's Phase 1 (simplified tax invoice) QR code carries.</summary>
public sealed record ZatcaSimplifiedInvoiceFields(
    string SellerName,
    string VatRegistrationNumber,
    DateTimeOffset InvoiceTimestamp,
    decimal InvoiceTotalWithVat,
    decimal VatTotal);
