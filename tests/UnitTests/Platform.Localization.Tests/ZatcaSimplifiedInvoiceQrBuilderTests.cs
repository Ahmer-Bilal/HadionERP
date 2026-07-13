using System.Text;
using Platform.Localization.Zatca;

namespace Platform.Localization.Tests;

public class ZatcaSimplifiedInvoiceQrBuilderTests
{
    private static readonly DateTimeOffset ReferenceTimestamp = new(2026, 7, 13, 10, 15, 0, TimeSpan.Zero);

    [Fact]
    public void Produces_the_exact_TLV_byte_structure_the_ZATCA_spec_requires()
    {
        var fields = new ZatcaSimplifiedInvoiceFields("Test Co", "300000000000003", ReferenceTimestamp, 115.00m, 15.00m);

        var actualBytes = Convert.FromBase64String(ZatcaSimplifiedInvoiceQrBuilder.BuildBase64(fields));

        var expected = new List<byte>();
        void AppendField(byte tag, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            expected.Add(tag);
            expected.Add((byte)bytes.Length);
            expected.AddRange(bytes);
        }

        AppendField(1, "Test Co");
        AppendField(2, "300000000000003");
        AppendField(3, "2026-07-13T10:15:00Z");
        AppendField(4, "115.00");
        AppendField(5, "15.00");

        Assert.Equal(expected.ToArray(), actualBytes);
    }

    [Fact]
    public void Parse_recovers_all_five_fields_exactly()
    {
        var fields = new ZatcaSimplifiedInvoiceFields("Gulf Falcon Construction Co", "300000000000003", ReferenceTimestamp, 11500.50m, 1500.07m);

        var parsed = ZatcaSimplifiedInvoiceQrBuilder.Parse(ZatcaSimplifiedInvoiceQrBuilder.BuildBase64(fields));

        Assert.Equal("Gulf Falcon Construction Co", parsed[1]);
        Assert.Equal("300000000000003", parsed[2]);
        Assert.Equal("2026-07-13T10:15:00Z", parsed[3]);
        Assert.Equal("11500.50", parsed[4]);
        Assert.Equal("1500.07", parsed[5]);
    }

    [Fact]
    public void Handles_a_multibyte_utf8_Arabic_seller_name_correctly()
    {
        const string arabicSellerName = "شركة الفا للمقاولات";
        var fields = new ZatcaSimplifiedInvoiceFields(arabicSellerName, "300000000000003", ReferenceTimestamp, 100m, 13.04m);

        var parsed = ZatcaSimplifiedInvoiceQrBuilder.Parse(ZatcaSimplifiedInvoiceQrBuilder.BuildBase64(fields));

        Assert.Equal(arabicSellerName, parsed[1]);
    }

    [Fact]
    public void Rejects_a_field_value_exceeding_the_TLV_255_byte_limit()
    {
        var tooLong = new string('A', 256);
        var fields = new ZatcaSimplifiedInvoiceFields(tooLong, "300000000000003", ReferenceTimestamp, 100m, 13m);

        Assert.Throws<ArgumentException>(() => ZatcaSimplifiedInvoiceQrBuilder.BuildBase64(fields));
    }

    [Fact]
    public void Timestamp_is_formatted_as_ISO_8601_UTC()
    {
        var fields = new ZatcaSimplifiedInvoiceFields("Test Co", "300000000000003", ReferenceTimestamp, 100m, 13m);

        var parsed = ZatcaSimplifiedInvoiceQrBuilder.Parse(ZatcaSimplifiedInvoiceQrBuilder.BuildBase64(fields));

        Assert.Equal("2026-07-13T10:15:00Z", parsed[3]);
    }
}
