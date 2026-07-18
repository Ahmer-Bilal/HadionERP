using Microsoft.AspNetCore.Mvc;
using Platform.Api;
using Platform.Attachments;

namespace Gateway.Api.Controllers;

/// <summary>
/// Generic file-attachment endpoints for any Business Object — the real backing for every mockup's own
/// "Attachments" tab (starting with the Journal Entry detail's, `UI/Finance/Finance_Jornal Entry_Object.png`),
/// not a Finance-specific concept. Lives in Gateway.Api alongside <see cref="SystemController"/> rather than
/// inside a business module's own Api project, the same reasoning: <see cref="IAttachmentService"/> is a
/// Platform-level service, not owned by any one module — reused here exactly as registered for
/// Modules.MasterData's own Business Partner/Item attachments (see Program.cs), a flat
/// (businessObjectType, businessObjectId) pair identifies the record either way.
/// </summary>
[Route("api/v1/attachments")]
public sealed class AttachmentsController : PlatformApiController
{
    private const long MaxUploadBytes = 10 * 1024 * 1024;

    private readonly IAttachmentService _service;

    public AttachmentsController(IAttachmentService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string businessObjectType, [FromQuery] Guid businessObjectId, CancellationToken cancellationToken) =>
        Ok(await _service.ListAsync(businessObjectType, businessObjectId, cancellationToken));

    [HttpPost]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> Upload(
        [FromQuery] string businessObjectType, [FromQuery] Guid businessObjectId, [FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0) return BadRequestError("No file was uploaded.");

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        try
        {
            var metadata = await _service.UploadAsync(
                businessObjectType, businessObjectId, file.FileName, file.ContentType, stream.ToArray(), CurrentActor, cancellationToken);
            return Ok(metadata);
        }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.DownloadAsync(id, cancellationToken);
        return result is null ? NotFound() : File(result.Value.Content, result.Value.Metadata.ContentType, result.Value.Metadata.FileName);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _service.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
