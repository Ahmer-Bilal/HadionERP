using Microsoft.AspNetCore.Mvc;
using Modules.Finance.Application;
using Modules.Identity.Contracts;
using Platform.Api;

namespace Modules.Finance.Api;

public sealed record ToggleClosingActivityStepRequest(bool IsCompleted);

public sealed record SetClosingActivityBlockedRequest(bool IsBlocked);

/// <summary>
/// Per-activity actions for the Period Closing Center checklist — separate from
/// <see cref="FiscalYearsController"/> since these operate on one <see cref="Domain.ClosingActivity"/> by
/// its own Id, not on a fiscal-year/period route. <see cref="ClosingActivityService"/> itself enforces the
/// real "every person has its own duties" rule: only the activity's assignee or a Finance Manager may call
/// any of these.
/// </summary>
[Route("api/v1/finance/closing-activities")]
public sealed class ClosingActivitiesController : PlatformApiController
{
    private readonly ClosingActivityService _service;
    private readonly IUserLookup _userLookup;

    public ClosingActivitiesController(ClosingActivityService service, IUserLookup userLookup)
    {
        _service = service;
        _userLookup = userLookup;
    }

    /// <summary>The assignee picker's own data source — deliberately not
    /// <c>Modules.Identity.Api.UsersController.List</c> (gated to <c>IdentitySecurity.AdministerPrivilegeKey</c>,
    /// a broader and unrelated privilege, and returns more than an assignee picker needs). Any authenticated
    /// user may call this — picking a name from a list reveals nothing an assignment confirmation wouldn't
    /// already reveal.</summary>
    [HttpGet("assignable-users")]
    public async Task<IActionResult> ListAssignableUsers(CancellationToken cancellationToken) =>
        Ok(await _userLookup.ListActiveAsync(cancellationToken));

    [HttpPost("{id:guid}/assign")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignClosingActivityRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await _service.AssignAsync(id, request, CurrentActor, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
    }

    [HttpPost("{id:guid}/steps/{stepId:guid}/toggle")]
    public async Task<IActionResult> ToggleStep(
        Guid id, Guid stepId, [FromBody] ToggleClosingActivityStepRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await _service.ToggleStepAsync(id, stepId, request.IsCompleted, CurrentActor, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return ConflictError(ex.Message); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
    }

    [HttpPost("{id:guid}/blocked")]
    public async Task<IActionResult> SetBlocked(Guid id, [FromBody] SetClosingActivityBlockedRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await _service.SetBlockedAsync(id, request.IsBlocked, CurrentActor, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
    }
}
