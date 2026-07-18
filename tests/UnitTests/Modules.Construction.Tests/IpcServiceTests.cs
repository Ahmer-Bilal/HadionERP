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

public class IpcServiceTests
{
    private static readonly int CurrentYear = DateTimeOffset.UtcNow.Year;
    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid ProjectWithNoCustomerId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid WbsBillingId = Guid.NewGuid();
    private static readonly Guid RevenueAccountId = Guid.NewGuid();
    private static readonly Guid ReceivableAccountId = Guid.NewGuid();
    private static readonly Guid ExpenseAccountId = Guid.NewGuid();
    private static readonly Guid PayableAccountId = Guid.NewGuid();
    private static readonly DateOnly PeriodStart = new(2026, 7, 1);
    private static readonly DateOnly PeriodEnd = new(2026, 7, 31);

    private static IActorRoleAssignmentStore BuildActorRoles() => new InMemoryActorRoleAssignmentStore(
        new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["ahmer.bilal"] = new[] { IpcSecurity.MaintainerRoleKey },
            ["engineer"] = new[] { IpcWorkflow.ApproverRoleKey },
        });

    private static FakeProjectLookup BuildProjectLookup()
    {
        var lookup = new FakeProjectLookup();
        lookup.Add(new ProjectSummary(ProjectId, "PM-PRJ-2026-000001", "Tower A Construction", null, CustomerId, "Approved", Array.Empty<WbsElementSummary>()));
        lookup.Add(new ProjectSummary(ProjectWithNoCustomerId, "PM-PRJ-2026-000002", "Internal Capital Project", null, null, "Approved", Array.Empty<WbsElementSummary>()));
        return lookup;
    }

    private static Contract NewApprovedContract(FakeContractRepository contractRepository, decimal quantity = 100m, decimal rate = 50m, Guid? projectId = null)
    {
        var contract = new Contract("ahmer.bilal", projectId ?? ProjectId, "LumpSum", null, 15m, null);
        contract.AddBoqLine("BOQ-001", "Excavation", null, "M3", quantity, rate, WbsBillingId);
        contract.AssignNumber("CON-CONTR-2026-000001");
        contract.Submit("ahmer.bilal");
        contract.Approve("con.manager");
        contractRepository.Add(contract);
        return contract;
    }

    /// <summary>Builds and certifies a Measurement Sheet directly against the domain (bypassing
    /// MeasurementSheetService, which is unit-tested separately) so IpcServiceTests stays self-contained.</summary>
    private static MeasurementSheet NewCertifiedSheet(
        FakeMeasurementSheetRepository repository, Guid contractId, Guid lineId, decimal quantityCertified,
        DateOnly? periodStart = null, DateOnly? periodEnd = null, string documentNumber = "CON-MEAS-2026-000001",
        Guid? projectId = null, CommercialDocumentType documentType = CommercialDocumentType.Contract)
    {
        var sheet = new MeasurementSheet(
            "ahmer.bilal", projectId ?? ProjectId, documentType, contractId,
            periodStart ?? PeriodStart, periodEnd ?? PeriodEnd, notes: null);
        var line = sheet.AddLine(lineId, quantityCertified, remarks: null);
        sheet.AssignNumber(documentNumber);
        sheet.Submit("ahmer.bilal");
        sheet.RecordCertifiedQuantities(new Dictionary<Guid, decimal> { [line.Id] = quantityCertified });
        sheet.Approve("engineer");
        repository.Add(sheet);
        return sheet;
    }

    private static IpcService BuildService(
        out FakeIpcRepository repository, out IAuditLog auditLog,
        out FakeContractRepository contractRepository, out FakeSubcontractRepository subcontractRepository,
        out FakeMeasurementSheetRepository measurementSheetRepository, out FakeCustomerInvoicingService customerInvoicingService,
        out FakeVendorInvoicingService vendorInvoicingService)
    {
        repository = new FakeIpcRepository();
        contractRepository = new FakeContractRepository();
        subcontractRepository = new FakeSubcontractRepository();
        measurementSheetRepository = new FakeMeasurementSheetRepository();
        customerInvoicingService = new FakeCustomerInvoicingService();
        vendorInvoicingService = new FakeVendorInvoicingService();
        var numberRanges = new InMemoryNumberRangeService(new[]
        {
            new NumberRangeDefinition(IpcService.NumberRangeKey, "CON", "IPC")
        });
        auditLog = new InMemoryAuditLog();
        var workflowInstances = new FakeWorkflowInstanceRepository();

        var workflowCatalog = new InMemoryWorkflowDefinitionCatalog(new[] { IpcWorkflow.SubmitApprovalDefinition });
        var workflowEngine = new WorkflowEngine(workflowCatalog, new RoleBasedWorkflowEligibilityService(new InMemoryDelegationRegistry()));

        var securityCatalog = new InMemorySecurityCatalog(
            new[] { IpcSecurity.MaintainerRole, IpcSecurity.ApproverRole },
            new[] { IpcSecurity.MaintainerDuty, IpcSecurity.ApproverDuty });

        return new IpcService(
            repository, measurementSheetRepository, contractRepository, subcontractRepository, numberRanges,
            new AuditRecorder(auditLog), workflowEngine, workflowInstances,
            new AuthorizationService(securityCatalog), BuildActorRoles(), BuildProjectLookup(), customerInvoicingService,
            vendorInvoicingService);
    }

    private static CreateIpcRequest BuildRequest(Guid contractId, Guid sheetId, decimal otherDeductions = 0m) =>
        new(ProjectId, "Contract", contractId, sheetId, otherDeductions, RevenueAccountId, ReceivableAccountId);

    [Fact]
    public async Task Create_assigns_a_document_number_and_computes_the_waterfall()
    {
        var service = BuildService(out _, out _, out var contractRepository, out _, out var sheetRepository, out _, out _);
        var contract = NewApprovedContract(contractRepository, quantity: 100m, rate: 50m);
        var lineId = contract.BoqLines.Single().Id;
        var sheet = NewCertifiedSheet(sheetRepository, contract.Id, lineId, quantityCertified: 40m);

        var created = await service.CreateAsync(BuildRequest(contract.Id, sheet.Id), "ahmer.bilal", "C001");

        Assert.Equal($"CON-IPC-{CurrentYear}-000001", created.DocumentNumber);
        Assert.Equal("Draft", created.Status);
        Assert.Single(created.Lines);
        Assert.Equal(2000m, created.GrossValueToDate); // 40 * 50
        Assert.Equal(2000m, created.GrossValueThisPeriod); // first IPC, nothing prior
        Assert.Equal(0m, created.GrossValuePreviousIpc);
        Assert.Equal(15m, created.AdvancePaymentPercentageApplied); // snapshotted from Contract
        Assert.Equal(300m, created.AdvanceRecoveryAmount); // 2000 * 15%
        Assert.Equal(RevenueAccountId, created.RevenueAccountId);
        Assert.Equal(ReceivableAccountId, created.ReceivableAccountId);
        Assert.Null(created.LinkedArInvoiceId); // not raised until certified
    }

    [Fact]
    public async Task Create_snapshots_a_Contracts_own_retention_percentage_not_just_a_Subcontracts()
    {
        // Regression test: LoadCommercialDocumentAsync used to always pass null for a Contract's retention
        // percentage, since Contract had no such field — meaning a Contract-type IPC never withheld any
        // retention at all, only a Subcontract-type one did. Contract.RetentionPercentage (added alongside
        // RetentionRelease) closes that gap.
        var service = BuildService(out _, out _, out var contractRepository, out _, out var sheetRepository, out _, out _);
        var contract = new Contract("ahmer.bilal", ProjectId, "LumpSum", null, 15m, null, retentionPercentage: 10m);
        contract.AddBoqLine("BOQ-001", "Excavation", null, "M3", 100m, 50m, WbsBillingId);
        contract.AssignNumber("CON-CONTR-2026-000002");
        contract.Submit("ahmer.bilal");
        contract.Approve("con.manager");
        contractRepository.Add(contract);

        var sheet = NewCertifiedSheet(sheetRepository, contract.Id, contract.BoqLines.Single().Id, quantityCertified: 40m);
        var created = await service.CreateAsync(BuildRequest(contract.Id, sheet.Id), "ahmer.bilal", "C001");

        Assert.Equal(10m, created.RetentionPercentageApplied);
        Assert.Equal(200m, created.RetentionAmount); // 40 * 50 * 10%
    }

    [Fact]
    public async Task Create_rejects_an_unknown_commercial_document_type()
    {
        var service = BuildService(out _, out _, out var contractRepository, out _, out var sheetRepository, out _, out _);
        var contract = NewApprovedContract(contractRepository);
        var sheet = NewCertifiedSheet(sheetRepository, contract.Id, contract.BoqLines.Single().Id, 40m);

        var request = new CreateIpcRequest(ProjectId, "NotAType", contract.Id, sheet.Id, 0m);
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_Contract_ipc_with_no_revenue_or_receivable_account()
    {
        var service = BuildService(out _, out _, out var contractRepository, out _, out var sheetRepository, out _, out _);
        var contract = NewApprovedContract(contractRepository);
        var sheet = NewCertifiedSheet(sheetRepository, contract.Id, contract.BoqLines.Single().Id, 40m);

        var request = new CreateIpcRequest(ProjectId, "Contract", contract.Id, sheet.Id, 0m);
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_Contract_ipc_when_the_project_has_no_customer()
    {
        var service = BuildService(out _, out _, out var contractRepository, out _, out var sheetRepository, out _, out _);
        var contract = NewApprovedContract(contractRepository, projectId: ProjectWithNoCustomerId);
        var sheet = NewCertifiedSheet(sheetRepository, contract.Id, contract.BoqLines.Single().Id, 40m, projectId: ProjectWithNoCustomerId);

        var request = new CreateIpcRequest(ProjectWithNoCustomerId, "Contract", contract.Id, sheet.Id, 0m, RevenueAccountId, ReceivableAccountId);
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_measurement_sheet_that_is_not_yet_Approved()
    {
        var service = BuildService(out _, out _, out var contractRepository, out _, out var sheetRepository, out _, out _);
        var contract = NewApprovedContract(contractRepository);
        var sheet = new MeasurementSheet("ahmer.bilal", ProjectId, CommercialDocumentType.Contract, contract.Id, PeriodStart, PeriodEnd, null);
        sheet.AddLine(contract.BoqLines.Single().Id, 40m, null);
        sheet.AssignNumber("CON-MEAS-2026-000009");
        sheetRepository.Add(sheet); // still Draft

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(BuildRequest(contract.Id, sheet.Id), "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_second_IPC_against_the_same_measurement_sheet()
    {
        var service = BuildService(out _, out _, out var contractRepository, out _, out var sheetRepository, out _, out _);
        var contract = NewApprovedContract(contractRepository);
        var sheet = NewCertifiedSheet(sheetRepository, contract.Id, contract.BoqLines.Single().Id, 40m);

        await service.CreateAsync(BuildRequest(contract.Id, sheet.Id), "ahmer.bilal", "C001");

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(BuildRequest(contract.Id, sheet.Id), "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_computes_QuantityToDate_cumulatively_across_sibling_sheets()
    {
        var service = BuildService(out _, out _, out var contractRepository, out _, out var sheetRepository, out _, out _);
        var contract = NewApprovedContract(contractRepository, quantity: 100m, rate: 50m);
        var lineId = contract.BoqLines.Single().Id;

        var sheet1 = NewCertifiedSheet(sheetRepository, contract.Id, lineId, 40m, documentNumber: "CON-MEAS-2026-000001");
        var ipc1 = await service.CreateAsync(BuildRequest(contract.Id, sheet1.Id), "ahmer.bilal", "C001");
        Assert.Equal(2000m, ipc1.GrossValueToDate);

        var sheet2 = NewCertifiedSheet(
            sheetRepository, contract.Id, lineId, 30m,
            periodStart: new DateOnly(2026, 8, 1), periodEnd: new DateOnly(2026, 8, 31), documentNumber: "CON-MEAS-2026-000002");
        var ipc2 = await service.CreateAsync(BuildRequest(contract.Id, sheet2.Id), "ahmer.bilal", "C001");

        Assert.Equal(3500m, ipc2.GrossValueToDate); // (40+30) * 50
        Assert.Equal(1500m, ipc2.GrossValueThisPeriod); // 30 * 50
        Assert.Equal(2000m, ipc2.GrossValuePreviousIpc); // 3500 - 1500
    }

    [Fact]
    public async Task Create_works_against_a_subcontract_with_expense_and_payable_accounts()
    {
        var service = BuildService(out _, out _, out _, out var subcontractRepository, out var sheetRepository, out _, out _);
        var subcontract = new Subcontract("ahmer.bilal", ProjectId, null, Guid.NewGuid(), 10m, null, null);
        var line = subcontract.AddLine("SUB-001", "Formwork", null, "M2", 60m, 40m, WbsBillingId);
        subcontract.AssignNumber("CON-SUBCON-2026-000001");
        subcontract.Submit("ahmer.bilal");
        subcontract.Approve("con.manager");
        subcontractRepository.Add(subcontract);

        var sheet = new MeasurementSheet("ahmer.bilal", ProjectId, CommercialDocumentType.Subcontract, subcontract.Id, PeriodStart, PeriodEnd, null);
        var sheetLine = sheet.AddLine(line.Id, 20m, null);
        sheet.AssignNumber("CON-MEAS-2026-000001");
        sheet.Submit("ahmer.bilal");
        sheet.RecordCertifiedQuantities(new Dictionary<Guid, decimal> { [sheetLine.Id] = 20m });
        sheet.Approve("engineer");
        sheetRepository.Add(sheet);

        var request = new CreateIpcRequest(ProjectId, "Subcontract", subcontract.Id, sheet.Id, 0m, ExpenseAccountId: ExpenseAccountId, PayableAccountId: PayableAccountId);
        var created = await service.CreateAsync(request, "ahmer.bilal", "C001");

        Assert.Equal(800m, created.GrossValueThisPeriod); // 20 * 40
        Assert.Equal(10m, created.RetentionPercentageApplied); // snapshotted from Subcontract
        Assert.Null(created.RevenueAccountId);
        Assert.Null(created.ReceivableAccountId);
        Assert.Equal(ExpenseAccountId, created.ExpenseAccountId);
        Assert.Equal(PayableAccountId, created.PayableAccountId);
    }

    [Fact]
    public async Task Create_rejects_a_Subcontract_ipc_with_no_expense_or_payable_account()
    {
        var service = BuildService(out _, out _, out _, out var subcontractRepository, out var sheetRepository, out _, out _);
        var subcontract = new Subcontract("ahmer.bilal", ProjectId, null, Guid.NewGuid(), 10m, null, null);
        var line = subcontract.AddLine("SUB-001", "Formwork", null, "M2", 60m, 40m, WbsBillingId);
        subcontract.AssignNumber("CON-SUBCON-2026-000001");
        subcontract.Submit("ahmer.bilal");
        subcontract.Approve("con.manager");
        subcontractRepository.Add(subcontract);

        var sheet = NewCertifiedSheet(sheetRepository, subcontract.Id, line.Id, 20m, documentType: CommercialDocumentType.Subcontract);
        var request = new CreateIpcRequest(ProjectId, "Subcontract", subcontract.Id, sheet.Id, 0m);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task CreateAsync_throws_for_an_actor_with_no_Maintainer_role()
    {
        var service = BuildService(out _, out _, out var contractRepository, out _, out var sheetRepository, out _, out _);
        var contract = NewApprovedContract(contractRepository);
        var sheet = NewCertifiedSheet(sheetRepository, contract.Id, contract.BoqLines.Single().Id, 40m);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.CreateAsync(BuildRequest(contract.Id, sheet.Id), "engineer", "C001"));
    }

    [Fact]
    public async Task Submit_then_approve_reaches_approved_and_raises_a_real_AR_invoice_for_a_Contract_ipc()
    {
        var service = BuildService(out _, out _, out var contractRepository, out _, out var sheetRepository, out var customerInvoicing, out _);
        var contract = NewApprovedContract(contractRepository, quantity: 100m, rate: 50m);
        var sheet = NewCertifiedSheet(sheetRepository, contract.Id, contract.BoqLines.Single().Id, 40m);
        var created = await service.CreateAsync(BuildRequest(contract.Id, sheet.Id), "ahmer.bilal", "C001");

        var submitted = await service.SubmitAsync(created.Id, "ahmer.bilal");
        Assert.Equal("Submitted", submitted.Status);
        Assert.Empty(customerInvoicing.RaisedInvoices); // not raised until certified

        var approved = await service.ApproveAsync(created.Id, "engineer");
        Assert.Equal("Approved", approved.Status);

        var raised = Assert.Single(customerInvoicing.RaisedInvoices);
        Assert.Equal(CustomerId, raised.CustomerId);
        Assert.Equal(RevenueAccountId, raised.RevenueAccountId);
        Assert.Equal(ReceivableAccountId, raised.ReceivableAccountId);
        Assert.Equal(approved.NetPayable, raised.NetAmount);
        Assert.NotNull(approved.LinkedArInvoiceId);
    }

    [Fact]
    public async Task Submit_then_approve_reaches_approved_and_raises_a_real_AP_invoice_for_a_Subcontract_ipc()
    {
        var service = BuildService(out _, out _, out _, out var subcontractRepository, out var sheetRepository, out var customerInvoicing, out var vendorInvoicing);
        var subcontractorId = Guid.NewGuid();
        var subcontract = new Subcontract("ahmer.bilal", ProjectId, null, subcontractorId, 10m, null, null);
        var line = subcontract.AddLine("SUB-001", "Formwork", null, "M2", 60m, 40m, WbsBillingId);
        subcontract.AssignNumber("CON-SUBCON-2026-000001");
        subcontract.Submit("ahmer.bilal");
        subcontract.Approve("con.manager");
        subcontractRepository.Add(subcontract);

        var sheet = NewCertifiedSheet(sheetRepository, subcontract.Id, line.Id, 20m, documentType: CommercialDocumentType.Subcontract);
        var request = new CreateIpcRequest(ProjectId, "Subcontract", subcontract.Id, sheet.Id, 0m, ExpenseAccountId: ExpenseAccountId, PayableAccountId: PayableAccountId);
        var created = await service.CreateAsync(request, "ahmer.bilal", "C001");

        var submitted = await service.SubmitAsync(created.Id, "ahmer.bilal");
        Assert.Equal("Submitted", submitted.Status);
        Assert.Empty(vendorInvoicing.RaisedInvoices); // not raised until certified

        var approved = await service.ApproveAsync(created.Id, "engineer");
        Assert.Equal("Approved", approved.Status);

        Assert.Empty(customerInvoicing.RaisedInvoices); // AP, not AR
        var raised = Assert.Single(vendorInvoicing.RaisedInvoices);
        Assert.Equal(subcontractorId, raised.VendorId);
        Assert.Equal(ExpenseAccountId, raised.ExpenseAccountId);
        Assert.Equal(PayableAccountId, raised.PayableAccountId);
        Assert.Equal(approved.NetPayable, raised.NetAmount);
        Assert.Null(approved.LinkedArInvoiceId);
        Assert.NotNull(approved.LinkedApInvoiceId);
    }

    [Fact]
    public async Task Reject_after_submit_reaches_rejected()
    {
        var service = BuildService(out _, out _, out var contractRepository, out _, out var sheetRepository, out _, out _);
        var contract = NewApprovedContract(contractRepository);
        var sheet = NewCertifiedSheet(sheetRepository, contract.Id, contract.BoqLines.Single().Id, 40m);
        var created = await service.CreateAsync(BuildRequest(contract.Id, sheet.Id), "ahmer.bilal", "C001");
        await service.SubmitAsync(created.Id, "ahmer.bilal");

        var rejected = await service.RejectAsync(created.Id, "engineer");
        Assert.Equal("Rejected", rejected.Status);
    }

    [Fact]
    public async Task Create_records_an_audit_entry()
    {
        var service = BuildService(out _, out var auditLog, out var contractRepository, out _, out var sheetRepository, out _, out _);
        var contract = NewApprovedContract(contractRepository);
        var sheet = NewCertifiedSheet(sheetRepository, contract.Id, contract.BoqLines.Single().Id, 40m);
        var created = await service.CreateAsync(BuildRequest(contract.Id, sheet.Id), "ahmer.bilal", "C001");

        var entries = auditLog.GetFor(new BusinessObjectReference(created.Id, "Ipc", "Self"));
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
