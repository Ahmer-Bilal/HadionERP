using Microsoft.AspNetCore.Mvc;
using Modules.Finance.Application;
using Platform.Api;

namespace Modules.Finance.Api;

public sealed record ReverseJournalEntryRequest(DateOnly? ReversalDate = null);

/// <summary>
/// Journal Entry controller — inherits <see cref="PlatformApiController"/> for shared conventions (paging,
/// error envelope), same route/actor/exception-mapping pattern as Modules.MasterData's controllers. Two
/// distinct actor literals enforce the Segregation of Duties split (maintainer can't be approver/poster).
/// </summary>
[Route("api/v1/finance/journal-entries")]
public sealed class JournalEntriesController : PlatformApiController
{
    private const string MaintainerActor = "system/ui";
    private const string ApproverActor = "system/approver";

    private readonly JournalEntryService _service;

    public JournalEntriesController(JournalEntryService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        ODataQuery query;
        try { query = ODataQuery.Parse(Request.Query); }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }

        var (items, totalCount) = await _service.ListAsync(query.Skip, query.Top, cancellationToken);
        return Ok(new PagedResult<JournalEntryDto>(items, totalCount, query.Skip, query.Top));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var entry = await _service.GetAsync(id, cancellationToken);
        return entry is null ? NotFound() : Ok(entry);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateJournalEntryRequest request, CancellationToken cancellationToken)
    {
        const string companyId = "C001";
        try
        {
            var created = await _service.CreateAsync(request, MaintainerActor, companyId, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        try { return Ok(await _service.SubmitAsync(id, MaintainerActor, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
        catch (InvalidOperationException ex) { return ConflictError(ex.Message); }
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        try { return Ok(await _service.ApproveAsync(id, ApproverActor, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
        catch (InvalidOperationException ex) { return ConflictError(ex.Message); }
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, CancellationToken cancellationToken)
    {
        try { return Ok(await _service.RejectAsync(id, ApproverActor, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
        catch (InvalidOperationException ex) { return ConflictError(ex.Message); }
    }

    [HttpPost("{id:guid}/post")]
    public async Task<IActionResult> Post(Guid id, CancellationToken cancellationToken)
    {
        try { return Ok(await _service.PostAsync(id, ApproverActor, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
        catch (InvalidOperationException ex) { return ConflictError(ex.Message); }
    }

    [HttpPost("{id:guid}/reverse")]
    public async Task<IActionResult> Reverse(Guid id, [FromBody] ReverseJournalEntryRequest? request, CancellationToken cancellationToken)
    {
        var reversalDate = request?.ReversalDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        try { return Ok(await _service.ReverseAsync(id, ApproverActor, reversalDate, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
        catch (InvalidOperationException ex) { return ConflictError(ex.Message); }
    }
}
