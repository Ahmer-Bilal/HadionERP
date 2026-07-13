using Gateway.Api.Localization;
using Microsoft.AspNetCore.Mvc;
using Platform.Audit;
using Platform.Api;
using Platform.Events.Outbox;
using Platform.Localization;
using Platform.Localization.Translation;

namespace Gateway.Api.Controllers;

/// <summary>
/// System-level endpoints — the permanent operational surface every real deployment needs (status,
/// health, and a localization proof-point), not a throwaway demo. Business-module controllers will sit
/// alongside this one as each module is built, inheriting the same <see cref="PlatformApiController"/> base
/// so the conventions (route prefix, error envelope, paging) are uniform across every module — see
/// docs/architecture/04-data-and-api.md #2.
/// </summary>
public class SystemController : PlatformApiController
{
    private readonly ITranslationService _translationService;
    private readonly IOutboxStore _outboxStore;
    private readonly IAuditLog _auditLog;

    public SystemController(ITranslationService translationService, IOutboxStore outboxStore, IAuditLog auditLog)
    {
        _translationService = translationService;
        _outboxStore = outboxStore;
        _auditLog = auditLog;
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var allMessages = _outboxStore.GetAll();
        var auditEntries = _auditLog.GetAll();
        // Re-verify the chain on every status read: if the log were ever tampered with out-of-band, this
        // is where an operator would see it (chainValid: false). Cheap on an in-memory log; a real
        // deployment would run this as a periodic background job rather than per-request.
        var chainValid = _auditLog.VerifyChain() is null;

        return Ok(new
        {
            application = "ERP Platform",
            phase = "Phase 0 - Platform Foundation",
            utcNow = DateTimeOffset.UtcNow,
            kernelServicesWired = new[] { "Platform.Core", "Platform.Security", "Platform.Localization", "Platform.Workflow", "Platform.Events", "Platform.Audit" },
            supportedLanguages = new[] { "en", "ar" },
            eventsOutbox = new
            {
                published = allMessages.Count(m => m.IsPublished),
                pending = allMessages.Count(m => !m.IsPublished)
            },
            audit = new
            {
                entries = auditEntries.Count,
                chainValid
            }
        });
    }

    [HttpGet("greeting")]
    public IActionResult GetGreeting([FromQuery] string lang = "en")
    {
        SupportedLanguage language;
        try
        {
            language = SupportedLanguageCodes.FromCode(lang);
        }
        catch (NotSupportedException)
        {
            return BadRequest(new { error = $"Unsupported language code '{lang}'. Supported: en, ar." });
        }

        var message = _translationService.Translate(GatewayApiLocalizationDefaults.WelcomeMessageKey, language);
        var direction = LanguageDirection.For(language).ToHtmlDirAttribute();

        return Ok(new { language = lang, direction, message });
    }
}
