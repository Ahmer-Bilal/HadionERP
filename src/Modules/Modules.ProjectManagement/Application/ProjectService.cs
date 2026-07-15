using System.Text.Json;
using Modules.MasterData.Contracts;
using Modules.ProjectManagement.Domain;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;

namespace Modules.ProjectManagement.Application;

public sealed class ProjectService
{
    public const string NumberRangeKey = "PM-PROJECT";

    /// <summary>Only a partner holding the Client role is a meaningful "sold-to party" for a project — same
    /// role-eligibility-check pattern every cross-module vendor/customer reference in this codebase uses.</summary>
    private static readonly HashSet<string> CustomerEligibleRoles = new(StringComparer.OrdinalIgnoreCase) { "Client" };

    private const string AuditTargetType = "Project";
    private const string AuditSource = "Modules.ProjectManagement";

    private readonly IProjectRepository _repository;
    private readonly INumberRangeService _numberRangeService;
    private readonly IAuditRecorder _auditRecorder;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IWorkflowInstanceRepository _workflowInstanceRepository;
    private readonly IAuthorizationService _authorizationService;
    private readonly IActorRoleAssignmentStore _actorRoleAssignmentStore;
    private readonly IBusinessPartnerLookup _businessPartnerLookup;

    public ProjectService(
        IProjectRepository repository,
        INumberRangeService numberRangeService,
        IAuditRecorder auditRecorder,
        IWorkflowEngine workflowEngine,
        IWorkflowInstanceRepository workflowInstanceRepository,
        IAuthorizationService authorizationService,
        IActorRoleAssignmentStore actorRoleAssignmentStore,
        IBusinessPartnerLookup businessPartnerLookup)
    {
        _repository = repository;
        _numberRangeService = numberRangeService;
        _auditRecorder = auditRecorder;
        _workflowEngine = workflowEngine;
        _workflowInstanceRepository = workflowInstanceRepository;
        _authorizationService = authorizationService;
        _actorRoleAssignmentStore = actorRoleAssignmentStore;
        _businessPartnerLookup = businessPartnerLookup;
    }

    public async Task<ProjectDto> CreateAsync(
        CreateProjectRequest request, string actor, string companyId, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, ProjectSecurity.MaintainPrivilegeKey);

        if (request.WbsElements.Count == 0)
            throw new ArgumentException("A project needs at least one WBS element.");

        if (request.CustomerId is { } customerId)
            await ValidateCustomerAsync(customerId, cancellationToken);

        var project = new Project(actor, request.ProjectName, request.ProjectNameArabic, request.CustomerId, request.StartDate, request.EndDate);

        var realIdByTempId = new Dictionary<int, Guid>();
        foreach (var elementRequest in request.WbsElements)
        {
            Guid? parentRealId = null;
            if (elementRequest.ParentTempId is { } parentTempId)
            {
                if (!realIdByTempId.TryGetValue(parentTempId, out var resolvedParentId))
                    throw new ArgumentException($"WBS element (tempId {elementRequest.TempId}) references parent tempId {parentTempId}, which must appear earlier in the list.");
                parentRealId = resolvedParentId;
            }

            var element = project.AddWbsElement(
                elementRequest.Code, elementRequest.Name, elementRequest.NameArabic, parentRealId,
                elementRequest.IsPlanningElement, elementRequest.IsAccountAssignmentElement, elementRequest.IsBillingElement);
            realIdByTempId[elementRequest.TempId] = element.Id;
        }

        var documentNumber = _numberRangeService.GetNext(NumberRangeKey, companyId, DateTimeOffset.UtcNow.Year);
        project.AssignNumber(documentNumber);

        _repository.Add(project);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordCreate(AuditReference(project.Id), actor,
            $"Project '{project.DocumentNumber}' created ({project.WbsElements.Count} WBS elements).", AuditSource);

