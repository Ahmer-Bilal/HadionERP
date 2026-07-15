using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Modules.Procurement.Application;
using Platform.Api;
using Platform.Attachments;

namespace Modules.Procurement.Api;

/// <summary>
/// Vendor Prequalification controller — inherits <see cref="PlatformApiController"/> for shared conventions,
/// same route/actor/exception-mapping pattern as every other module's controller. The five review-step
/// decisions (Commercial/Legal/Technical/HSE/Quality) all funnel through the same approve/reject endpoints —
/// <see cref="Platform.Workflow.WorkflowEngine"/> resolves which step is actually current from the workflow
/// instance itself, so the controller doesn't need one endpoint per step.
/// </summary>
[Route("api/v1/procurement/vendor-prequalifications")]
public sealed class VendorPrequalificationsController : PlatformApiController
{
    private readonly VendorPrequalificationService _service;

    public VendorPrequalificationsController(VendorPrequalificationService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        ODataQuery query;
        try { query = ODataQuery.Parse(Request.Query); }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }

        var (items, totalCount) = await _service.ListAsync(query.Skip, query.Top, cancellationToken);
        return Ok(new PagedResult<VendorPrequalificationDto>(items, totalCount, query.Skip, query.Top));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var prequalification = await _service.GetAsync(id, cancellationToken);
        return prequalification is null ? NotFound() : Ok(prequalification);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVendorPrequalificationRequest request, CancellationToken cancellationToken)
    {
        const string companyId = "C001";
        try
        {
            var created = await _service.CreateAsync(request, CurrentActor, companyId, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        try { return Ok(await _service.SubmitAsync(id, CurrentActor, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
        catch (InvalidOperationException ex) { return ConflictError(ex.Message); }
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        try { return Ok(await _service.ApproveAsync(id, CurrentActor, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
        catch (InvalidOperationException ex) { return ConflictError(ex.Message); }
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, CancellationToken cancellationToken)
    {
        try { return Ok(await _service.RejectAsync(id, CurrentActor, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
        catch (InvalidOperationException ex) { return ConflictError(ex.Message); }
    }

    [HttpPost("{id:guid}/attachments")]
    [RequestSizeLimit(AttachmentService.MaxSizeBytes)]
    public async Task<IActionResult> AddAttachment(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0) return BadRequestError("A file is required.");

        await using var stream = file.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);

        try
        {
            var attachment = await _service.AddAttachmentAsync(
                id, file.FileName, file.ContentType, memoryStream.ToArray(), CurrentActor, cancellationToken);
            return Ok(attachment);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
    }

    [HttpGet("{id:guid}/attachments")]
    public async Task<IActionResult> ListAttachments(Guid id, CancellationToken cancellationToken)
    {
        try { return Ok(await _service.ListAttachmentsAsync(id, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpGet("{id:guid}/attachments/{attachmentId:guid}/content")]
    public async Task<IActionResult> DownloadAttachment(Guid id, Guid attachmentId, CancellationToken cancellationToken)
    {
        var download = await _service.DownloadAttachmentAsync(id, attachmentId, cancellationToken);
        if (download is null) return NotFound();

        return File(download.Value.Content, download.Value.Metadata.ContentType, download.Value.Metadata.FileName);
    }

    [HttpDelete("{id:guid}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DeleteAttachment(Guid id, Guid attachmentId, CancellationToken cancellationToken)
    {
        try
        {
            await _service.DeleteAttachmentAsync(id, attachmentId, CurrentActor, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
    }
}
