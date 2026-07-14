using System.Text.Json;
using Modules.MasterData.Contracts;
using Modules.Procurement.Domain;
using Platform.Attachments;
using Platform.Audit;
using Platform.Configuration;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;

namespace Modules.Procurement.Application;

public sealed class VendorPrequalificationService
{
    public const string NumberRangeKey = "PROC-VPQ";

    /// <summary>How many months an Approved certificate stays valid from its approval date — a real
    /// Platform.Configuration key (docs/architecture/04-data-and-api.md #3's "don't hard-code business
    /// rules" applies here just as much as to a tax rate or approval threshold), overridable per Tenant/
    /// Company through the same future admin UI every other configured value in this codebase awaits.
    /// Defaults to 24 months, matching the common Saudi EPC/construction industry prequalification
    /// renewal cycle.</summary>
    public const string ValidityMonthsConfigurationKey = "Procurement.VendorPrequalification.ValidityMonths";

    /// <summary>Excluded outright, per docs/architecture/06-roadmap.md's Vendor Prequalification design:
    /// "Government Authority is not prequalified at all" — there is no commercial relationship to qualify.</summary>
    private const string GovernmentAuthorityRoleType = "GovernmentAuthority";

    private const string AuditTargetType = "VendorPrequalification";
    private const string AuditSource = "Modules.Procurement";

    private readonly IVendorPrequalificationRepository _repository;
    private readonly INumberRangeService _numberRangeService;
    private readonly IAuditRecorder _auditRecorder;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IWorkflowInstanceRepository _workflowInstanceRepository;
    private readonly IAuthorizationService _authorizationService;
    private readonly IActorRoleAssignmentStore _actorRoleAssignmentStore;
    private readonly IBusinessPartnerLookup _businessPartnerLookup;
    private readonly IConfigurationResolver _configurationResolver;
    private readonly IAttachmentService _attachmentService;

    public VendorPrequalificationService(
        IVendorPrequalificationRepository repository,
        INumberRangeService numberRangeService,
        IAuditRecorder auditRecorder,
        IWorkflowEngine workflowEngine,
        IWorkflowInstanceRepository workflowInstanceRepository,
        IAuthorizationService authorizationService,
        IActorRoleAssignmentStore actorRoleAssignmentStore,
        IBusinessPartnerLookup businessPartnerLookup,
        IConfigurationResolver configurationResolver,
        IAttachmentService attachmentService)
    {
        _repository = repository;
        _numberRangeService = numberRangeService;
        _auditRecorder = auditRecorder;
        _workflowEngine = workflowEngine;
        _workflowInstanceRepository = workflowInstanceRepository;
        _authorizationService = authorizationService;
        _actorRoleAssignmentStore = actorRoleAssignmentStore;
        _businessPartnerLookup = businessPartnerLookup;
        _configurationResolver = configurationResolver;
        _attachmentService = attachmentService;
    }

    public async Task<VendorPrequalificationDto> CreateAsync(
        CreateVendorPrequalificationRequest request, string actor, string companyId, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, VendorPrequalificationSecurity.MaintainPrivilegeKey);

