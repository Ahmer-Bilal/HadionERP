using Microsoft.AspNetCore.Mvc;
using Modules.Finance.Application;
using Platform.Api;

namespace Modules.Finance.Api;

public sealed record SetTargetCloseDateRequest(DateOnly TargetCloseDate);

/// <summary>
/// Fiscal Year/Period controller — the Period Closing Center's own API surface
/// (`UI/Finance/d1f20165-...png`). No Maintainer/Approver split (see
/// <see cref="Domain.FiscalYear"/>'s own doc comment for why); the period-scoped read endpoints
/// (checklist/insights/activity-log/completion-trend) live here rather than a separate controller since
/// they all take the same fiscal-year/period route.
/// </summary>
[Route("api/v1/finance/fiscal-years")]
public sealed class FiscalYearsController : PlatformApiController
{
    private readonly FiscalYearService _fiscalYearService;
    private readonly ClosingActivityService _closingActivityService;

    public FiscalYearsController(FiscalYearService fiscalYearService, ClosingActivityService closingActivityService)
    {
        _fiscalYearService = fiscalYearService;
        _closingActivityService = closingActivityService;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) =>
        Ok(await _fiscalYearService.ListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var year = await _fiscalYearService.GetAsync(id, cancellationToken);
        return year is null ? NotFound() : Ok(year);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFiscalYearRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _fiscalYearService.CreateAsync(request, CurrentActor, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
    }

    [HttpPost("{id:guid}/periods/{periodNumber:int}/close")]
    public async Task<IActionResult> ClosePeriod(Guid id, int periodNumber, CancellationToken cancellationToken)
    {
        try { return Ok(await _fiscalYearService.ClosePeriodAsync(id, periodNumber, CurrentActor, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
    }

    [HttpPost("{id:guid}/periods/{periodNumber:int}/reopen")]
    public async Task<IActionResult> ReopenPeriod(Guid id, int periodNumber, CancellationToken cancellationToken)
    {
        try { return Ok(await _fiscalYearService.ReopenPeriodAsync(id, periodNumber, CurrentActor, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
    }

    [HttpPut("{id:guid}/periods/{periodNumber:int}/target-close-date")]
    public async Task<IActionResult> SetTargetCloseDate(
        Guid id, int periodNumber, [FromBody] SetTargetCloseDateRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await _fiscalYearService.SetTargetCloseDateAsync(id, periodNumber, request.TargetCloseDate, CurrentActor, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }
        catch (UnauthorizedAccessException ex) { return ForbiddenError(ex.Message); }
    }

    [HttpGet("{id:guid}/periods/{periodNumber:int}/closing-checklist")]
    public async Task<IActionResult> GetClosingChecklist(Guid id, int periodNumber, CancellationToken cancellationToken)
    {
        try { return Ok(await _closingActivityService.ListForPeriodAsync(id, periodNumber, CurrentActor, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }
    }

    [HttpGet("{id:guid}/periods/{periodNumber:int}/insights")]
    public async Task<IActionResult> GetInsights(Guid id, int periodNumber, CancellationToken cancellationToken)
    {
        try { return Ok(await _closingActivityService.GetInsightsAsync(id, periodNumber, CurrentActor, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }
    }

    [HttpGet("{id:guid}/periods/{periodNumber:int}/activity-log")]
    public async Task<IActionResult> GetActivityLog(Guid id, int periodNumber, [FromQuery] int take, CancellationToken cancellationToken)
    {
        try { return Ok(await _closingActivityService.GetActivityLogAsync(id, periodNumber, take <= 0 ? 20 : take, CurrentActor, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }
    }

    [HttpGet("{id:guid}/periods/{periodNumber:int}/completion-trend")]
    public async Task<IActionResult> GetCompletionTrend(Guid id, int periodNumber, CancellationToken cancellationToken)
    {
        try { return Ok(await _closingActivityService.GetCompletionTrendAsync(id, periodNumber, CurrentActor, cancellationToken)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }
    }
}
