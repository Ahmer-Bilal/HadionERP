using Platform.Localization;
using Platform.Localization.Translation;

namespace Gateway.Api.Localization;

/// <summary>
/// Gateway.Api's own shipped default translated content — same pattern as
/// Platform.Localization/LocalizationDefaults.cs (the one place literal display text is allowed to
/// live; everything else looks text up by resource key). This file is on the architecture test's
/// allow-list in tests/ArchitectureTests/Platform.ArchitectureTests for exactly that reason.
///
/// NOTE: unlike the ZATCA QR format (a technical spec I verified against ZATCA's own documentation),
/// the Arabic wording here is a draft translation, not independently verified by a professional Arabic
/// linguist — it should be reviewed before this text is shown to real users in production.
/// </summary>
public static class GatewayApiLocalizationDefaults
{
    public const string WelcomeMessageKey = "Gateway.System.WelcomeMessage";

    public static void RegisterDefaults(InMemoryTranslationService translationService)
    {
        translationService.RegisterDefault(WelcomeMessageKey, SupportedLanguage.English, "Welcome to HadionERP.");
        translationService.RegisterDefault(WelcomeMessageKey, SupportedLanguage.Arabic, "مرحبًا بك في HadionERP.");
    }
}
