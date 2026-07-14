using Microsoft.AspNetCore.Mvc;
using Modules.MasterData.Application;
using Platform.Api;

namespace Modules.MasterData.Api;

/// <summary>
/// Cost Center controller — inherits <see cref="PlatformApiController"/> for shared conventions (paging,
/// error envelope), same route/actor/exception-mapping pattern as <see cref="GLAccountsController"/>. Two
/// distinct actor literals enforce the Segregation of Duties split (maintainer can't be approver).
/// </summary>
[Route("api/v1/masterdata/cost-centers")]
public sealed class CostCentersController : PlatformApiController
{
    private const string MaintainerActor = "system/ui";
    private const string ApproverActor = "system/approver";

    private readonly CostCenterService _service;

    public CostCentersController(CostCenterService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        ODataQuery query;
        try { query = ODataQuery.Parse(Request.Query); }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }

        var (items, totalCount) = await _service.ListAsync(query.Skip, query.Top, cancellationToken);
        return Ok(new PagedResult<CostCenterDto>(items, totalCount, query.Skip, query.Top));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var costCenter = await _service.GetAsync(id, cancellationToken);
        return costCenter is null ? NotFound() : Ok(costCenter);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCostCenterRequest request, CancellationToken cancellationToken)
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

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCostCenterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _service.UpdateAsync(id, request, MaintainerActor, cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        try { return Ok(await _service.SubmitAsync(id, MaintainerActor, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
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
}
