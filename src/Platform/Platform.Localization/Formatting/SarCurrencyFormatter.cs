using System.Globalization;

namespace Platform.Localization.Formatting;

/// <summary>
/// Formats a SAR amount given an already-resolved currency label — this class is pure number-formatting
/// logic and deliberately holds no display text itself. The caller resolves <paramref name="currencyLabel"/>
/// via <see cref="Translation.ITranslationService"/> using <see cref="LocalizationResourceKeys.SarCurrencySymbol"/>
/// (see LocalizationDefaults.cs for where the actual "SAR" / "ر.س" text is registered) — the same
/// separation SAP enforces via its OTR text repository and Dynamics 365 enforces via label files: code
/// never contains a literal display string, it looks one up by key.
///
/// Number formatting itself deliberately does NOT rely on CultureInfo("ar-SA")'s automatic digit
/// substitution — that behavior is inconsistent across .NET versions/operating systems (depends on host
/// ICU data), so grouping/decimal separators always follow the invariant (Western) convention, matching
/// Saudi statutory documents including ZATCA e-invoices. Arabic-Indic digits are applied only when
/// explicitly requested (see <see cref="ArabicIndicDigits"/>).
/// </summary>
public static class SarCurrencyFormatter
{
    public static string Format(decimal amount, string currencyLabel, SupportedLanguage language, bool useArabicIndicDigits = false)
    {
        var numberText = amount.ToString("N2", CultureInfo.InvariantCulture);

        if (useArabicIndicDigits && language == SupportedLanguage.Arabic)
        {
            numberText = ArabicIndicDigits.Transpose(numberText);
        }

        return language == SupportedLanguage.Arabic
            ? $"{numberText} {currencyLabel}"
            : $"{currencyLabel} {numberText}";
    }
}
