using System.Globalization;
using System.Text;

namespace Platform.Localization.Zatca;

/// <summary>
/// Builds the ZATCA "Phase 1" (simplified tax invoice) QR code payload: five Tag-Length-Value fields
/// concatenated into one byte string and Base64-encoded, per ZATCA's published QR code specification
/// (verified against ZATCA's own documentation and third-party implementation guides before writing this,
/// since getting a statutory format wrong is a compliance issue, not just a bug):
///
///   Tag 1 = seller name, Tag 2 = VAT registration number, Tag 3 = invoice timestamp (ISO 8601),
///   Tag 4 = invoice total with VAT, Tag 5 = VAT total.
///
/// Each field is: 1 byte tag number, 1 byte length (the UTF-8 byte length of the value — so a 256+ byte
/// value cannot be represented and is rejected rather than silently truncated), then the value's UTF-8
/// bytes. The resulting Base64 string is what gets embedded in the printed/rendered QR code image
/// (image rendering itself is a Platform.Reporting concern, not built yet).
///
/// This covers Phase 1 only. Phase 2 (integrated e-invoicing with ZATCA's clearance API, XML/UBL 2.1,
/// digital signing, and cryptographic stamp tags 6-9) requires a live integration with ZATCA's API and
/// certificate-based signing — deferred until Platform.Integration and a real Finance module exist to
/// integrate against.
/// </summary>
public static class ZatcaSimplifiedInvoiceQrBuilder
{
    private const byte SellerNameTag = 1;
    private const byte VatRegistrationNumberTag = 2;
    private const byte TimestampTag = 3;
    private const byte InvoiceTotalTag = 4;
    private const byte VatTotalTag = 5;

    public static string BuildBase64(ZatcaSimplifiedInvoiceFields fields)
    {
        var timestamp = fields.InvoiceTimestamp.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

        var segments = new (byte Tag, string Value)[]
        {
            (SellerNameTag, fields.SellerName),
            (VatRegistrationNumberTag, fields.VatRegistrationNumber),
            (TimestampTag, timestamp),
            (InvoiceTotalTag, fields.InvoiceTotalWithVat.ToString("F2", CultureInfo.InvariantCulture)),
            (VatTotalTag, fields.VatTotal.ToString("F2", CultureInfo.InvariantCulture)),
        };

        using var buffer = new MemoryStream();

        foreach (var (tag, value) in segments)
        {
            var valueBytes = Encoding.UTF8.GetBytes(value);

            if (valueBytes.Length > byte.MaxValue)
            {
                throw new ArgumentException(
                    $"ZATCA QR field (tag {tag}) is {valueBytes.Length} UTF-8 bytes, exceeding the TLV " +
                    "format's 255-byte-per-field limit.");
            }

            buffer.WriteByte(tag);
            buffer.WriteByte((byte)valueBytes.Length);
            buffer.Write(valueBytes, 0, valueBytes.Length);
        }

        return Convert.ToBase64String(buffer.ToArray());
    }

    /// <summary>Decodes a Base64 QR payload back into its raw tag → value map — used by tests to prove
    /// round-trip correctness, and reusable later for validating/auditing an existing invoice's QR code.</summary>
    public static IReadOnlyDictionary<byte, string> Parse(string base64Payload)
    {
        var bytes = Convert.FromBase64String(base64Payload);
        var result = new Dictionary<byte, string>();

        var i = 0;
        while (i < bytes.Length)
        {
            var tag = bytes[i++];
            var length = bytes[i++];
            result[tag] = Encoding.UTF8.GetString(bytes, i, length);
            i += length;
        }

        return result;
    }
}
