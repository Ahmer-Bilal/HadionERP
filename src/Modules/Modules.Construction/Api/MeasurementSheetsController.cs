using Microsoft.AspNetCore.Mvc;
using Modules.Construction.Application;
using Platform.Api;

namespace Modules.Construction.Api;

/// <summary>
/// Measurement Sheet controller — inherits <see cref="PlatformApiController"/> for shared conventions, same
/// route/actor/exception-mapping pattern as every other module's controller. The certify action is POST
/// with a body (per-line certified quantities), unlike every other module's parameterless approve — see
/// <see cref="MeasurementSheetService.CertifyAsync"/> for why.
/// </summary>
[Route("api/v1/construction/measurement-sheets")]
public sealed class MeasurementSheetsController : PlatformApiController
{
    private readonly MeasurementSheetService _service;

    public MeasurementSheetsController(MeasurementSheetService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        ODataQuery query;
        try { query = ODataQuery.Parse(Request.Query); }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }

        var (items, totalCount) = await _service.ListAsync(query.Skip, query.Top, cancellationToken);
        return Ok(new PagedResult<MeasurementSheetDto>(items, totalCount, query.Skip, query.Top));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var sheet = await _service.GetAsync(id, cancellationToken);
        return sheet is null ? NotFound() : Ok(sheet);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMeasurementSheetRequest request, CancellationToken cancellationToken)
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

    [HttpPost("{id:guid}/certify")]
    public async Task<IActionResult> Certify(Guid id, [FromBody] CertifyMeasurementSheetRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await _service.CertifyAsync(id, request, CurrentActor, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }
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
