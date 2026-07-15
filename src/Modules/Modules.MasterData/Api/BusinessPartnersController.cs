using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Modules.MasterData.Application;
using Platform.Api;
using Platform.Attachments;

namespace Modules.MasterData.Api;

/// <summary>
/// The first business-module controller — inherits <see cref="PlatformApiController"/> for the shared
/// conventions (paging, error envelope), with its own explicit kebab-case route
/// (docs/architecture/05-engineering-standards.md #2: "API route: kebab-case, plural resource") since the
/// base's default `[controller]` token can't produce a hyphen for a compound resource name.
///
/// Every action below uses <see cref="PlatformApiController.CurrentActor"/> — the real logged-in user's
/// username, resolved from the validated JWT (`ARCHITECTURE-AUDIT.md` Part 1 §1, closed by
/// `Modules.Identity`). What used to be two hardcoded actor literals impersonating "the maintainer" and
/// "the approver" is now whichever real user is actually logged in; the Segregation of Duties split
/// registered in <see cref="Modules.MasterData.Application.BusinessPartnerSecurity"/> (a real fraud/
/// compliance control, per <see cref="Modules.MasterData.Domain.BusinessPartner"/>'s own doc comment) is
/// enforced by real role assignment (a user simply can't hold both the Maintainer and Approver role, or
/// `Modules.Identity.Application.UserService.AssignRoleAsync` blocks it), not by which literal a request
/// happened to use.
/// </summary>
[Route("api/v1/masterdata/business-partners")]
public sealed class BusinessPartnersController : PlatformApiController
{
    private readonly BusinessPartnerService _service;

    public BusinessPartnersController(BusinessPartnerService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        ODataQuery query;
        try
        {
            query = ODataQuery.Parse(Request.Query);
        }
        catch (ArgumentException ex)
        {
            return BadRequestError(ex.Message);
        }

        var (items, totalCount) = await _service.ListAsync(query.Skip, query.Top, cancellationToken);
        return Ok(new PagedResult<BusinessPartnerDto>(items, totalCount, query.Skip, query.Top));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var partner = await _service.GetAsync(id, cancellationToken);
        return partner is null ? NotFound() : Ok(partner);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBusinessPartnerRequest request, CancellationToken cancellationToken)
    {
        // The company should come from the selected company context once real multi-company selection
        // exists — see Modules.MasterData/README.md for what's deferred.
        const string companyId = "C001";

        try
        {
            var created = await _service.CreateAsync(request, CurrentActor, companyId, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequestError(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ForbiddenError(ex.Message);
        }
    }

    [HttpPost("{id:guid}/addresses")]
    public async Task<IActionResult> AddAddress(Guid id, [FromBody] AddBusinessPartnerAddressRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.AddAddressAsync(id, request, CurrentActor, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequestError(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ForbiddenError(ex.Message);
        }
    }

    [HttpPost("{id:guid}/contacts")]
    public async Task<IActionResult> AddContact(Guid id, [FromBody] AddBusinessPartnerContactRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.AddContactAsync(id, request, CurrentActor, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequestError(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ForbiddenError(ex.Message);
        }
    }

    [HttpPost("{id:guid}/business-roles")]
    public async Task<IActionResult> AddBusinessRole(Guid id, [FromBody] AddBusinessRoleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.AddBusinessRoleAsync(id, request, CurrentActor, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequestError(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return ConflictError(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ForbiddenError(ex.Message);
        }
    }

    [HttpDelete("{id:guid}/business-roles/{roleId:guid}")]
    public async Task<IActionResult> RemoveBusinessRole(Guid id, Guid roleId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.RemoveBusinessRoleAsync(id, roleId, CurrentActor, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException ex)
        {
            return ForbiddenError(ex.Message);
        }
    }

    [HttpPost("{id:guid}/attachments")]
    [RequestSizeLimit(AttachmentService.MaxSizeBytes)]
    public async Task<IActionResult> AddAttachment(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequestError("A file is required.");
        }

        await using var stream = file.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);

        try
        {
            var attachment = await _service.AddAttachmentAsync(
                id, file.FileName, file.ContentType, memoryStream.ToArray(), CurrentActor, cancellationToken);
            return Ok(attachment);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequestError(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ForbiddenError(ex.Message);
        }
    }

    [HttpGet("{id:guid}/attachments")]
    public async Task<IActionResult> ListAttachments(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.ListAttachmentsAsync(id, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("{id:guid}/attachments/{attachmentId:guid}/content")]
    public async Task<IActionResult> DownloadAttachment(Guid id, Guid attachmentId, CancellationToken cancellationToken)
    {
        var download = await _service.DownloadAttachmentAsync(id, attachmentId, cancellationToken);
        if (download is null)
        {
            return NotFound();
        }

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
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException ex)
        {
            return ForbiddenError(ex.Message);
        }
    }

    [HttpPost("{id:guid}/notes")]
    public async Task<IActionResult> AddNote(Guid id, [FromBody] AddBusinessPartnerNoteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.AddNoteAsync(id, request.Text, CurrentActor, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequestError(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ForbiddenError(ex.Message);
        }
    }

    [HttpGet("{id:guid}/notes")]
    public async Task<IActionResult> ListNotes(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.ListNotesAsync(id, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id:guid}/notes/{noteId:guid}")]
    public async Task<IActionResult> DeleteNote(Guid id, Guid noteId, CancellationToken cancellationToken)
    {
        try
        {
            await _service.DeleteNoteAsync(id, noteId, CurrentActor, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException ex)
        {
            return ForbiddenError(ex.Message);
        }
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.SubmitAsync(id, CurrentActor, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return ConflictError(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ForbiddenError(ex.Message);
        }
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.ApproveAsync(id, CurrentActor, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return ConflictError(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ForbiddenError(ex.Message);
        }
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.RejectAsync(id, CurrentActor, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return ConflictError(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ForbiddenError(ex.Message);
        }
    }
}
