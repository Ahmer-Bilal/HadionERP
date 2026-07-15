using Microsoft.AspNetCore.Mvc;
using Modules.MasterData.Application;
using Platform.Api;

namespace Modules.MasterData.Api;

/// <summary>
/// The Admin Panel's Lookup Data controller — CRUD for lookup types and their values. One actor only
/// (unlike every Business Object controller's Maintainer/Approver split): see
/// <see cref="Modules.MasterData.Domain.LookupType"/>'s doc comment for why lookup data has no approval
/// workflow. Nested route (<c>/lookup-types/{typeCode}/values</c>) because a value has no meaning outside
/// its type, same "child resource nested under its parent" shape as attachments/notes on every other
/// controller.
/// </summary>
[Route("api/v1/masterdata/lookup-types")]
public sealed class LookupsController : PlatformApiController
{
    private readonly LookupService _service;

    public LookupsController(LookupService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> ListTypes(CancellationToken cancellationToken) =>
        Ok(await _service.ListTypesAsync(cancellationToken));

    [HttpPost]
    public async Task<IActionResult> CreateType([FromBody] CreateLookupTypeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _service.CreateTypeAsync(request, CurrentActor, cancellationToken);
            return CreatedAtAction(nameof(ListTypes), created);
        }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
    }

    [HttpPut("{typeCode}")]
    public async Task<IActionResult> UpdateType(string typeCode, [FromBody] UpdateLookupTypeRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await _service.UpdateTypeAsync(typeCode, request, CurrentActor, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
    }

    [HttpDelete("{typeCode}")]
    public async Task<IActionResult> DeleteType(string typeCode, CancellationToken cancellationToken)
    {
        try { await _service.DeleteTypeAsync(typeCode, CurrentActor, cancellationToken); return NoContent(); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return ConflictError(ex.Message); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
    }

    [HttpGet("{typeCode}/values")]
    public async Task<IActionResult> ListValues(string typeCode, [FromQuery] bool includeInactive, CancellationToken cancellationToken)
    {
        try { return Ok(await _service.ListValuesAsync(typeCode, includeInactive, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPost("{typeCode}/values")]
    public async Task<IActionResult> CreateValue(string typeCode, [FromBody] CreateLookupValueRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _service.CreateValueAsync(typeCode, request, CurrentActor, cancellationToken);
            return CreatedAtAction(nameof(ListValues), new { typeCode }, created);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
    }

    [HttpPut("{typeCode}/values/{id:guid}")]
    public async Task<IActionResult> UpdateValue(string typeCode, Guid id, [FromBody] UpdateLookupValueRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await _service.UpdateValueAsync(typeCode, id, request, CurrentActor, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
    }

    [HttpPost("{typeCode}/values/{id:guid}/activate")]
    public async Task<IActionResult> ActivateValue(string typeCode, Guid id, CancellationToken cancellationToken)
    {
        try { return Ok(await _service.SetActiveAsync(typeCode, id, isActive: true, CurrentActor, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
    }

    [HttpPost("{typeCode}/values/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateValue(string typeCode, Guid id, CancellationToken cancellationToken)
    {
        try { return Ok(await _service.SetActiveAsync(typeCode, id, isActive: false, CurrentActor, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
    }

    [HttpDelete("{typeCode}/values/{id:guid}")]
    public async Task<IActionResult> DeleteValue(string typeCode, Guid id, CancellationToken cancellationToken)
    {
        try { await _service.DeleteValueAsync(typeCode, id, CurrentActor, cancellationToken); return NoContent(); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return ConflictError(ex.Message); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
    }
}
