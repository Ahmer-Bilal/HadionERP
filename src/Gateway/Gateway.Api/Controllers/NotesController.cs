using Microsoft.AspNetCore.Mvc;
using Platform.Api;
using Platform.Notes;

namespace Gateway.Api.Controllers;

public sealed record AddNoteRequest(string Text);

/// <summary>Generic note endpoints for any Business Object — same reasoning as
/// <see cref="AttachmentsController"/>.</summary>
[Route("api/v1/notes")]
public sealed class NotesController : PlatformApiController
{
    private readonly INoteService _service;

    public NotesController(INoteService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string businessObjectType, [FromQuery] Guid businessObjectId, CancellationToken cancellationToken) =>
        Ok(await _service.ListAsync(businessObjectType, businessObjectId, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Add(
        [FromQuery] string businessObjectType, [FromQuery] Guid businessObjectId, [FromBody] AddNoteRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await _service.AddAsync(businessObjectType, businessObjectId, request.Text, CurrentActor, cancellationToken)); }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _service.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
