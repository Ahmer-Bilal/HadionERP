namespace Platform.Localization.Translation;

/// <summary>
/// Reference implementation of <see cref="ITranslationService"/>. A real deployment backs this with a
/// database-managed translation store maintained through a non-developer-friendly workbench
/// (docs/architecture/03-platform-services.md #1.4) — this in-memory version proves the resolution
/// order works, the same pattern as every other "InMemory*" reference implementation in Platform.Core
/// and Platform.Security.
/// </summary>
public sealed class InMemoryTranslationService : ITranslationService
{
    private readonly Dictionary<string, Dictionary<SupportedLanguage, string>> _defaults = new();
    private readonly Dictionary<string, Dictionary<string, Dictionary<SupportedLanguage, string>>> _tenantOverrides = new();

    public void RegisterDefault(string resourceKey, SupportedLanguage language, string text)
    {
        if (!_defaults.TryGetValue(resourceKey, out var languages))
        {
            languages = new Dictionary<SupportedLanguage, string>();
            _defaults[resourceKey] = languages;
        }

        languages[language] = text;
    }

    public void RegisterTenantOverride(string tenantId, string resourceKey, SupportedLanguage language, string text)
    {
        if (!_tenantOverrides.TryGetValue(tenantId, out var keys))
        {
            keys = new Dictionary<string, Dictionary<SupportedLanguage, string>>();
            _tenantOverrides[tenantId] = keys;
        }

        if (!keys.TryGetValue(resourceKey, out var languages))
        {
            languages = new Dictionary<SupportedLanguage, string>();
            keys[resourceKey] = languages;
        }

        languages[language] = text;
    }

    public string Translate(string resourceKey, SupportedLanguage language, string? tenantId = null)
    {
        if (tenantId is not null
            && _tenantOverrides.TryGetValue(tenantId, out var tenantKeys)
            && tenantKeys.TryGetValue(resourceKey, out var tenantLanguages)
            && tenantLanguages.TryGetValue(language, out var tenantText))
        {
            return tenantText;
        }

        if (_defaults.TryGetValue(resourceKey, out var languages))
        {
            if (languages.TryGetValue(language, out var text))
            {
                return text;
            }

            if (languages.TryGetValue(SupportedLanguage.English, out var englishFallback))
            {
                return englishFallback;
            }
        }

        return $"[[{resourceKey}]]";
    }
}
