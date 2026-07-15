using Modules.MasterData.Domain;
using Platform.Audit;
using Platform.Core;
using Platform.Security;

namespace Modules.MasterData.Application;

/// <summary>
/// Application service for the admin-configurable lookup engine — see
/// <see cref="Modules.MasterData.Domain.LookupType"/>'s doc comment for why this has no workflow/lifecycle
/// (immediate-effect, single-privilege-gated, same as SAP domain-value maintenance / D365 Option Sets).
/// </summary>
public sealed class LookupService
{
    private const string TypeAuditTargetType = "LookupType";
    private const string ValueAuditTargetType = "LookupValue";
    private const string AuditSource = "Modules.MasterData";

    private readonly ILookupRepository _repository;
    private readonly IAuditRecorder _auditRecorder;
    private readonly IAuthorizationService _authorizationService;
    private readonly IActorRoleAssignmentStore _actorRoleAssignmentStore;

    public LookupService(
        ILookupRepository repository,
        IAuditRecorder auditRecorder,
        IAuthorizationService authorizationService,
        IActorRoleAssignmentStore actorRoleAssignmentStore)
    {
        _repository = repository;
        _auditRecorder = auditRecorder;
        _authorizationService = authorizationService;
        _actorRoleAssignmentStore = actorRoleAssignmentStore;
    }

    public async Task<IReadOnlyList<LookupTypeDto>> ListTypesAsync(CancellationToken cancellationToken = default)
    {
        var types = await _repository.ListTypesAsync(cancellationToken);
        var result = new List<LookupTypeDto>(types.Count);
        foreach (var type in types)
        {
            var count = await _repository.CountValuesAsync(type.Code, cancellationToken);
            result.Add(ToDto(type, count));
        }
        return result;
    }

    public async Task<LookupTypeDto> CreateTypeAsync(
        CreateLookupTypeRequest request, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor);

        var existing = await _repository.GetTypeAsync(request.Code, cancellationToken);
        if (existing is not null)
            throw new ArgumentException($"Lookup type '{request.Code}' already exists.");

        var type = new LookupType(actor, request.Code, request.Name, request.NameArabic, isSystemDefined: false);
        _repository.AddType(type);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordCreate(TypeAuditReference(type.Id), actor,
            $"Lookup type '{type.Code}' ({type.Name}) created.", AuditSource);

