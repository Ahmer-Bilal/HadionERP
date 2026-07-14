using Modules.MasterData.Application;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Workflow;
using Platform.Workflow.Delegation;

namespace Modules.MasterData.Tests;

public class BusinessPartnerServiceTests
{
    private static readonly int CurrentYear = DateTimeOffset.UtcNow.Year;

    private static BusinessPartnerService BuildService(out FakeBusinessPartnerRepository repository) =>
        BuildService(out repository, out _, out _);

    private static BusinessPartnerService BuildService(out FakeBusinessPartnerRepository repository, out IAuditLog auditLog) =>
        BuildService(out repository, out auditLog, out _);

    /// <summary>Wires the REAL Platform.Workflow engine (only its own in-memory reference
    /// implementations, already proven by Platform.Workflow.Tests) with
    /// <see cref="Modules.MasterData.Application.BusinessPartnerWorkflow.SubmitApprovalDefinition"/>
    /// registered — the same definition Gateway.Api registers at startup — so these tests exercise the
    /// real routing/eligibility logic, not a stub that always says "approved."</summary>
    private static BusinessPartnerService BuildService(
        out FakeBusinessPartnerRepository repository, out IAuditLog auditLog, out FakeWorkflowInstanceRepository workflowInstances)
    {
        repository = new FakeBusinessPartnerRepository();
        var numberRanges = new InMemoryNumberRangeService(new[]
        {
            new NumberRangeDefinition(BusinessPartnerService.NumberRangeKey, "MD", "BP")
        });
        auditLog = new InMemoryAuditLog();
        workflowInstances = new FakeWorkflowInstanceRepository();

        var workflowCatalog = new InMemoryWorkflowDefinitionCatalog(new[] { BusinessPartnerWorkflow.SubmitApprovalDefinition });
        var eligibilityService = new RoleBasedWorkflowEligibilityService(new InMemoryDelegationRegistry());
        var workflowEngine = new WorkflowEngine(workflowCatalog, eligibilityService);

        return new BusinessPartnerService(repository, numberRanges, new AuditRecorder(auditLog), workflowEngine, workflowInstances);
    }

    private static CreateBusinessPartnerRequest ValidRequest(string partnerType = "Vendor") =>
        new("Gulf Falcon Trading Co", partnerType, "300000000000003");

    private static IReadOnlyList<AuditEntry> AuditEntriesFor(IAuditLog auditLog, Guid partnerId) =>
        auditLog.GetFor(new BusinessObjectReference(partnerId, "BusinessPartner", "Self"));

    [Fact]
    public async Task Create_assigns_a_document_number_and_starts_in_draft()
    {
        var service = BuildService(out _);

        var created = await service.CreateAsync(ValidRequest(), actor: "ahmer.bilal", companyId: "C001");

        Assert.Equal($"MD-BP-{CurrentYear}-000001", created.DocumentNumber);
        Assert.Equal("Draft", created.Status);
        Assert.Equal("Gulf Falcon Trading Co", created.Name);
    }