        if (string.Equals(request.RoleType, GovernmentAuthorityRoleType, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("A Government Authority is not prequalified — it has no commercial relationship to qualify.");

        var vendor = await _businessPartnerLookup.GetAsync(request.BusinessPartnerId, cancellationToken)
            ?? throw new ArgumentException($"Business partner {request.BusinessPartnerId} was not found.");
        if (vendor.Status != "Approved")
            throw new ArgumentException($"Business partner '{vendor.Name}' is not Approved and cannot be prequalified yet.");
        if (!vendor.BusinessRoles.Any(r => string.Equals(r, request.RoleType, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"Business partner '{vendor.Name}' does not hold the '{request.RoleType}' role.");

        var prequalification = new VendorPrequalification(actor, request.BusinessPartnerId, request.RoleType, request.Trade);

        var documentNumber = _numberRangeService.GetNext(NumberRangeKey, companyId, DateTimeOffset.UtcNow.Year);
        prequalification.AssignNumber(documentNumber);

        _repository.Add(prequalification);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordCreate(AuditReference(prequalification.Id), actor,
            $"Vendor prequalification '{prequalification.DocumentNumber}' created for '{vendor.Name}' ({request.RoleType}).", AuditSource);

        return ToDto(prequalification);
    }

    public async Task<VendorPrequalificationDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var prequalification = await _repository.GetAsync(id, cancellationToken);
        return prequalification is null ? null : ToDto(prequalification);
    }

    public async Task<(IReadOnlyList<VendorPrequalificationDto> Items, int TotalCount)> ListAsync(
        int skip, int top, CancellationToken cancellationToken = default)
    {
        var items = await _repository.ListAsync(skip, top, cancellationToken);
        var total = await _repository.CountAsync(cancellationToken);
        return (items.Select(ToDto).ToList(), total);
    }

    public async Task<VendorPrequalificationDto> SubmitAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, VendorPrequalificationSecurity.MaintainPrivilegeKey);
        var prequalification = await RequirePrequalificationAsync(id, cancellationToken);

        var fromStatus = prequalification.Status;
        prequalification.Submit(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(prequalification.Id), actor,
            $"Vendor prequalification '{prequalification.DocumentNumber}' submitted for review.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(prequalification.Status.ToString()), AuditSource);

        var instance = _workflowEngine.Start(VendorPrequalificationWorkflow.BusinessObjectType, VendorPrequalificationWorkflow.SubmitTransition, prequalification.Id);
        if (instance is not null)
        {
            _workflowInstanceRepository.Add(instance);
            await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);
            if (instance.Status == WorkflowInstanceStatus.Approved)
                await ApproveInternalAsync(prequalification, actor, cancellationToken);
        }

        return ToDto(prequalification);
    }

