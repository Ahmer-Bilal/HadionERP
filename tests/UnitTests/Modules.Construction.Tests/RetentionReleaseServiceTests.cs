using Modules.Construction.Application;
using Modules.Construction.Domain;
using Modules.ProjectManagement.Contracts;
using Platform.Audit;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;
using Platform.Workflow.Delegation;

namespace Modules.Construction.Tests;

public class RetentionReleaseServiceTests
{
    private static readonly int CurrentYear = DateTimeOffset.UtcNow.Year;
    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid WbsBillingId = Guid.NewGuid();
    private static readonly Guid RevenueAccountId = Guid.NewGuid();
    private static readonly Guid ReceivableAccountId = Guid.NewGuid();
    private static readonly Guid ExpenseAccountId = Guid.NewGuid();
    private static readonly Guid PayableAccountId = Guid.NewGuid();
    private static readonly DateOnly ReleaseDate = new(2026, 8, 1);

    private static IActorRoleAssignmentStore BuildActorRoles() => new InMemoryActorRoleAssignmentStore(
        new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["ahmer.bilal"] = new[] { RetentionReleaseSecurity.MaintainerRoleKey },
            ["commercial.manager"] = new[] { RetentionReleaseWorkflow.ApproverRoleKey },
        });

    private static FakeProjectLookup BuildProjectLookup()
    {
        var lookup = new FakeProjectLookup();
        lookup.Add(new ProjectSummary(ProjectId, "PM-PRJ-2026-000001", "Tower A Construction", null, CustomerId, "Approved", Array.Empty<WbsElementSummary>()));
        return lookup;
    }

    private static Contract NewApprovedContract(FakeContractRepository contractRepository, decimal retentionPercentage = 10m)
    {
        var contract = new Contract("ahmer.bilal", ProjectId, "LumpSum", null, null, null, retentionPercentage);
        contract.AddBoqLine("BOQ-001", "Excavation", null, "M3", 100m, 50m, WbsBillingId);
        contract.AssignNumber("CON-CONTR-2026-000001");
        contract.Submit("ahmer.bilal");
        contract.Approve("con.manager");
        contractRepository.Add(contract);
        return contract;
    }

    /// <summary>Builds and directly persists an Approved Contract-type IPC withholding
    /// <paramref name="retentionAmount"/> of retention — bypasses IpcService (unit-tested separately) so this
    /// test class stays self-contained.</summary>
    private static void AddApprovedIpcWithRetention(
        FakeIpcRepository ipcRepository, Guid contractId, decimal grossValue, decimal retentionPercentage, string documentNumber)
    {
        var lineId = Guid.NewGuid();
        var rate = grossValue; // quantity 1 * rate = grossValue, simplest shape for a fixed retention test
        var ipc = new Ipc(
            "ahmer.bilal", ProjectId, CommercialDocumentType.Contract, contractId, Guid.NewGuid(),
            ReleaseDate, ReleaseDate, retentionPercentage, null, 0m, RevenueAccountId, ReceivableAccountId);
        ipc.AddLine(lineId, rate, 1m, 1m);
        ipc.AssignNumber(documentNumber);
        ipc.Submit("ahmer.bilal");
        ipc.Approve("engineer");
        ipcRepository.Add(ipc);
    }

    private static RetentionReleaseService BuildService(
        out FakeRetentionReleaseRepository repository, out IAuditLog auditLog,
        out FakeIpcRepository ipcRepository, out FakeContractRepository contractRepository,
        out FakeSubcontractRepository subcontractRepository, out FakeCustomerInvoicingService customerInvoicingService,
        out FakeVendorInvoicingService vendorInvoicingService)
    {
        repository = new FakeRetentionReleaseRepository();
        ipcRepository = new FakeIpcRepository();
        contractRepository = new FakeContractRepository();
        subcontractRepository = new FakeSubcontractRepository();
        customerInvoicingService = new FakeCustomerInvoicingService();
        vendorInvoicingService = new FakeVendorInvoicingService();
        var numberRanges = new InMemoryNumberRangeService(new[]
        {
            new NumberRangeDefinition(RetentionReleaseService.NumberRangeKey, "CON", "RETREL")
        });
        auditLog = new InMemoryAuditLog();
        var workflowInstances = new FakeWorkflowInstanceRepository();

        var workflowCatalog = new InMemoryWorkflowDefinitionCatalog(new[] { RetentionReleaseWorkflow.SubmitApprovalDefinition });
        var workflowEngine = new WorkflowEngine(workflowCatalog, new RoleBasedWorkflowEligibilityService(new InMemoryDelegationRegistry()));

        var securityCatalog = new InMemorySecurityCatalog(
            new[] { RetentionReleaseSecurity.MaintainerRole, RetentionReleaseSecurity.ApproverRole },
            new[] { RetentionReleaseSecurity.MaintainerDuty, RetentionReleaseSecurity.ApproverDuty });

        return new RetentionReleaseService(
            repository, ipcRepository, contractRepository, subcontractRepository, numberRanges,
            new AuditRecorder(auditLog), workflowEngine, workflowInstances,
            new AuthorizationService(securityCatalog), BuildActorRoles(), BuildProjectLookup(), customerInvoicingService,
            vendorInvoicingService);
    }

    private static CreateRetentionReleaseRequest BuildRequest(Guid contractId, decimal amountReleased) =>
        new(ProjectId, "Contract", contractId, ReleaseDate, amountReleased, "Manual", RevenueAccountId, ReceivableAccountId);

    [Fact]
    public async Task GetRetentionBalanceAsync_sums_approved_IPC_retention_with_no_releases_yet()
    {
        var service = BuildService(out _, out _, out var ipcRepository, out var contractRepository, out _, out _, out _);
        var contract = NewApprovedContract(contractRepository, retentionPercentage: 10m);
        AddApprovedIpcWithRetention(ipcRepository, contract.Id, grossValue: 2000m, retentionPercentage: 10m, documentNumber: "CON-IPC-2026-000001");

        var balance = await service.GetRetentionBalanceAsync("Contract", contract.Id);

        Assert.Equal(200m, balance.TotalWithheldToDate); // 2000 * 10%
        Assert.Equal(0m, balance.TotalReleasedToDate);
        Assert.Equal(200m, balance.OutstandingBalance);
    }

    [Fact]
    public async Task Create_succeeds_for_an_amount_within_the_outstanding_balance()
    {
        var service = BuildService(out _, out _, out var ipcRepository, out var contractRepository, out _, out _, out _);
        var contract = NewApprovedContract(contractRepository);
        AddApprovedIpcWithRetention(ipcRepository, contract.Id, 2000m, 10m, "CON-IPC-2026-000001");

        var created = await service.CreateAsync(BuildRequest(contract.Id, 150m), "ahmer.bilal", "C001");

        Assert.Equal($"CON-RETREL-{CurrentYear}-000001", created.DocumentNumber);
        Assert.Equal("Draft", created.Status);
        Assert.Equal(150m, created.AmountReleased);
        Assert.Equal("Manual", created.TriggerEvent);
    }

    [Fact]
    public async Task Create_rejects_an_amount_exceeding_the_outstanding_retention_balance()
    {
        var service = BuildService(out _, out _, out var ipcRepository, out var contractRepository, out _, out _, out _);
        var contract = NewApprovedContract(contractRepository);
        AddApprovedIpcWithRetention(ipcRepository, contract.Id, 2000m, 10m, "CON-IPC-2026-000001"); // withheld = 200

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(BuildRequest(contract.Id, 201m), "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_when_no_retention_has_ever_been_withheld()
    {
        var service = BuildService(out _, out _, out _, out var contractRepository, out _, out _, out _);
        var contract = NewApprovedContract(contractRepository);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(BuildRequest(contract.Id, 1m), "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task A_second_release_is_validated_against_the_balance_remaining_after_the_first()
    {
        var service = BuildService(out _, out _, out var ipcRepository, out var contractRepository, out _, out _, out _);
        var contract = NewApprovedContract(contractRepository);
        AddApprovedIpcWithRetention(ipcRepository, contract.Id, 2000m, 10m, "CON-IPC-2026-000001"); // withheld = 200

        var first = await service.CreateAsync(BuildRequest(contract.Id, 120m), "ahmer.bilal", "C001");
        await service.SubmitAsync(first.Id, "ahmer.bilal");
        await service.ApproveAsync(first.Id, "commercial.manager");

        var balance = await service.GetRetentionBalanceAsync("Contract", contract.Id);
        Assert.Equal(120m, balance.TotalReleasedToDate);
        Assert.Equal(80m, balance.OutstandingBalance);

        // A second release for more than the remaining 80 must fail...
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(BuildRequest(contract.Id, 81m), "ahmer.bilal", "C001"));

        // ...but exactly the remaining balance succeeds.
        var second = await service.CreateAsync(BuildRequest(contract.Id, 80m), "ahmer.bilal", "C001");
        Assert.Equal(80m, second.AmountReleased);
    }

    [Fact]
    public async Task Create_rejects_a_Contract_release_with_no_revenue_or_receivable_account()
    {
        var service = BuildService(out _, out _, out var ipcRepository, out var contractRepository, out _, out _, out _);
        var contract = NewApprovedContract(contractRepository);
        AddApprovedIpcWithRetention(ipcRepository, contract.Id, 2000m, 10m, "CON-IPC-2026-000001");

        var request = new CreateRetentionReleaseRequest(ProjectId, "Contract", contract.Id, ReleaseDate, 100m, "Manual");
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_an_unapproved_commercial_document()
    {
        var service = BuildService(out _, out _, out _, out var contractRepository, out _, out _, out _);
        var contract = new Contract("ahmer.bilal", ProjectId, "LumpSum", null, null, null, 10m);
        contract.AddBoqLine("BOQ-001", "Excavation", null, "M3", 100m, 50m, WbsBillingId);
        contract.AssignNumber("CON-CONTR-2026-000002");
        contractRepository.Add(contract); // still Draft

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(BuildRequest(contract.Id, 1m), "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task CreateAsync_throws_for_an_actor_with_no_Maintainer_role()
    {
        var service = BuildService(out _, out _, out var ipcRepository, out var contractRepository, out _, out _, out _);
        var contract = NewApprovedContract(contractRepository);
        AddApprovedIpcWithRetention(ipcRepository, contract.Id, 2000m, 10m, "CON-IPC-2026-000001");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.CreateAsync(BuildRequest(contract.Id, 100m), "commercial.manager", "C001"));
    }

    [Fact]
    public async Task Submit_then_approve_raises_a_real_AR_invoice_for_a_Contract_release()
    {
        var service = BuildService(out _, out _, out var ipcRepository, out var contractRepository, out _, out var customerInvoicing, out _);
        var contract = NewApprovedContract(contractRepository);
        AddApprovedIpcWithRetention(ipcRepository, contract.Id, 2000m, 10m, "CON-IPC-2026-000001");
        var created = await service.CreateAsync(BuildRequest(contract.Id, 150m), "ahmer.bilal", "C001");

        var submitted = await service.SubmitAsync(created.Id, "ahmer.bilal");
        Assert.Equal("Submitted", submitted.Status);
        Assert.Empty(customerInvoicing.RaisedInvoices);

        var approved = await service.ApproveAsync(created.Id, "commercial.manager");
        Assert.Equal("Approved", approved.Status);

        var raised = Assert.Single(customerInvoicing.RaisedInvoices);
        Assert.Equal(CustomerId, raised.CustomerId);
        Assert.Equal(RevenueAccountId, raised.RevenueAccountId);
        Assert.Equal(ReceivableAccountId, raised.ReceivableAccountId);
        Assert.Equal(150m, raised.NetAmount);
        Assert.NotNull(approved.LinkedArInvoiceId);
    }

    [Fact]
    public async Task Submit_then_approve_raises_a_real_AP_invoice_for_a_Subcontract_release()
    {
        var service = BuildService(out var repository, out _, out var ipcRepository, out _, out var subcontractRepository, out var customerInvoicing, out var vendorInvoicing);
        var subcontractorId = Guid.NewGuid();
        var subcontract = new Subcontract("ahmer.bilal", ProjectId, null, subcontractorId, 10m, null, null);
        subcontract.AddLine("SUB-001", "Formwork", null, "M2", 60m, 40m, WbsBillingId);
        subcontract.AssignNumber("CON-SUBCON-2026-000001");
        subcontract.Submit("ahmer.bilal");
        subcontract.Approve("con.manager");
        subcontractRepository.Add(subcontract);

        var ipc = new Ipc(
            "ahmer.bilal", ProjectId, CommercialDocumentType.Subcontract, subcontract.Id, Guid.NewGuid(),
            ReleaseDate, ReleaseDate, 10m, null, 0m, expenseAccountId: ExpenseAccountId, payableAccountId: PayableAccountId);
        ipc.AddLine(Guid.NewGuid(), 800m, 1m, 1m); // gross 800, retention 10% = 80
        ipc.AssignNumber("CON-IPC-2026-000001");
        ipc.Submit("ahmer.bilal");
        ipc.Approve("engineer");
        ipcRepository.Add(ipc);

        var request = new CreateRetentionReleaseRequest(
            ProjectId, "Subcontract", subcontract.Id, ReleaseDate, 80m, "DefectsLiabilityExpiry",
            ExpenseAccountId: ExpenseAccountId, PayableAccountId: PayableAccountId);
        var created = await service.CreateAsync(request, "ahmer.bilal", "C001");

        await service.SubmitAsync(created.Id, "ahmer.bilal");
        var approved = await service.ApproveAsync(created.Id, "commercial.manager");

        Assert.Empty(customerInvoicing.RaisedInvoices);
        var raised = Assert.Single(vendorInvoicing.RaisedInvoices);
        Assert.Equal(subcontractorId, raised.VendorId);
        Assert.Equal(ExpenseAccountId, raised.ExpenseAccountId);
        Assert.Equal(PayableAccountId, raised.PayableAccountId);
        Assert.Equal(80m, raised.NetAmount);
        Assert.Null(approved.LinkedArInvoiceId);
        Assert.NotNull(approved.LinkedApInvoiceId);
    }

    [Fact]
    public async Task Reject_after_submit_reaches_rejected()
    {
        var service = BuildService(out _, out _, out var ipcRepository, out var contractRepository, out _, out _, out _);
        var contract = NewApprovedContract(contractRepository);
        AddApprovedIpcWithRetention(ipcRepository, contract.Id, 2000m, 10m, "CON-IPC-2026-000001");
        var created = await service.CreateAsync(BuildRequest(contract.Id, 100m), "ahmer.bilal", "C001");
        await service.SubmitAsync(created.Id, "ahmer.bilal");

        var rejected = await service.RejectAsync(created.Id, "commercial.manager");
        Assert.Equal("Rejected", rejected.Status);
    }

    [Fact]
    public async Task Create_records_an_audit_entry()
    {
        var service = BuildService(out _, out var auditLog, out var ipcRepository, out var contractRepository, out _, out _, out _);
        var contract = NewApprovedContract(contractRepository);
        AddApprovedIpcWithRetention(ipcRepository, contract.Id, 2000m, 10m, "CON-IPC-2026-000001");
        var created = await service.CreateAsync(BuildRequest(contract.Id, 100m), "ahmer.bilal", "C001");

        var entries = auditLog.GetFor(new BusinessObjectReference(created.Id, "RetentionRelease", "Self"));
        var createEntry = Assert.Single(entries);
        Assert.Equal(AuditAction.Create, createEntry.Action);
    }

    [Fact]
    public async Task GetAsync_returns_null_for_unknown_id()
    {
        var service = BuildService(out _, out _, out _, out _, out _, out _, out _);
        Assert.Null(await service.GetAsync(Guid.NewGuid()));
    }
}