    [Fact]
    public async Task Create_rejects_an_invalid_partner_type()
    {
        var service = BuildService(out _);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(ValidRequest(partnerType: "NotARealType"), "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Get_returns_null_for_an_unknown_id()
    {
        var service = BuildService(out _);

        Assert.Null(await service.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task List_reports_the_total_count_alongside_the_page()
    {
        var service = BuildService(out _);
        for (var i = 0; i < 3; i++)
        {
            await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");
        }

        var (items, totalCount) = await service.ListAsync(skip: 0, top: 2);

        Assert.Equal(2, items.Count);
        Assert.Equal(3, totalCount);
    }

    [Fact]
    public async Task Submit_then_approve_moves_the_partner_through_the_lifecycle()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");

        var submitted = await service.SubmitAsync(created.Id, "ahmer.bilal");
        Assert.Equal("Submitted", submitted.Status);

        var approved = await service.ApproveAsync(created.Id, "finance.manager");
        Assert.Equal("Approved", approved.Status);
    }

    [Fact]
    public async Task Acting_on_an_unknown_id_throws_KeyNotFoundException()
    {
        var service = BuildService(out _);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.SubmitAsync(Guid.NewGuid(), "ahmer.bilal"));
    }

    [Fact]
    public async Task AddAddressAsync_appends_an_address_to_the_partner()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");

        var updated = await service.AddAddressAsync(
            created.Id, new AddBusinessPartnerAddressRequest("SiteOffice", "Saudi Arabia", "Riyadh", "King Fahd Road"), "ahmer.bilal");

        var address = Assert.Single(updated.Addresses);
        Assert.Equal("SiteOffice", address.AddressType);
        Assert.Equal("Riyadh", address.City);
    }

    [Fact]
    public async Task AddAddressAsync_rejects_an_invalid_address_type()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddAddressAsync(created.Id, new AddBusinessPartnerAddressRequest("NotARealType", null, null, null), "ahmer.bilal"));
    }

    [Fact]
    public async Task AddContactAsync_appends_a_contact_to_the_partner()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");

        var updated = await service.AddContactAsync(
            created.Id, new AddBusinessPartnerContactRequest("Fahad Al-Otaibi", "Procurement Manager", "fahad@vendor.example", "+966500000000"), "ahmer.bilal");

        var contact = Assert.Single(updated.Contacts);
        Assert.Equal("Fahad Al-Otaibi", contact.Name);
        Assert.Equal("Procurement Manager", contact.JobTitle);
    }

    [Fact]
    public async Task CreateAsync_records_an_audit_create_entry()
    {
        var service = BuildService(out _, out var auditLog);

        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");

        var entry = Assert.Single(AuditEntriesFor(auditLog, created.Id));
        Assert.Equal(AuditAction.Create, entry.Action);
        Assert.Equal("ahmer.bilal", entry.ActorPrincipalKey);
        Assert.Equal("BusinessPartner", entry.BusinessObject.TargetType);
    }

    [Fact]
    public async Task AddAddressAsync_records_an_audit_field_update_entry()
    {
        var service = BuildService(out _, out var auditLog);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");

        await service.AddAddressAsync(
            created.Id, new AddBusinessPartnerAddressRequest("SiteOffice", "Saudi Arabia", "Riyadh", "King Fahd Road"), "ahmer.bilal");

        var entries = AuditEntriesFor(auditLog, created.Id);
        var addressEntry = Assert.Single(entries, e => e.Action == AuditAction.Update);
        var change = Assert.Single(addressEntry.FieldValueChanges);
        Assert.Equal("Addresses", change.FieldName);
        Assert.Null(change.OldValueJson);
        Assert.Contains("Riyadh", change.NewValueJson);
    }

    [Fact]
    public async Task AddContactAsync_records_an_audit_field_update_entry()
    {
        var service = BuildService(out _, out var auditLog);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");

        await service.AddContactAsync(
            created.Id, new AddBusinessPartnerContactRequest("Fahad Al-Otaibi", "Procurement Manager", "fahad@vendor.example", "+966500000000"), "ahmer.bilal");

        var entries = AuditEntriesFor(auditLog, created.Id);
        var contactEntry = Assert.Single(entries, e => e.Action == AuditAction.Update);
        var change = Assert.Single(contactEntry.FieldValueChanges);
        Assert.Equal("Contacts", change.FieldName);
        Assert.Contains("Fahad Al-Otaibi", change.NewValueJson);
    }

    [Fact]
    public async Task Submit_then_approve_records_two_audit_status_transition_entries()
    {
        var service = BuildService(out _, out var auditLog);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");

        await service.SubmitAsync(created.Id, "ahmer.bilal");
        await service.ApproveAsync(created.Id, "finance.manager");

        var transitions = AuditEntriesFor(auditLog, created.Id).Where(e => e.Action == AuditAction.StatusTransition).ToList();
        Assert.Equal(2, transitions.Count);
        Assert.Equal("\"Draft\"", transitions[0].FieldValueChanges.Single().OldValueJson);
        Assert.Equal("\"Submitted\"", transitions[0].FieldValueChanges.Single().NewValueJson);
        Assert.Equal("finance.manager", transitions[1].ActorPrincipalKey);
        Assert.Equal("\"Approved\"", transitions[1].FieldValueChanges.Single().NewValueJson);
    }

    [Fact]
    public async Task SubmitAsync_starts_a_running_workflow_instance_and_leaves_the_partner_Submitted()
    {
        var service = BuildService(out _, out _, out var workflowInstances);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");

        var submitted = await service.SubmitAsync(created.Id, "ahmer.bilal");

        Assert.Equal("Submitted", submitted.Status);
        var instance = await workflowInstances.GetActiveAsync("BusinessPartner", created.Id);
        Assert.NotNull(instance);
        Assert.Equal(WorkflowInstanceStatus.Running, instance!.Status);
        Assert.Equal(BusinessPartnerWorkflow.SubmitApprovalDefinition.DefinitionKey, instance.DefinitionKey);
    }

    [Fact]
    public async Task ApproveAsync_decides_the_pending_workflow_instance_and_completes_it()
    {
        var service = BuildService(out _, out _, out var workflowInstances);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");
        await service.SubmitAsync(created.Id, "ahmer.bilal");

        var approved = await service.ApproveAsync(created.Id, "finance.manager");

        Assert.Equal("Approved", approved.Status);
        Assert.Null(await workflowInstances.GetActiveAsync("BusinessPartner", created.Id));
    }

    [Fact]
    public async Task RejectAsync_decides_the_pending_workflow_instance_and_rejects_the_partner()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");
        await service.SubmitAsync(created.Id, "ahmer.bilal");

        var rejected = await service.RejectAsync(created.Id, "compliance.officer");

        Assert.Equal("Rejected", rejected.Status);
    }

    [Fact]
    public async Task ApproveAsync_throws_when_there_is_no_pending_approval_to_decide()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");

        // Never submitted — nothing started a workflow instance for this partner.
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveAsync(created.Id, "finance.manager"));
    }

    [Fact]
    public async Task ApproveAsync_throws_when_the_pending_approval_was_already_decided()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");
        await service.SubmitAsync(created.Id, "ahmer.bilal");
        await service.ApproveAsync(created.Id, "finance.manager");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ApproveAsync(created.Id, "finance.manager"));
    }

    [Fact]
    public async Task Number_range_counters_are_sequential_per_company()
    {
        var service = BuildService(out _);

        var first = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");
        var second = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");

        Assert.Equal($"MD-BP-{CurrentYear}-000001", first.DocumentNumber);
        Assert.Equal($"MD-BP-{CurrentYear}-000002", second.DocumentNumber);
    }
}
