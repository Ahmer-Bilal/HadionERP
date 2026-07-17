using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Platform.Api;

/// <summary>
/// The base controller every module's API controller inherits. Establishes the shared conventions from
/// docs/architecture/05-data-and-api.md #2 so every endpoint across every module behaves the same way:
/// the same route-prefix pattern, the same error-envelope shape, the same paged-result helper. A module's
/// controller inherits this and gets the conventions for free; it doesn't reimplement them.
///
/// Route convention: <c>api/v{version}/[controller]</c> — the URL-segment major version (§2.1). The
/// existing SystemController already used <c>api/v1/system</c>; this base formalizes that pattern as the
/// standard rather than a one-off.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public abstract class PlatformApiController : ControllerBase
{
    /// <summary>The real logged-in user's username, resolved from the validated JWT's name claim — replaces
    /// what every controller used to hardcode as <c>"system/ui"</c>/<c>"system/approver"</c>
    /// (`MISSING-FEATURES-AUDIT.md` Part 1 §1). This is the exact string every Application-layer service's
    /// existing <c>actor: string</c> parameter already expects; no service-layer code changes when a
    /// controller switches from a hardcoded literal to this property. Throws if called from an action that
    /// doesn't actually require authentication — every action does by default (see the global
    /// <c>AuthorizeFilter</c> registered in Gateway.Api's <c>Program.cs</c>) except <c>AuthController.Login</c>,
    /// which never needs this since it's what produces the identity in the first place.</summary>
    protected string CurrentActor =>
        User.Identity?.Name
        ?? throw new InvalidOperationException("CurrentActor was accessed from an unauthenticated request.");

    /// <summary>Returns a paged result from a full in-memory set, applying the OData $top/$skip. This is
    /// the helper a module's list endpoint calls when its data source is already materialized.</summary>
    protected static PagedResult<T> Paged<T>(IReadOnlyList<T> fullSet, ODataQuery query) =>
        PagedResult<T>.From(fullSet, query.Skip, query.Top);

    /// <summary>Returns a 400 validation error with field-level failures, using the standard error envelope.</summary>
    protected static IActionResult ValidationError(IReadOnlyDictionary<string, string[]> errors, string? detail = null) =>
        new BadRequestObjectResult(ApiErrorEnvelope.Validation(errors, detail));

    /// <summary>Returns a 400 bad-request error with a single message, using the standard error envelope.</summary>
    protected static IActionResult BadRequestError(string detail) =>
        new BadRequestObjectResult(ApiErrorEnvelope.BadRequest(detail));

    /// <summary>Returns a 409 conflict error (e.g. optimistic concurrency mismatch, duplicate doc number).</summary>
    protected static IActionResult ConflictError(string detail) =>
        new ConflictObjectResult(ApiErrorEnvelope.Conflict(detail));

    /// <summary>Returns a 403 forbidden error (an actor lacking the required Privilege), using the
    /// standard error envelope.</summary>
    protected static IActionResult ForbiddenError(string detail) =>
        new ObjectResult(ApiErrorEnvelope.Forbidden(detail)) { StatusCode = StatusCodes.Status403Forbidden };
}
