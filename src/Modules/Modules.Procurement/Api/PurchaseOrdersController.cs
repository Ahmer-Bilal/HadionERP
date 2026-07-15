using Microsoft.AspNetCore.Mvc;
using Modules.Procurement.Application;
using Platform.Api;

namespace Modules.Procurement.Api;

/// <summary>
/// Purchase Order controller — inherits <see cref="PlatformApiController"/> for shared conventions, same
/// route/actor/exception-mapping pattern as every other module's controller.
/// </summary>
[Route("api/v1/procurement/purchase-orders")]
public sealed class PurchaseOrdersController : PlatformApiController
{
    private readonly PurchaseOrderService _service;
    private readonly ThreeWayMatchService _threeWayMatchService;

    public PurchaseOrdersController(PurchaseOrderService service, ThreeWayMatchService threeWayMatchService)
    {
        _service = service;
        _threeWayMatchService = threeWayMatchService;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        ODataQuery query;
        try { query = ODataQuery.Parse(Request.Query); }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }

        var (items, totalCount) = await _service.ListAsync(query.Skip, query.Top, cancellationToken);
        return Ok(new PagedResult<PurchaseOrderDto>(items, totalCount, query.Skip, query.Top));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var po = await _service.GetAsync(id, cancellationToken);
        return po is null ? NotFound() : Ok(po);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderRequest request, CancellationToken cancellationToken)
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

    /// <summary>The 3-way match check (Ordered vs Received vs Invoiced) — computed on demand, nothing
    /// persisted. See <see cref="ThreeWayMatchService"/>'s own doc comment for why this lives here.</summary>
    [HttpGet("{id:guid}/three-way-match")]
    public async Task<IActionResult> ThreeWayMatch(Guid id, [FromQuery] Guid apInvoiceId, CancellationToken cancellationToken)
    {
        try { return Ok(await _threeWayMatchService.CheckAsync(id, apInvoiceId, cancellationToken)); }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }
    }
}
