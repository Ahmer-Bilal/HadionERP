namespace Platform.Localization;

/// <summary>
/// The languages this platform treats as first-class (docs/architecture/03-platform-services.md #1.1).
/// Adding a third language later means adding a case here and to <see cref="TextDirection"/> — it does
/// not mean redesigning any UI component, because every component is authored direction-aware, not
/// English-with-Arabic-bolted-on.
/// </summary>
public enum SupportedLanguage
{
    Arabic,
    English
}

public static class SupportedLanguageCodes
{
    /// <summary>BCP-47 language tag, as used in resource keys, HTTP Accept-Language, and CultureInfo.</summary>
    public static string ToCode(this SupportedLanguage language) => language switch
    {
        SupportedLanguage.Arabic => "ar",
        SupportedLanguage.English => "en",
        _ => throw new ArgumentOutOfRangeException(nameof(language))
    };

    public static SupportedLanguage FromCode(string code) => code switch
    {
        "ar" => SupportedLanguage.Arabic,
        "en" => SupportedLanguage.English,
        _ => throw new NotSupportedException($"Language code '{code}' is not a supported first-class language.")
    };
}
