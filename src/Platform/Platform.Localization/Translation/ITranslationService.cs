namespace Platform.Localization.Translation;

/// <summary>
/// Resolves a namespaced resource key (e.g. "Modules.Procurement.PurchaseOrder.Header") to display text,
/// per the fallback chain in docs/architecture/04-platform-services.md #1.1: tenant override → module
/// default (requested language) → module default (English) → a visibly-missing marker (never a silent
/// blank — a missing translation should be obvious to whoever's reviewing the screen, not invisible).
/// </summary>
public interface ITranslationService
{
    string Translate(string resourceKey, SupportedLanguage language, string? tenantId = null);
}