        return ToDto(project);
    }

    public async Task<ProjectDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var project = await _repository.GetAsync(id, cancellationToken);
        return project is null ? null : ToDto(project);
    }

    public async Task<(IReadOnlyList<ProjectDto> Items, int TotalCount)> ListAsync(
        int skip, int top, CancellationToken cancellationToken = default)
    {
        var items = await _repository.ListAsync(skip, top, cancellationToken);
        var total = await _repository.CountAsync(cancellationToken);
        return (items.Select(ToDto).ToList(), total);
    }

    public async Task<ProjectDto> SubmitAsync(Guid id, string actor, CancellationToken cancellationToken = default)
    {
        RequireAuthorization(actor, ProjectSecurity.MaintainPrivilegeKey);
        var project = await RequireProjectAsync(id, cancellationToken);

        var fromStatus = project.Status;
        project.Submit(actor);
        await _repository.SaveChangesAsync(cancellationToken);

        _auditRecorder.RecordStatusTransition(AuditReference(project.Id), actor,
            $"Project '{project.DocumentNumber}' submitted for release.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(project.Status.ToString()), AuditSource);

        var instance = _workflowEngine.Start(ProjectWorkflow.BusinessObjectType, ProjectWorkflow.SubmitTransition, project.Id);
        if (instance is not null)
        {
            _workflowInstanceRepository.Add(instance);
            await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);
            if (instance.Status == WorkflowInstanceStatus.Approved)
                await ApproveInternalAsync(project, actor, cancellationToken);
        }

        return ToDto(project);
    }

    public Task<ProjectDto> ApproveAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Approve, cancellationToken);

    public Task<ProjectDto> RejectAsync(Guid id, string actor, CancellationToken cancellationToken = default) =>
        DecideApprovalAsync(id, actor, WorkflowDecision.Reject, cancellationToken);

    private async Task<ProjectDto> DecideApprovalAsync(
        Guid id, string actor, WorkflowDecision decision, CancellationToken cancellationToken)
    {
        RequireAuthorization(actor, ProjectSecurity.ApprovePrivilegeKey);
        var project = await RequireProjectAsync(id, cancellationToken);
        var instance = await _workflowInstanceRepository.GetActiveAsync(ProjectWorkflow.BusinessObjectType, project.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Project {id} has no pending approval to decide.");

        _workflowEngine.Decide(instance, BuildPrincipal(actor), decision);
        await _workflowInstanceRepository.SaveChangesAsync(cancellationToken);

        if (instance.Status == WorkflowInstanceStatus.Approved)
            await ApproveInternalAsync(project, actor, cancellationToken);
        else if (instance.Status == WorkflowInstanceStatus.Rejected)
            await RejectInternalAsync(project, actor, cancellationToken);

        return ToDto(project);
    }

    private async Task ApproveInternalAsync(Project project, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = project.Status;
        project.Approve(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(project.Id), actor,
            $"Project '{project.DocumentNumber}' released.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(project.Status.ToString()), AuditSource);
    }

    private async Task RejectInternalAsync(Project project, string actor, CancellationToken cancellationToken)
    {
        var fromStatus = project.Status;
        project.Reject(actor);
        await _repository.SaveChangesAsync(cancellationToken);
        _auditRecorder.RecordStatusTransition(AuditReference(project.Id), actor,
            $"Project '{project.DocumentNumber}' rejected.",
            JsonSerializer.Serialize(fromStatus.ToString()), JsonSerializer.Serialize(project.Status.ToString()), AuditSource);
    }

    private async Task ValidateCustomerAsync(Guid customerId, CancellationToken cancellationToken)
    {
        var customer = await _businessPartnerLookup.GetAsync(customerId, cancellationToken)
            ?? throw new ArgumentException($"Business partner {customerId} was not found.");
        if (customer.Status != "Approved")
            throw new ArgumentException($"Business partner '{customer.Name}' is not Approved and cannot be set as a project's customer.");
        if (!customer.BusinessRoles.Any(CustomerEligibleRoles.Contains))
            throw new ArgumentException($"Business partner '{customer.Name}' does not hold the Client role.");
    }

    private void RequireAuthorization(string actor, string privilegeKey)
    {
        var result = _authorizationService.Authorize(BuildPrincipal(actor), privilegeKey);
        if (!result.Allowed) throw new UnauthorizedAccessException(result.Reason);
    }

    private SecurityPrincipal BuildPrincipal(string actor) =>
        new(actor, _actorRoleAssignmentStore.ResolveRoleKeys(actor), new Dictionary<string, IReadOnlySet<string>>());

    private static BusinessObjectReference AuditReference(Guid id) => new(id, AuditTargetType, "Self");

    private async Task<Project> RequireProjectAsync(Guid id, CancellationToken cancellationToken) =>
        await _repository.GetAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Project {id} was not found.");

    private static ProjectDto ToDto(Project p) => new(
        p.Id, p.DocumentNumber, p.Status.ToString(), p.ProjectName, p.ProjectNameArabic, p.CustomerId, p.StartDate, p.EndDate,
        p.WbsElements.Select(w => new WbsElementDto(
            w.Id, w.Code, w.Name, w.NameArabic, w.ParentWbsElementId, w.IsPlanningElement, w.IsAccountAssignmentElement, w.IsBillingElement)).ToList(),
        p.CreatedAt, p.CreatedBy);
}
