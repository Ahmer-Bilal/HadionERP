using Modules.MasterData.Contracts;
using Modules.Procurement.Application;
using Platform.Attachments;
using Platform.Audit;
using Platform.Configuration;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;
using Platform.Workflow.Delegation;

namespace Modules.Procurement.Tests;

public class VendorPrequalificationServiceTests
{
    private static readonly int CurrentYear = DateTimeOffset.UtcNow.Year;
    private static readonly Guid ApprovedVendorId = Guid.NewGuid();
    private static readonly Guid DraftVendorId = Guid.NewGuid();
    private static readonly Guid UnknownVendorId = Guid.NewGuid();

    private static IActorRoleAssignmentStore BuildActorRoles() => new InMemoryActorRoleAssignmentStore(
        new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["ahmer.bilal"] = new[] { VendorPrequalificationSecurity.MaintainerRoleKey },
            ["procurement.reviewer"] = new[]
            {
                VendorPrequalificationWorkflow.CommercialReviewerRoleKey,
                VendorPrequalificationWorkflow.LegalReviewerRoleKey,
                VendorPrequalificationWorkflow.TechnicalReviewerRoleKey,
                VendorPrequalificationWorkflow.HseReviewerRoleKey,
                VendorPrequalificationWorkflow.QualityReviewerRoleKey,
            },
            ["procurement.commercial.reviewer"] = new[] { VendorPrequalificationWorkflow.CommercialReviewerRoleKey },
        });

    private static FakeBusinessPartnerLookup BuildBusinessPartnerLookup()
    {
        var lookup = new FakeBusinessPartnerLookup();
        lookup.Add(new BusinessPartnerSummary(ApprovedVendorId, "Gulf Falcon Trading Co", null, new[] { "Supplier" }, "Approved"));
        lookup.Add(new BusinessPartnerSummary(DraftVendorId, "New Vendor Co", null, new[] { "Supplier" }, "Draft"));
        return lookup;
    }

    private static IConfigurationResolver BuildConfigurationResolver(string validityMonths = "24")
    {
        var catalog = new InMemoryConfigurationCatalog();
        catalog.Register(new ConfigurationKeyDefinition(
            VendorPrequalificationService.ValidityMonthsConfigurationKey, "Validity months",
            new[] { ConfigurationLevel.System }, DefaultValue: validityMonths));
        return new ConfigurationResolver(catalog, new InMemoryConfigurationStore());
    }

    private static VendorPrequalificationService BuildService(out FakeVendorPrequalificationRepository repository) =>
        BuildService(out repository, out _);

    private static VendorPrequalificationService BuildService(
        out FakeVendorPrequalificationRepository repository, out IAuditLog auditLog)
    {
        repository = new FakeVendorPrequalificationRepository();
        var numberRanges = new InMemoryNumberRangeService(new[]
        {
            new NumberRangeDefinition(VendorPrequalificationService.NumberRangeKey, "PROC", "VPQ")
        });
        auditLog = new InMemoryAuditLog();
        var workflowInstances = new FakeWorkflowInstanceRepository();

        var workflowCatalog = new InMemoryWorkflowDefinitionCatalog(new[] { VendorPrequalificationWorkflow.SubmitApprovalDefinition });
        var workflowEngine = new WorkflowEngine(workflowCatalog, new RoleBasedWorkflowEligibilityService(new InMemoryDelegationRegistry()));

        var securityCatalog = new InMemorySecurityCatalog(
            new[]
            {
                VendorPrequalificationSecurity.MaintainerRole,
                VendorPrequalificationSecurity.CommercialReviewerRole, VendorPrequalificationSecurity.LegalReviewerRole,
                VendorPrequalificationSecurity.TechnicalReviewerRole, VendorPrequalificationSecurity.HseReviewerRole,
                VendorPrequalificationSecurity.QualityReviewerRole,
            },
            new[]
            {
                VendorPrequalificationSecurity.MaintainerDuty,
                VendorPrequalificationSecurity.CommercialReviewerDuty, VendorPrequalificationSecurity.LegalReviewerDuty,
                VendorPrequalificationSecurity.TechnicalReviewerDuty, VendorPrequalificationSecurity.HseReviewerDuty,
                VendorPrequalificationSecurity.QualityReviewerDuty,
            });

        return new VendorPrequalificationService(
            repository, numberRanges, new AuditRecorder(auditLog), workflowEngine, workflowInstances,
            new AuthorizationService(securityCatalog), BuildActorRoles(),
            BuildBusinessPartnerLookup(), BuildConfigurationResolver(),
            new AttachmentService(new FakeAttachmentRepository()));
    }

    private static CreateVendorPrequalificationRequest ValidRequest(string roleType = "Supplier", string? trade = null) =>
        new(ApprovedVendorId, roleType, trade);

    [Fact]
    public async Task Create_assigns_a_document_number_and_starts_in_draft()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");

        Assert.Equal($"PROC-VPQ-{CurrentYear}-000001", created.DocumentNumber);
        Assert.Equal("Draft", created.Status);
        Assert.Equal(ApprovedVendorId, created.BusinessPartnerId);
        Assert.Equal("Supplier", created.RoleType);
    }

    [Fact]
    public async Task Create_rejects_government_authority_outright()
    {
        var service = BuildService(out _);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(ValidRequest("GovernmentAuthority"), "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_an_unknown_vendor()
    {
        var service = BuildService(out _);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(new CreateVendorPrequalificationRequest(UnknownVendorId, "Supplier"), "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_vendor_that_is_not_yet_Approved()
    {
        var service = BuildService(out _);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(new CreateVendorPrequalificationRequest(DraftVendorId, "Supplier"), "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_role_the_vendor_does_not_hold()
    {
        var service = BuildService(out _);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(ValidRequest("Subcontractor"), "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task CreateAsync_throws_for_an_actor_with_no_Maintainer_role()
    {
        var service = BuildService(out _);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CreateAsync(ValidRequest(), "procurement.reviewer", "C001"));
    }

    [Fact]
    public async Task Create_records_an_audit_entry()
    {
        var service = BuildService(out _, out var auditLog);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");

        var entries = auditLog.GetFor(new BusinessObjectReference(created.Id, "VendorPrequalification", "Self"));
        var createEntry = Assert.Single(entries);
        Assert.Equal(AuditAction.Create, createEntry.Action);
    }

    [Fact]
    public async Task Submit_starts_the_review_workflow_and_stays_submitted_until_all_five_steps_decide()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");
        var submitted = await service.SubmitAsync(created.Id, "ahmer.bilal");

        Assert.Equal("Submitted", submitted.Status);

        var afterOneStep = await service.ApproveAsync(created.Id, "procurement.reviewer");
        Assert.Equal("Submitted", afterOneStep.Status);
    }

    [Fact]
    public async Task All_five_review_steps_approving_reaches_approved_and_sets_a_validity_period()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");
        await service.SubmitAsync(created.Id, "ahmer.bilal");

        VendorPrequalificationDto? last = null;
        for (var i = 0; i < 5; i++)
        {
            last = await service.ApproveAsync(created.Id, "procurement.reviewer");
        }

        Assert.NotNull(last);
        Assert.Equal("Approved", last!.Status);
        Assert.NotNull(last.ValidFrom);
        Assert.Equal(last.ValidFrom!.Value.AddMonths(24), last.ValidUntil);
    }

    [Fact]
    public async Task Rejecting_at_any_step_rejects_the_prequalification_without_a_validity_period()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");
        await service.SubmitAsync(created.Id, "ahmer.bilal");
        await service.ApproveAsync(created.Id, "procurement.reviewer"); // Commercial step approved

        var rejected = await service.RejectAsync(created.Id, "procurement.reviewer"); // Legal step rejects
        Assert.Equal("Rejected", rejected.Status);
        Assert.Null(rejected.ValidFrom);
        Assert.Null(rejected.ValidUntil);
    }

    [Fact]
    public async Task A_reviewer_who_only_holds_one_step_role_cannot_decide_a_different_step()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");
        await service.SubmitAsync(created.Id, "ahmer.bilal");

        // "procurement.commercial.reviewer" only holds the Commercial role — fine for step 1...
        await service.ApproveAsync(created.Id, "procurement.commercial.reviewer");

        // ...but not for step 2 (Legal), which WorkflowEngine.Decide enforces via eligibility.
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ApproveAsync(created.Id, "procurement.commercial.reviewer"));
    }

    [Fact]
    public async Task ApproveAsync_throws_for_an_actor_with_no_review_privilege_at_all()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");
        await service.SubmitAsync(created.Id, "ahmer.bilal");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ApproveAsync(created.Id, "ahmer.bilal"));
    }

    [Fact]
    public async Task Attachment_round_trip_add_list_download_delete()
    {
        var service = BuildService(out _);
        var created = await service.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");

        var added = await service.AddAttachmentAsync(
            created.Id, "cert.pdf", "application/pdf", new byte[] { 1, 2, 3 }, "ahmer.bilal");
        Assert.Equal("cert.pdf", added.FileName);

        var list = await service.ListAttachmentsAsync(created.Id);
        Assert.Single(list);

        var downloaded = await service.DownloadAttachmentAsync(created.Id, added.Id);
        Assert.NotNull(downloaded);
        Assert.Equal(new byte[] { 1, 2, 3 }, downloaded!.Value.Content);

        await service.DeleteAttachmentAsync(created.Id, added.Id, "ahmer.bilal");
        Assert.Empty(await service.ListAttachmentsAsync(created.Id));
    }

    [Fact]
    public async Task GetAsync_returns_null_for_unknown_id()
    {
        var service = BuildService(out _);
        Assert.Null(await service.GetAsync(Guid.NewGuid()));
    }
}
