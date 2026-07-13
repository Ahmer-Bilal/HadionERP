using Gateway.Api.Localization;
using Microsoft.AspNetCore.Mvc;
using Platform.Localization;
using Platform.Localization.Translation;

namespace Gateway.Api.Controllers;

/// <summary>
/// System-level endpoints — the permanent operational surface every real deployment needs (status,
/// health, and a localization proof-point), not a throwaway demo. Business-module controllers will sit
/// alongside this one as each module is built, following the same Platform.Api conventions
/// (docs/architecture/04-data-and-api.md #2) once those are formalized.
/// </summary>
[ApiController]
[Route("api/v1/system")]
public class SystemController : ControllerBase
{
    private readonly ITranslationService _translationService;

    public SystemController(ITranslationService translationService)
    {
        _translationService = translationService;
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            application = "ERP Platform",
            phase = "Phase 0 - Platform Foundation",
            utcNow = DateTimeOffset.UtcNow,
            kernelServicesWired = new[] { "Platform.Core", "Platform.Security", "Platform.Localization", "Platform.Workflow" },
            supportedLanguages = new[] { "en", "ar" }
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
