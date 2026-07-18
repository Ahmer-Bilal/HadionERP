using Microsoft.AspNetCore.Mvc;
using Modules.Finance.Application;
using Platform.Api;

namespace Modules.Finance.Api;

/// <summary>
/// Finance reporting endpoints — read-only aggregations over already-posted documents, not a Business
/// Object of their own (see <see cref="TrialBalanceService"/>'s own doc comment). Separate controller from
/// <see cref="JournalEntriesController"/> since these actions aren't CRUD/lifecycle transitions on one
/// document type — a growing report family (Income Statement, Balance Sheet next) belongs together under
/// its own route rather than bolted onto the Journal Entry document controller.
/// </summary>
[Route("api/v1/finance/reports")]
public sealed class ReportsController : PlatformApiController
{
    private readonly TrialBalanceService _trialBalanceService;
    private readonly IncomeStatementService _incomeStatementService;
    private readonly BalanceSheetService _balanceSheetService;

    public ReportsController(
        TrialBalanceService trialBalanceService,
        IncomeStatementService incomeStatementService,
        BalanceSheetService balanceSheetService)
    {
        _trialBalanceService = trialBalanceService;
        _incomeStatementService = incomeStatementService;
        _balanceSheetService = balanceSheetService;
    }

    [HttpGet("trial-balance")]
    public async Task<IActionResult> TrialBalance([FromQuery] DateOnly periodStart, [FromQuery] DateOnly periodEnd, CancellationToken cancellationToken)
    {
        try { return Ok(await _trialBalanceService.GetAsync(periodStart, periodEnd, cancellationToken)); }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }
    }

    [HttpGet("income-statement")]
    public async Task<IActionResult> IncomeStatement(
        [FromQuery] DateOnly periodStart, [FromQuery] DateOnly periodEnd,
        [FromQuery] DateOnly? comparePeriodStart, [FromQuery] DateOnly? comparePeriodEnd,
        CancellationToken cancellationToken)
    {
        try { return Ok(await _incomeStatementService.GetAsync(periodStart, periodEnd, comparePeriodStart, comparePeriodEnd, cancellationToken)); }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }
    }

    [HttpGet("balance-sheet")]
    public async Task<IActionResult> BalanceSheet(
        [FromQuery] DateOnly asOfDate, [FromQuery] DateOnly? compareAsOfDate, CancellationToken cancellationToken)
    {
        try { return Ok(await _balanceSheetService.GetAsync(asOfDate, compareAsOfDate, cancellationToken)); }
        catch (ArgumentException ex) { return BadRequestError(ex.Message); }
    }
}
