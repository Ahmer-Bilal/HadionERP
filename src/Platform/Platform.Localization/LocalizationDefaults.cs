using Platform.Localization.Translation;

namespace Platform.Localization;

/// <summary>
/// The module-default translated content this module ships with — the equivalent of SAP's default OTR
/// text entries or Dynamics 365's default .resx label values. This is the ONLY place in
/// Platform.Localization where a literal display string is written; everywhere else (formatters,
/// services) looks text up by resource key through <see cref="ITranslationService"/>. A tenant can
/// override any of this without touching code, via <see cref="InMemoryTranslationService.RegisterTenantOverride"/>
/// (or its database-backed equivalent later).
/// </summary>
public static class LocalizationDefaults
{
    public static void RegisterDefaults(InMemoryTranslationService translationService)
    {
        translationService.RegisterDefault(LocalizationResourceKeys.SarCurrencySymbol, SupportedLanguage.English, "SAR");
        translationService.RegisterDefault(LocalizationResourceKeys.SarCurrencySymbol, SupportedLanguage.Arabic, "ر.س");
    }
}