    public Task<VendorPrequalificationDto> ApproveAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Approve, cancellationToken);

    public Task<VendorPrequalificationDto> RejectAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Reject, cancellationToken);

    private async Task<VendorPrequalificationDto> DecideApprovalAsync(
        Guid id, string actor, WorkflowDecision decision, CancellationToken cancellationToken)
    {
        RequireAuthorization(actor, VendorPrequalificationSecurity.ReviewPrivilegeKey);
        var prequalification = await RequirePrequalificationAsync(id, cancellationToken);
        var instance = await _workflowInstanceRepository.GetActiveAsync(VendorPrequalificationWorkflow.BusinessObjectType, prequalification.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Vendor prequalification {id} has no pending review step to decide.");

        _workflowEngine.Decide(instance, BuildPrincipal(actor), decision);
        await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);

        if (instance.Status == WorkflowInstanceStatus.Approved)
            await ApproveInternalAsync(prequalification, actor, cancellationToken);
        else if (instance.Status == WorkflowInstanceStatus.Rejected)
            await RejectInternalAsync(prequalification, actor, cancellationToken);

        return ToDto(prequalification);
    }

    private async Task ApproveInternalAsync(VendorPrequalification prequalification, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = prequalification.Status;
        prequalification.Approve(actor);

        var validityMonths = int.Parse(_configurationResolver.Resolve(ValidityMonthsConfigurationKey, ConfigurationContext.SystemOnly)!);
        var validFrom = DateOnly.FromDateTime(DateTime.UtcNow);
        prequalification.SetValidityPeriod(validFrom, validityMonths);

        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(prequalification.Id), actor,
            $"Vendor prequalification '{prequalification.DocumentNumber}' approved (valid {prequalification.ValidFrom:yyyy-MM-dd} to {prequalification.ValidUntil:yyyy-MM-dd}).",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(prequalification.Status.ToString()), AuditSource);
    }

    private async Task RejectInternalAsync(VendorPrequalification prequalification, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = prequalification.Status;
        prequalification.Reject(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(prequalification.Id), actor,
            $"Vendor prequalification '{prequalification.DocumentNumber}' rejected.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(prequalification.Status.ToString()), AuditSource);
    }

    public async Task<AttachmentDto> AddAttachmentAsync(
        Guid id, string fileName, string contentType, byte[] content, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, VendorPrequalificationSecurity.MaintainPrivilegeKey);
        var prequalification = await RequirePrequalificationAsync(id, cancellationToken);
        var metadata = await _attachmentService.UploadAsync(
            AuditTargetType, prequalification.Id, fileName, contentType, content, actor, cancellationToken);

        _auditRecorder.RecordFieldUpdate(
            AuditReference(prequalification.Id), actor,
            $"Attachment '{fileName}' added to '{prequalification.DocumentNumber}'.",
            new[] { new FieldValueChange("Attachments", OldValueJson: null, NewValueJson: JsonSerializer.Serialize(fileName)) },
            AuditSource);

        return ToAttachmentDto(metadata);
    }

    public async Task<IReadOnlyList<AttachmentDto>> ListAttachmentsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await RequirePrequalificationAsync(id, cancellationToken);
        var attachments = await _attachmentService.ListAsync(AuditTargetType, id, cancellationToken);
        return attachments.Select(ToAttachmentDto).ToList();
    }

    /// <summary>Null if the attachment doesn't exist, or exists but doesn't belong to this
    /// prequalification — the controller treats both as 404.</summary>
    public async Task<(AttachmentDto Metadata, byte[] Content)?> DownloadAttachmentAsync(
        Guid id, Guid attachmentId, CancellationToken cancellationToken = default)
    {
        var prequalification = await RequirePrequalificationAsync(id, cancellationToken);
        var download = await _attachmentService.DownloadAsync(attachmentId, cancellationToken);
        if (download is null || download.Value.Metadata.BusinessObjectId != prequalification.Id)
            return null;

        return (ToAttachmentDto(download.Value.Metadata), download.Value.Content);
    }

    public async Task DeleteAttachmentAsync(Guid id, Guid attachmentId, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, VendorPrequalificationSecurity.MaintainPrivilegeKey);
        var prequalification = await RequirePrequalificationAsync(id, cancellationToken);
        var metadata = await _attachmentService.DownloadAsync(attachmentId, cancellationToken);
        if (metadata is null || metadata.Value.Metadata.BusinessObjectId != prequalification.Id)
            throw new KeyNotFoundException($"Attachment {attachmentId} was not found on vendor prequalification {id}.");

        await _attachmentService.DeleteAsync(attachmentId, cancellationToken);

        _auditRecorder.RecordFieldUpdate(
            AuditReference(prequalification.Id), actor,
            $"Attachment '{metadata.Value.Metadata.FileName}' removed from '{prequalification.DocumentNumber}'.",
            new[] { new FieldValueChange("Attachments", OldValueJson: JsonSerializer.Serialize(metadata.Value.Metadata.FileName), NewValueJson: null) },
            AuditSource);
    }

    private void RequireAuthorization(string actor, string privilegeKey)
    {
        var result = _authorizationService.Authorize(BuildPrincipal(actor), privilegeKey);
        if (!result.Allowed) throw new UnauthorizedAccessException(result.Reason);
    }

    private SecurityPrincipal BuildPrincipal(string actor) =>
        new(actor, _actorRoleAssignmentStore.ResolveRoleKeys(actor), new Dictionary<string, IReadOnlySet<string>>());

    private static Platform.Core.BusinessObjectReference AuditReference(Guid id) => new(id, AuditTargetType, "Self");

    private async Task<VendorPrequalification> RequirePrequalificationAsync(Guid id, CancellationToken cancellationToken) =>
        await _repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Vendor prequalification {id} was not found.");

    private static AttachmentDto ToAttachmentDto(AttachmentMetadata metadata) => new(
        metadata.Id, metadata.FileName, metadata.ContentType, metadata.SizeBytes, metadata.UploadedBy, metadata.UploadedAt);

    private static VendorPrequalificationDto ToDto(VendorPrequalification p) => new(
        p.Id, p.DocumentNumber, p.Status.ToString(), p.BusinessPartnerId, p.RoleType, p.Trade,
        p.ValidFrom, p.ValidUntil, p.CreatedAt, p.CreatedBy);
}