        return ToDto(type, 0);
    }

    public async Task<LookupTypeDto> UpdateTypeAsync(
        string code, UpdateLookupTypeRequest request, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor);
        var type = await RequireTypeAsync(code, cancellationToken);

        type.Rename(actor, request.Name, request.NameArabic);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordFieldUpdate(TypeAuditReference(type.Id), actor,
            $"Lookup type '{type.Code}' renamed.", Array.Empty<FieldValueChange>(), AuditSource);

        var count = await _repository.CountValuesAsync(type.Code, cancellationToken);
        return ToDto(type, count);
    }

    public async Task DeleteTypeAsync(string code, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor);
        var type = await RequireTypeAsync(code, cancellationToken);

        if (type.IsSystemDefined)
            throw new InvalidOperationException(
                $"Lookup type '{type.Code}' is used by this platform's own code and cannot be deleted — deactivate or remove its individual values instead.");

        var valueCount = await _repository.CountValuesAsync(type.Code, cancellationToken);
        if (valueCount > 0)
            throw new InvalidOperationException(
                $"Lookup type '{type.Code}' still has {valueCount} value(s) — remove them first.");

        _repository.RemoveType(type);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordDeleteAttempt(TypeAuditReference(type.Id), actor,
            $"Lookup type '{type.Code}' deleted.", AuditSource);
    }

    public async Task<IReadOnlyList<LookupValueDto>> ListValuesAsync(
        string lookupTypeCode, bool includeInactive = true, CancellationToken cancellationToken = default)
    {
        await RequireTypeAsync(lookupTypeCode, cancellationToken);
        var values = await _repository.ListValuesAsync(lookupTypeCode, includeInactive, cancellationToken);
        return values.OrderBy(v => v.SortOrder).ThenBy(v => v.Code).Select(ToDto).ToList();
    }

    public async Task<LookupValueDto> CreateValueAsync(
        string lookupTypeCode, CreateLookupValueRequest request, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor);
        await RequireTypeAsync(lookupTypeCode, cancellationToken);

        var existing = await _repository.GetValueByCodeAsync(lookupTypeCode, request.Code, cancellationToken);
        if (existing is not null)
            throw new ArgumentException($"'{request.Code}' already exists in lookup type '{lookupTypeCode}'.");

        var value = new LookupValue(actor, lookupTypeCode, request.Code, request.Name, request.NameArabic, request.SortOrder);
        _repository.AddValue(value);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordCreate(ValueAuditReference(value.Id), actor,
            $"Lookup value '{value.Code}' ({value.Name}) added to '{lookupTypeCode}'.", AuditSource);

        return ToDto(value);
    }

    public async Task<LookupValueDto> UpdateValueAsync(
        string lookupTypeCode, Guid id, UpdateLookupValueRequest request, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor);
        var value = await RequireValueAsync(lookupTypeCode, id, cancellationToken);

        value.Update(actor, request.Name, request.NameArabic, request.SortOrder);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordFieldUpdate(ValueAuditReference(value.Id), actor,
            $"Lookup value '{value.Code}' updated.", Array.Empty<FieldValueChange>(), AuditSource);

        return ToDto(value);
    }

    public async Task<LookupValueDto> SetActiveAsync(
        string lookupTypeCode, Guid id, bool isActive, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor);
        var value = await RequireValueAsync(lookupTypeCode, id, cancellationToken);

        if (isActive) value.Activate(actor); else value.Deactivate(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordFieldUpdate(ValueAuditReference(value.Id), actor,
            $"Lookup value '{value.Code}' {(isActive ? "activated" : "deactivated")}.", Array.Empty<FieldValueChange>(), AuditSource);

        return ToDto(value);
    }

    public async Task DeleteValueAsync(string lookupTypeCode, Guid id, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor);
        var value = await RequireValueAsync(lookupTypeCode, id, cancellationToken);

        var inUse = await _repository.IsValueInUseAsync(lookupTypeCode, value.Code, cancellationToken);
        if (inUse)
            throw new InvalidOperationException(
                $"'{value.Code}' is referenced by existing records and cannot be deleted — deactivate it instead so existing records stay valid.");

        _repository.RemoveValue(value);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordDeleteAttempt(ValueAuditReference(value.Id), actor,
            $"Lookup value '{value.Code}' deleted from '{lookupTypeCode}'.", AuditSource);
    }

    private void RequireAuthorization(string actor)
    {
        var result = _authorizationService.Authorize(BuildPrincipal(actor), LookupSecurity.AdministerPrivilegeKey);
        if (!result.Allowed) throw new UnauthorizedAccessException(result.Reason);
    }

    private SecurityPrincipal BuildPrincipal(string actor) =>
        new(actor, _actorRoleAssignmentStore.ResolveRoleKeys(actor), new Dictionary<string, IReadOnlySet<string>>());

    private async Task<LookupType> RequireTypeAsync(string code, CancellationToken cancellationToken) =>
        await _repository.GetTypeAsync(code, cancellationToken)
            ?? throw new KeyNotFoundException($"Lookup type '{code}' was not found.");

    private async Task<LookupValue> RequireValueAsync(string lookupTypeCode, Guid id, CancellationToken cancellationToken)
    {
        var value = await _repository.GetValueAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Lookup value {id} was not found.");
        if (!string.Equals(value.LookupTypeCode, lookupTypeCode, StringComparison.Ordinal))
            throw new KeyNotFoundException($"Lookup value {id} does not belong to lookup type '{lookupTypeCode}'.");
        return value;
    }

    private static BusinessObjectReference TypeAuditReference(Guid id) => new(id, TypeAuditTargetType, "Self");

    private static BusinessObjectReference ValueAuditReference(Guid id) => new(id, ValueAuditTargetType, "Self");

    private static LookupTypeDto ToDto(LookupType t, int valueCount) =>
        new(t.Id, t.Code, t.Name, t.NameArabic, t.IsSystemDefined, valueCount);

    private static LookupValueDto ToDto(LookupValue v) =>
        new(v.Id, v.LookupTypeCode, v.Code, v.Name, v.NameArabic, v.IsActive, v.SortOrder);
}
