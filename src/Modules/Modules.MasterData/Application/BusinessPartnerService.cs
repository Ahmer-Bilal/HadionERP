using System.Text.Json;
using Modules.MasterData.Domain;
using Platform.Attachments;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Notes;
using Platform.Security;
using Platform.Workflow;

namespace Modules.MasterData.Application;

/// <summary>
/// Orchestrates Business Partner use cases — validates input, drives the Domain object, and persists
/// through the repository port. No business rules live here (those belong on
/// <see cref="BusinessPartner"/> itself, per docs/architecture/01-architecture-foundation.md #1); this
/// layer only coordinates. Audit, Workflow, and Security are likewise platform services, not module logic
/// (CLAUDE.md / docs/architecture/03-platform-services.md #2, #4-5) — this layer only calls
/// <see cref="Platform.Audit.IAuditRecorder"/>, <see cref="Platform.Workflow.IWorkflowEngine"/>, and
/// <see cref="Platform.Security.IAuthorizationService"/> at the points a real business process cares
/// about; the actual capture/hash-chaining/approval-routing/permission logic lives entirely in those
/// platform projects.
/// </summary>
public sealed class BusinessPartnerService
{
    /// <summary>The number range key this module registers for Business Partners — matches the naming
    /// convention "{ModuleAbbrev}-{DocAbbrev}" in docs/architecture/05-engineering-standards.md #2.</summary>
    public const string NumberRangeKey = "MD-BP";

    private const string AuditTargetType = "BusinessPartner";
    private const string AuditSource = "Modules.MasterData";

    private readonly IBusinessPartnerRepository _repository;
    private readonly INumberRangeService _numberRangeService;
    private readonly IAuditRecorder _auditRecorder;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IWorkflowInstanceRepository _workflowInstanceRepository;
    private readonly IAuthorizationService _authorizationService;
    private readonly IActorRoleAssignmentStore _actorRoleAssignmentStore;
    private readonly IAttachmentService _attachmentService;
    private readonly INoteService _noteService;
    private readonly ILookupRepository _lookupRepository;

    public BusinessPartnerService(
        IBusinessPartnerRepository repository,
        INumberRangeService numberRangeService,
        IAuditRecorder auditRecorder,
        IWorkflowEngine workflowEngine,
        IWorkflowInstanceRepository workflowInstanceRepository,
        IAuthorizationService authorizationService,
        IActorRoleAssignmentStore actorRoleAssignmentStore,
        IAttachmentService attachmentService,
        INoteService noteService,
        ILookupRepository lookupRepository)
    {
        _repository = repository;
        _numberRangeService = numberRangeService;
        _auditRecorder = auditRecorder;
        _workflowEngine = workflowEngine;
        _workflowInstanceRepository = workflowInstanceRepository;
        _authorizationService = authorizationService;
        _actorRoleAssignmentStore = actorRoleAssignmentStore;
        _attachmentService = attachmentService;
        _noteService = noteService;
        _lookupRepository = lookupRepository;
    }

    public async Task<BusinessPartnerDto> CreateAsync(
        CreateBusinessPartnerRequest request, string actor, string companyId, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, BusinessPartnerSecurity.MaintainPrivilegeKey);

        await ValidateRoleTypeAsync(request.InitialRole, cancellationToken);

        var partner = new BusinessPartner(actor, request.Name, request.InitialRole, request.InitialTrade);
        partner.UpdateTaxRegistrationNumber(request.TaxRegistrationNumber);
        partner.UpdateNameArabic(request.NameArabic);

        var documentNumber = _numberRangeService.GetNext(NumberRangeKey, companyId, DateTimeOffset.UtcNow.Year);
        partner.AssignNumber(documentNumber);

        _repository.Add(partner);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordCreate(
            AuditReference(partner.Id),
            actor,
            $"Business partner '{partner.Name}' ({partner.DocumentNumber}) created.",
            AuditSource);

