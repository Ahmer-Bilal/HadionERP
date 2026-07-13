using Platform.Localization.Formatting;
using Platform.Localization.Translation;

namespace Platform.Localization.Tests;

public class CurrencyFormattingTests
{
    [Fact]
    public void English_label_comes_before_the_amount()
    {
        var result = SarCurrencyFormatter.Format(12500.5m, "SAR", SupportedLanguage.English);

        Assert.Equal("SAR 12,500.50", result);
    }

    [Fact]
    public void Arabic_label_comes_after_the_amount()
    {
        var result = SarCurrencyFormatter.Format(12500.5m, "ر.س", SupportedLanguage.Arabic);

        Assert.Equal("12,500.50 ر.س", result);
    }

    [Fact]
    public void Arabic_indic_digits_are_only_applied_when_explicitly_requested()
    {
        var western = SarCurrencyFormatter.Format(1234m, "ر.س", SupportedLanguage.Arabic, useArabicIndicDigits: false);
        var arabicIndic = SarCurrencyFormatter.Format(1234m, "ر.س", SupportedLanguage.Arabic, useArabicIndicDigits: true);

        Assert.Equal("1,234.00 ر.س", western);
        Assert.Equal("١,٢٣٤.٠٠ ر.س", arabicIndic);
    }

    [Fact]
    public void Digit_transposition_only_touches_ascii_digits()
    {
        Assert.Equal("٠١٢٣٤٥٦٧٨٩", ArabicIndicDigits.Transpose("0123456789"));
        Assert.Equal("١٢٣.٤٥-", ArabicIndicDigits.Transpose("123.45-"));
    }

    [Fact]
    public void Currency_symbol_defaults_are_seeded_into_the_translation_service_and_resolve_by_language()
    {
        var translations = new InMemoryTranslationService();
        LocalizationDefaults.RegisterDefaults(translations);

        var englishSymbol = translations.Translate(LocalizationResourceKeys.SarCurrencySymbol, SupportedLanguage.English);
        var arabicSymbol = translations.Translate(LocalizationResourceKeys.SarCurrencySymbol, SupportedLanguage.Arabic);

        Assert.Equal("SAR", englishSymbol);
        Assert.Equal("ر.س", arabicSymbol);

        Assert.Equal("SAR 100.00", SarCurrencyFormatter.Format(100m, englishSymbol, SupportedLanguage.English));
    }
}
