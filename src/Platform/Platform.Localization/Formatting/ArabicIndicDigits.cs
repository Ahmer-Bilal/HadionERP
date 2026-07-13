namespace Platform.Localization.Formatting;

/// <summary>
/// Transposes Western digits (0-9) to Eastern Arabic-Indic digits (٠-٩) — an explicit, opt-in operation.
/// It is not applied automatically by <see cref="SarCurrencyFormatter"/> because Saudi statutory
/// documents (including ZATCA e-invoices) use Western digits regardless of language; this exists for UI
/// display preference only, where a user has explicitly asked for it.
/// </summary>
public static class ArabicIndicDigits
{
    private static readonly char[] Digits = { '٠', '١', '٢', '٣', '٤', '٥', '٦', '٧', '٨', '٩' };

    public static string Transpose(string westernDigits)
    {
        var chars = westernDigits.ToCharArray();

        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] is >= '0' and <= '9')
            {
                chars[i] = Digits[chars[i] - '0'];
            }
        }

        return new string(chars);
    }
}
