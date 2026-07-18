using Microsoft.AspNetCore.Mvc;
using Platform.Api;
using Platform.Audit;
using Platform.Core;

namespace Gateway.Api.Controllers;

public sealed record AuditHistoryEntryDto(DateTimeOffset OccurredAt, string Action, string Actor, string Summary);

/// <summary>Generic change-history endpoint for any Business Object — the real backing for every mockup's
/// own "History" tab (starting with the Journal Entry detail's), reading <see cref="IAuditLog"/>'s existing
/// permanent, hash-chained record rather than a new document-specific log. Same reasoning as
/// <see cref="AttachmentsController"/>/<see cref="NotesController"/>.</summary>
[Route("api/v1/audit-history")]
public sealed class AuditHistoryController : PlatformApiController
{
    private readonly IAuditLog _auditLog;

    public AuditHistoryController(IAuditLog auditLog) => _auditLog = auditLog;

    [HttpGet]
    public IActionResult List([FromQuery] string targetType, [FromQuery] Guid targetId)
    {
        var entries = _auditLog.GetFor(new BusinessObjectReference(targetId, targetType, "Self"))
            .OrderByDescending(e => e.OccurredAt)
            .Select(e => new AuditHistoryEntryDto(e.OccurredAt, e.Action.ToString(), e.ActorPrincipalKey, e.Summary))
            .ToList();
        return Ok(entries);
    }
}