        return ToDto(partner);
    }

    public async Task<BusinessPartnerDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var partner = await _repository.GetAsync(id, cancellationToken);
        return partner is null ? null : ToDto(partner);
    }

    public async Task<(IReadOnlyList<BusinessPartnerDto> Items, int TotalCount)> ListAsync(
        int skip, int top, CancellationToken cancellationToken = default)
    {
        var items = await _repository.ListAsync(skip, top, cancellationToken);
        var total = await _repository.CountAsync(cancellationToken);
        return (items.Select(ToDto).ToList(), total);
    }

    public async Task<BusinessPartnerDto> AddAddressAsync(
        Guid id, AddBusinessPartnerAddressRequest request, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, BusinessPartnerSecurity.MaintainPrivilegeKey);

        await ValidateAddressTypeAsync(request.AddressType, cancellationToken);
        await ValidateCountryAsync(request.Country, cancellationToken);

        var partner = await RequirePartnerAsync(id, cancellationToken);
        var address = partner.AddAddress(request.AddressType, request.Country, request.City, request.AddressLine);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordFieldUpdate(
            AuditReference(partner.Id),
            actor,
            $"Address added to '{partner.Name}'.",
            new[]
            {
                new FieldValueChange(
                    "Addresses",
                    OldValueJson: null,
                    NewValueJson: JsonSerializer.Serialize(new BusinessPartnerAddressDto(
                        address.Id, address.AddressType, address.Country, address.City, address.AddressLine)))
            },
            AuditSource);

        return ToDto(partner);
    }

    public async Task<BusinessPartnerDto> AddContactAsync(
        Guid id, AddBusinessPartnerContactRequest request, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, BusinessPartnerSecurity.MaintainPrivilegeKey);

        var partner = await RequirePartnerAsync(id, cancellationToken);
        var contact = partner.AddContact(request.Name, request.JobTitle, request.Email, request.Phone);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordFieldUpdate(
            AuditReference(partner.Id),
            actor,
            $"Contact added to '{partner.Name}'.",
            new[]
            {
                new FieldValueChange(
                    "Contacts",
                    OldValueJson: null,
                    NewValueJson: JsonSerializer.Serialize(new BusinessPartnerContactDto(
                        contact.Id, contact.Name, contact.JobTitle, contact.Email, contact.Phone)))
            },
            AuditSource);

        return ToDto(partner);
    }

    public async Task<BusinessPartnerDto> AddBusinessRoleAsync(
        Guid id, AddBusinessRoleRequest request, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, BusinessPartnerSecurity.MaintainPrivilegeKey);

        await ValidateRoleTypeAsync(request.RoleType, cancellationToken);

        var partner = await RequirePartnerAsync(id, cancellationToken);
        var role = partner.AddBusinessRole(request.RoleType, request.Trade);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordFieldUpdate(
            AuditReference(partner.Id),
            actor,
            $"Business role '{role.RoleType}' added to '{partner.Name}'.",
            new[] { new FieldValueChange("BusinessRoles", OldValueJson: null, NewValueJson: JsonSerializer.Serialize(new BusinessRoleDto(role.Id, role.RoleType, role.Trade))) },
            AuditSource);

        return ToDto(partner);
    }

    public async Task<BusinessPartnerDto> RemoveBusinessRoleAsync(
        Guid id, Guid roleId, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, BusinessPartnerSecurity.MaintainPrivilegeKey);

        var partner = await RequirePartnerAsync(id, cancellationToken);
        partner.RemoveBusinessRole(roleId);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordFieldUpdate(
            AuditReference(partner.Id), actor, $"Business role removed from '{partner.Name}'.",
            new[] { new FieldValueChange("BusinessRoles", OldValueJson: JsonSerializer.Serialize(roleId), NewValueJson: null) },
            AuditSource);

        return ToDto(partner);
    }

    public async Task<AttachmentDto> AddAttachmentAsync(
        Guid id, string fileName, string contentType, byte[] content, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, BusinessPartnerSecurity.MaintainPrivilegeKey);

        var partner = await RequirePartnerAsync(id, cancellationToken);
        var metadata = await _attachmentService.UploadAsync(
            AuditTargetType, partner.Id, fileName, contentType, content, actor, cancellationToken);

        _auditRecorder.RecordFieldUpdate(
            AuditReference(partner.Id),
            actor,
            $"Attachment '{fileName}' added to '{partner.Name}'.",
            new[] { new FieldValueChange("Attachments", OldValueJson: null, NewValueJson: JsonSerializer.Serialize(fileName)) },
            AuditSource);

        return ToAttachmentDto(metadata);
    }

    public async Task<IReadOnlyList<AttachmentDto>> ListAttachmentsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await RequirePartnerAsync(id, cancellationToken);
        var attachments = await _attachmentService.ListAsync(AuditTargetType, id, cancellationToken);
        return attachments.Select(ToAttachmentDto).ToList();
    }

    /// <summary>Null if the attachment doesn't exist, or exists but doesn't belong to this partner — the
    /// controller treats both as 404, never leaking whether an id belongs to some other Business Partner.</summary>
    public async Task<(AttachmentDto Metadata, byte[] Content)?> DownloadAttachmentAsync(
        Guid id, Guid attachmentId, CancellationToken cancellationToken = default)
    {
        var partner = await RequirePartnerAsync(id, cancellationToken);
        var download = await _attachmentService.DownloadAsync(attachmentId, cancellationToken);
        if (download is null || download.Value.Metadata.BusinessObjectId != partner.Id)
        {
            return null;
        }

        return (ToAttachmentDto(download.Value.Metadata), download.Value.Content);
    }

    public async Task DeleteAttachmentAsync(Guid id, Guid attachmentId, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, BusinessPartnerSecurity.MaintainPrivilegeKey);

        var partner = await RequirePartnerAsync(id, cancellationToken);
        var metadata = await _attachmentService.DownloadAsync(attachmentId, cancellationToken);
        if (metadata is null || metadata.Value.Metadata.BusinessObjectId != partner.Id)
        {
            throw new KeyNotFoundException($"Attachment {attachmentId} was not found on business partner {id}.");
        }

        await _attachmentService.DeleteAsync(attachmentId, cancellationToken);

        _auditRecorder.RecordFieldUpdate(
            AuditReference(partner.Id),
            actor,
            $"Attachment '{metadata.Value.Metadata.FileName}' removed from '{partner.Name}'.",
            new[] { new FieldValueChange("Attachments", OldValueJson: JsonSerializer.Serialize(metadata.Value.Metadata.FileName), NewValueJson: null) },
            AuditSource);
    }

    public async Task<NoteDto> AddNoteAsync(
        Guid id, string text, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, BusinessPartnerSecurity.MaintainPrivilegeKey);

        var partner = await RequirePartnerAsync(id, cancellationToken);
        var note = await _noteService.AddAsync(AuditTargetType, partner.Id, text, actor, cancellationToken);

        _auditRecorder.RecordFieldUpdate(
            AuditReference(partner.Id),
            actor,
            $"Note added to '{partner.Name}'.",
            new[] { new FieldValueChange("Notes", OldValueJson: null, NewValueJson: JsonSerializer.Serialize(text)) },
            AuditSource);

        return ToNoteDto(note);
    }

    public async Task<IReadOnlyList<NoteDto>> ListNotesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await RequirePartnerAsync(id, cancellationToken);
        var notes = await _noteService.ListAsync(AuditTargetType, id, cancellationToken);
        return notes.Select(ToNoteDto).ToList();
    }

    public async Task DeleteNoteAsync(Guid id, Guid noteId, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, BusinessPartnerSecurity.MaintainPrivilegeKey);

        var partner = await RequirePartnerAsync(id, cancellationToken);
        var notes = await _noteService.ListAsync(AuditTargetType, partner.Id, cancellationToken);
        var note = notes.FirstOrDefault(n => n.Id == noteId)
            ?? throw new KeyNotFoundException($"Note {noteId} was not found on business partner {id}.");

        await _noteService.DeleteAsync(noteId, cancellationToken);

        _auditRecorder.RecordFieldUpdate(
            AuditReference(partner.Id),
            actor,
            $"Note removed from '{partner.Name}'.",
            new[] { new FieldValueChange("Notes", OldValueJson: JsonSerializer.Serialize(note.Text), NewValueJson: null) },
            AuditSource);
    }

    /// <summary>
    /// Moves the partner to Submitted, then starts (or resolves) its onboarding approval workflow —
    /// replacing what used to be a direct, unconditional path to Approved. Three outcomes, matching
    /// <see cref="IWorkflowEngine.Start"/>'s documented contract:
    /// (1) no workflow configured at all (<c>Start</c> returns null) — proceed as already approved;
    /// (2) a workflow is configured but no step applies to this resource context (zero
    ///     <see cref="WorkflowInstance.ApplicableSteps"/>) — the instance auto-completes as Approved, so
    ///     approve the partner immediately, same net effect as (1);
    /// (3) a real step applies — persist the Running instance and leave the partner Submitted until a
    ///     real decision resolves it via <see cref="ApproveAsync"/>/<see cref="RejectAsync"/>.
    /// </summary>
    public async Task<BusinessPartnerDto> SubmitAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, BusinessPartnerSecurity.MaintainPrivilegeKey);

        var partner = await RequirePartnerAsync(id, cancellationToken);
        if (partner.BusinessRoles.Count == 0)
            throw new ArgumentException($"Business partner '{partner.Name}' has no business role and cannot be submitted.");

        var fromStatus = partner.Status;
        partner.Submit(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(
            AuditReference(partner.Id),
            actor,
            $"Business partner '{partner.Name}' submitted for approval.",
            JsonSerializer.Serialize(fromStatus.ToString()),
            JsonSerializer.Serialize(partner.Status.ToString()),
            AuditSource);

        var instance = _workflowEngine.Start(BusinessPartnerWorkflow.BusinessObjectType, BusinessPartnerWorkflow.SubmitTransition, partner.Id);
        if (instance is null)
        {
            // No workflow configured at all for this BO type + transition — IWorkflowEngine's contract
            // says the caller proceeds as if already approved.
            await ApproveInternalAsync(partner, actor, cancellationToken);
            return ToDto(partner);
        }

        _workflowInstanceRepository.Add(instance);
        await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);

        if (instance.Status == WorkflowInstanceStatus.Approved)
        {
            // Zero applicable steps for this resource context (e.g. a future condition-gated step that
            // didn't match) — the instance completed immediately with nothing to decide.
            await ApproveInternalAsync(partner, actor, cancellationToken);
        }

        return ToDto(partner);
    }

    public Task<BusinessPartnerDto> ApproveAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Approve, cancellationToken);

    public Task<BusinessPartnerDto> RejectAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Reject, cancellationToken);

    /// <summary>
    /// Records one approver's decision against the partner's pending workflow instance, then applies the
    /// resulting Business Object transition only if the workflow itself reached a final state — a
    /// multi-step matrix (not configured today, but supported by the engine) would leave the partner
    /// Submitted after this call, still waiting on the next step's approver.
    /// </summary>
    private async Task<BusinessPartnerDto> DecideApprovalAsync(
        Guid id, string actor, WorkflowDecision decision, CancellationToken cancellationToken)
    {
        RequireAuthorization(actor, BusinessPartnerSecurity.ApprovePrivilegeKey);

        var partner = await RequirePartnerAsync(id, cancellationToken);
        var instance = await _workflowInstanceRepository.GetActiveAsync(BusinessPartnerWorkflow.BusinessObjectType, partner.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Business partner {id} has no pending approval to decide.");

        _workflowEngine.Decide(instance, BuildPrincipal(actor), decision);
        await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);

        switch (instance.Status)
        {
            case WorkflowInstanceStatus.Approved:
                await ApproveInternalAsync(partner, actor, cancellationToken);
                break;
            case WorkflowInstanceStatus.Rejected:
                await RejectInternalAsync(partner, actor, cancellationToken);
                break;
            // Running (a later step still pending) and Cancelled are left as-is — the partner stays
            // Submitted; there is no Cancel path wired for Business Partner yet.
        }

        return ToDto(partner);
    }

    private async Task ApproveInternalAsync(BusinessPartner partner, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = partner.Status;
        partner.Approve(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(
            AuditReference(partner.Id),
            actor,
            $"Business partner '{partner.Name}' approved.",
            JsonSerializer.Serialize(fromStatus.ToString()),
            JsonSerializer.Serialize(partner.Status.ToString()),
            AuditSource);
    }

    private async Task RejectInternalAsync(BusinessPartner partner, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = partner.Status;
        partner.Reject(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(
            AuditReference(partner.Id),
            actor,
            $"Business partner '{partner.Name}' rejected.",
            JsonSerializer.Serialize(fromStatus.ToString()),
            JsonSerializer.Serialize(partner.Status.ToString()),
            AuditSource);
    }

    /// <summary>
    /// The real authorization gate — calls <see cref="Platform.Security.IAuthorizationService"/>, the
    /// same platform service every module is meant to call rather than reimplementing permission checks
    /// (docs/architecture/03-platform-services.md #2.2). Throws <see cref="UnauthorizedAccessException"/>
    /// on denial, the same exception type <see cref="IWorkflowEngine.Decide"/> already throws for
    /// workflow-eligibility denials — <see cref="Api.BusinessPartnersController"/> maps both to a 403.
    /// </summary>
    private void RequireAuthorization(string actor, string privilegeKey)
    {
        var result = _authorizationService.Authorize(BuildPrincipal(actor), privilegeKey);
        if (!result.Allowed)
        {
            throw new UnauthorizedAccessException(result.Reason);
        }
    }

    /// <summary>
    /// Builds the principal both <see cref="RequireAuthorization"/> and <see cref="IWorkflowEngine.Decide"/>
    /// check against, resolving real Role keys from <see cref="IActorRoleAssignmentStore"/> — replacing
    /// what used to be an unconditional grant (see git history for the prior shim). Still a placeholder
    /// for real authentication (there is no logged-in user yet, only the bare actor-id strings every
    /// endpoint currently hardcodes — see `Modules.MasterData/README.md`'s deferred list), but it is no
    /// longer a shim that grants every actor every role: an actor with no assignment resolves to zero
    /// Roles and is correctly denied.
    /// </summary>
    private SecurityPrincipal BuildPrincipal(string actor) =>
        new(actor, _actorRoleAssignmentStore.ResolveRoleKeys(actor), new Dictionary<string, IReadOnlySet<string>>());

    /// <summary>Validates a role code against the admin-configurable <c>"BusinessRoleType"</c> lookup
    /// type instead of <c>Enum.TryParse</c> — CLAUDE.md's "don't hard-code lookup data" instruction; an
    /// administrator can add a new role type via the Lookup Data admin panel without a code change, and it
    /// becomes selectable here immediately.</summary>
    private async Task ValidateRoleTypeAsync(string roleType, CancellationToken cancellationToken)
    {
        var value = await _lookupRepository.GetValueByCodeAsync("BusinessRoleType", roleType, cancellationToken);
        if (value is null || !value.IsActive)
            throw new ArgumentException($"'{roleType}' is not a known, active Business Role Type.");
    }

    private async Task ValidateAddressTypeAsync(string addressType, CancellationToken cancellationToken)
    {
        var value = await _lookupRepository.GetValueByCodeAsync("AddressType", addressType, cancellationToken);
        if (value is null || !value.IsActive)
            throw new ArgumentException($"'{addressType}' is not a known, active Address Type.");
    }

    /// <summary>Country is optional on an address, so a null/blank value is always allowed — only a
    /// non-blank value is checked against the admin-configurable <c>"Country"</c> lookup type.</summary>
    private async Task ValidateCountryAsync(string? country, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(country)) return;
        var value = await _lookupRepository.GetValueByCodeAsync("Country", country, cancellationToken);
        if (value is null || !value.IsActive)
            throw new ArgumentException($"'{country}' is not a known, active Country.");
    }

    private static BusinessObjectReference AuditReference(Guid partnerId) => new(partnerId, AuditTargetType, "Self");

    private async Task<BusinessPartner> RequirePartnerAsync(Guid id, CancellationToken cancellationToken) =>
        await _repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Business partner {id} was not found.");

    private static AttachmentDto ToAttachmentDto(AttachmentMetadata metadata) => new(
        metadata.Id, metadata.FileName, metadata.ContentType, metadata.SizeBytes, metadata.UploadedBy, metadata.UploadedAt);

    private static NoteDto ToNoteDto(Note note) => new(note.Id, note.Text, note.CreatedBy, note.CreatedAt);

    private static BusinessPartnerDto ToDto(BusinessPartner partner) => new(
        partner.Id,
        partner.DocumentNumber,
        partner.Status.ToString(),
        partner.Name,
        partner.NameArabic,
        partner.TaxRegistrationNumber,
        partner.Addresses.Select(a => new BusinessPartnerAddressDto(a.Id, a.AddressType, a.Country, a.City, a.AddressLine)).ToList(),
        partner.Contacts.Select(c => new BusinessPartnerContactDto(c.Id, c.Name, c.JobTitle, c.Email, c.Phone)).ToList(),
        partner.BusinessRoles.Select(r => new BusinessRoleDto(r.Id, r.RoleType, r.Trade)).ToList(),
        partner.CreatedAt,
        partner.CreatedBy);
}
