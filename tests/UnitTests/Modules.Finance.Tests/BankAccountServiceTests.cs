using Modules.Finance.Application;
using Modules.MasterData.Contracts;
using Platform.Audit;
using Platform.Core.NumberRanges;
using Platform.Security;
using Platform.Workflow;
using Platform.Workflow.Delegation;

namespace Modules.Finance.Tests;

public class BankAccountServiceTests
{
    private static readonly Guid LinkedGLAccountId = Guid.NewGuid();
    private static readonly Guid InactiveGLAccountId = Guid.NewGuid();
    private static readonly Guid NonPostableGLAccountId = Guid.NewGuid();

    private static IActorRoleAssignmentStore BuildActorRoles() => new InMemoryActorRoleAssignmentStore(
        new Dictionary<string, IReadOnlyCollection<string>>
        {
            ["ahmer.bilal"] = new[] { BankAccountSecurity.MaintainerRoleKey },
            ["finance.manager"] = new[] { BankAccountWorkflow.ApproverRoleKey },
        });

    private static FakeGLAccountLookup BuildGLAccountLookup()
    {
        var lookup = new FakeGLAccountLookup();
        lookup.Add(new GLAccountSummary(LinkedGLAccountId, "1010", "Bank - Al Rajhi Current", "Debit", IsPostable: true, IsActive: true));
        lookup.Add(new GLAccountSummary(InactiveGLAccountId, "1099", "Closed Bank Account", "Debit", IsPostable: true, IsActive: false));
        lookup.Add(new GLAccountSummary(NonPostableGLAccountId, "1000", "Bank Accounts (Group)", "Debit", IsPostable: false, IsActive: true));
        return lookup;
    }

    private static (BankAccountService BankAccounts, FakeBankAccountRepository Repo) BuildServices()
    {
        var repo = new FakeBankAccountRepository();
        var auditRecorder = new AuditRecorder(new InMemoryAuditLog());
        var workflowInstances = new FakeWorkflowInstanceRepository();

        var workflowCatalog = new InMemoryWorkflowDefinitionCatalog(new[] { BankAccountWorkflow.SubmitApprovalDefinition });
        var workflowEngine = new WorkflowEngine(workflowCatalog, new RoleBasedWorkflowEligibilityService(new InMemoryDelegationRegistry()));

        var securityCatalog = new InMemorySecurityCatalog(
            new[] { BankAccountSecurity.MaintainerRole, BankAccountSecurity.ApproverRole },
            new[] { BankAccountSecurity.MaintainerDuty, BankAccountSecurity.ApproverDuty });
        var authorizationService = new AuthorizationService(securityCatalog);

        var numberRanges = new InMemoryNumberRangeService(new[]
        {
            new NumberRangeDefinition(BankAccountService.NumberRangeKey, "FIN", "BANK")
        });

        var service = new BankAccountService(
            repo, numberRanges, auditRecorder, workflowEngine, workflowInstances,
            authorizationService, BuildActorRoles(), BuildGLAccountLookup());

        return (service, repo);
    }

    private static CreateBankAccountRequest ValidRequest() => new(
        "BANK-001", "Al Rajhi Current Account", "Al Rajhi Bank", LinkedGLAccountId, "حساب الراجحي الجاري", "SA0000000000000000000000");

    [Fact]
    public async Task Create_assigns_a_document_number_and_starts_in_draft()
    {
        var (bankAccounts, _) = BuildServices();
        var created = await bankAccounts.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");

        Assert.Equal("Draft", created.Status);
        Assert.Equal("BANK-001", created.AccountCode);
        Assert.True(created.IsActive);
        Assert.NotNull(created.DocumentNumber);
    }

    [Fact]
    public async Task Create_rejects_a_duplicate_account_code()
    {
        var (bankAccounts, _) = BuildServices();
        await bankAccounts.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            bankAccounts.CreateAsync(ValidRequest(), "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_an_inactive_linked_GL_account()
    {
        var (bankAccounts, _) = BuildServices();
        var request = ValidRequest() with { LinkedGLAccountId = InactiveGLAccountId };

        await Assert.ThrowsAsync<ArgumentException>(() => bankAccounts.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task Create_rejects_a_non_postable_linked_GL_account()
    {
        var (bankAccounts, _) = BuildServices();
        var request = ValidRequest() with { LinkedGLAccountId = NonPostableGLAccountId };

        await Assert.ThrowsAsync<ArgumentException>(() => bankAccounts.CreateAsync(request, "ahmer.bilal", "C001"));
    }

    [Fact]
    public async Task CreateAsync_throws_for_an_actor_with_no_Maintainer_role()
    {
        var (bankAccounts, _) = BuildServices();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            bankAccounts.CreateAsync(ValidRequest(), "finance.manager", "C001"));
    }

    [Fact]
    public async Task Submit_then_approve_transitions_to_Approved()
    {
        var (bankAccounts, _) = BuildServices();
        var created = await bankAccounts.CreateAsync(ValidRequest(), "ahmer.bilal", "C001");

        await bankAccounts.SubmitAsync(created.Id, "ahmer.bilal");
        var approved = await bankAccounts.ApproveAsync(created.Id, "finance.manager");

        Assert.Equal("Approved", approved.Status);
    }

    [Fact]
    public async Task GetAsync_returns_null_for_unknown_id()
    {
        var (bankAccounts, _) = BuildServices();
        Assert.Null(await bankAccounts.GetAsync(Guid.NewGuid()));
    }
}
