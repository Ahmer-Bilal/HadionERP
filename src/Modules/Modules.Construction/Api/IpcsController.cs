using Microsoft.AspNetCore.Mvc;
using Modules.Construction.Application;
using Platform.Api;

namespace Modules.Construction.Api;

/// <summary>
/// IPC controller — inherits <see cref="PlatformApiController"/> for shared conventions, same
/// route/actor/exception-mapping pattern as every other module's controller.
/// </summary>
[Route("api/v1/construction/ipcs")]
public sealed class IpcsController : PlatformApiController
{
    private readonly IpcService _service;

    public IpcsController(IpcService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        ODataQuery query;
        try { query = ODataQuery.Parse(Request.Query); }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }

        var (items, totalCount) = await _service.ListAsync(query.Skip, query.Top, cancellationToken);
        return Ok(new PagedResult<IpcDto>(items, totalCount, query.Skip, query.Top));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var ipc = await _service.GetAsync(id, cancellationToken);
        return ipc is null ? NotFound() : Ok(ipc);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateIpcRequest request, CancellationToken cancellationToken)
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
}
