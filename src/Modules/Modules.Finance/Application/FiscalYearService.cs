using Modules.Finance.Domain;
using Platform.Audit;
using Platform.Core;
using Platform.Security;

namespace Modules.Finance.Application;

/// <summary>
/// Application service for Fiscal Years/Periods — see <see cref="FiscalYear"/>'s own doc comment for why
/// this has no workflow/lifecycle (immediate-effect, single-privilege-gated, same as
/// <see cref="Modules.MasterData.Application.LookupService"/>).
/// </summary>
public sealed class FiscalYearService
{
    private const string AuditTargetType = "FiscalYear";
    private const string AuditSource = "Modules.Finance";

    private readonly IFiscalYearRepository _repository;
    private readonly IAuditRecorder _auditRecorder;
    private readonly IAuthorizationService _authorizationService;
    private readonly IActorRoleAssignmentStore _actorRoleAssignmentStore;

    public FiscalYearService(
        IFiscalYearRepository repository,
        IAuditRecorder auditRecorder,
        IAuthorizationService authorizationService,
        IActorRoleAssignmentStore actorRoleAssignmentStore)
    {
        _repository = repository;
        _auditRecorder = auditRecorder;
        _authorizationService = authorizationService;
        _actorRoleAssignmentStore = actorRoleAssignmentStore;
    }

    public async Task<FiscalYearDto> CreateAsync(
        CreateFiscalYearRequest request, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor);

        var existing = await _repository.GetByYearAsync(request.Year, cancellationToken);
        if (existing is not null)
            throw new ArgumentException($"Fiscal year {request.Year} already exists.");

        var fiscalYear = new FiscalYear(actor, request.Year);
        _repository.Add(fiscalYear);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordCreate(AuditReference(fiscalYear.Id), actor,
            $"Fiscal year {fiscalYear.Year} opened with its 12 monthly periods.", AuditSource);

        return ToDto(fiscalYear);
    }

    public async Task<FiscalYearDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var fiscalYear = await _repository.GetAsync(id, cancellationToken);
        return fiscalYear is null ? null : ToDto(fiscalYear);
    }

    public async Task<IReadOnlyList<FiscalYearDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var years = await _repository.ListAsync(cancellationToken);
        return years.OrderByDescending(y => y.Year).Select(ToDto).ToList();
    }

    public Task<FiscalYearDto> ClosePeriodAsync(Guid fiscalYearId, int periodNumber, string actor, CancellationToken cancellationToken = default) =>
        SetPeriodOpenAsync(fiscalYearId, periodNumber, isOpen: false, actor, cancellationToken);

    public Task<FiscalYearDto> ReopenPeriodAsync(Guid fiscalYearId, int periodNumber, string actor, CancellationToken cancellationToken = default) =>
        SetPeriodOpenAsync(fiscalYearId, periodNumber, isOpen: true, actor, cancellationToken);

    public async Task<FiscalYearDto> SetTargetCloseDateAsync(
        Guid fiscalYearId, int periodNumber, DateOnly targetCloseDate, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor);
        var fiscalYear = await _repository.GetAsync(fiscalYearId, cancellationToken)
            ?? throw new KeyNotFoundException($"Fiscal year {fiscalYearId} was not found.");
        var period = fiscalYear.Periods.FirstOrDefault(p => p.PeriodNumber == periodNumber)
            ?? throw new ArgumentException($"Fiscal year {fiscalYear.Year} has no period {periodNumber}.");

        period.SetTargetCloseDate(targetCloseDate, actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordFieldUpdate(AuditReference(fiscalYear.Id), actor,
            $"Fiscal year {fiscalYear.Year}, period {periodNumber} target close date set to {targetCloseDate}.",
            Array.Empty<FieldValueChange>(), AuditSource);

        return ToDto(fiscalYear);
    }

    private async Task<FiscalYearDto> SetPeriodOpenAsync(
        Guid fiscalYearId, int periodNumber, bool isOpen, string actor, CancellationToken cancellationToken)
    {
        RequireAuthorization(actor);
        var fiscalYear = await _repository.GetAsync(fiscalYearId, cancellationToken)
            ?? throw new KeyNotFoundException($"Fiscal year {fiscalYearId} was not found.");

        var period = fiscalYear.Periods.FirstOrDefault(p => p.PeriodNumber == periodNumber)
            ?? throw new ArgumentException($"Fiscal year {fiscalYear.Year} has no period {periodNumber}.");

        if (isOpen) period.Reopen(actor); else period.Close(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordFieldUpdate(AuditReference(fiscalYear.Id), actor,
            $"Fiscal year {fiscalYear.Year}, period {periodNumber} {(isOpen ? "reopened" : "closed")}.",
            Array.Empty<FieldValueChange>(), AuditSource);

        return ToDto(fiscalYear);
    }

    private void RequireAuthorization(string actor)
    {
        var result = _authorizationService.Authorize(BuildPrincipal(actor), FiscalYearSecurity.AdministerPrivilegeKey);
        if (!result.Allowed) throw new UnauthorizedAccessException(result.Reason);
    }

    private SecurityPrincipal BuildPrincipal(string actor) =>
        new(actor, _actorRoleAssignmentStore.ResolveRoleKeys(actor), new Dictionary<string, IReadOnlySet<string>>());

    private static BusinessObjectReference AuditReference(Guid fiscalYearId) => new(fiscalYearId, AuditTargetType, "Self");

    private static FiscalYearDto ToDto(FiscalYear y) => new(
        y.Id, y.Year,
        y.Periods.OrderBy(p => p.PeriodNumber).Select(p => new FiscalPeriodDto(p.Id, p.PeriodNumber, p.StartDate, p.EndDate, p.IsOpen, p.TargetCloseDate)).ToList(),
        y.CreatedAt, y.CreatedBy);
}
