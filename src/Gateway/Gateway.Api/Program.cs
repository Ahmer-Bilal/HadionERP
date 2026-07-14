using Gateway.Api.Events;
using Gateway.Api.Localization;
using Microsoft.EntityFrameworkCore;
using Modules.MasterData.Application;
using Modules.MasterData.Infrastructure;
using Platform.Attachments;
using Platform.Audit;
using Platform.Configuration;
using Platform.Configuration.FeatureFlags;
using Platform.Core;
using Platform.Core.NumberRanges;
using Platform.Events;
using Platform.Events.Outbox;
using Platform.Localization.Translation;
using Platform.Notes;
using Platform.Security;
using Platform.Security.Sod;
using Platform.Workflow;
using Platform.Workflow.Delegation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

// Apps.Shell (the frontend, Vite dev server) runs on a different origin during development.
const string appsShellDevCorsPolicy = "AppsShellDev";
builder.Services.AddCors(options =>
{
    options.AddPolicy(appsShellDevCorsPolicy, policy => policy
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// Platform.Localization: in-memory reference implementation for now (docs/architecture/03-platform-services.md #1.4
// calls for a database-managed translation store later — same swap-behind-an-interface pattern as everywhere else).
builder.Services.AddSingleton(_ =>
{
    var service = new InMemoryTranslationService();
    Platform.Localization.LocalizationDefaults.RegisterDefaults(service);
    GatewayApiLocalizationDefaults.RegisterDefaults(service);
    return service;
});
builder.Services.AddSingleton<ITranslationService>(sp => sp.GetRequiredService<InMemoryTranslationService>());

// Platform.Security: seeded with the one baseline role every real deployment needs at first login, plus
// Modules.MasterData's own Business Partner Maintainer/Approver roles (BusinessPartnerSecurity) — a
// business module's roles/duties get registered the same way as those modules are built.
builder.Services.AddSingleton<ISecurityCatalog>(_ =>
{
    var manageSecurityDuty = new Duty(
        "ManagePlatformSecurity",
        "Manage roles, duties, and users",
        new[] { PrivilegeGrant.Unconditional("Platform.Security.Manage") });

    var administratorRole = new Role("PlatformAdministrator", "Platform administrator", new[] { manageSecurityDuty.Key });

    return new InMemorySecurityCatalog(
        new[] { administratorRole, BusinessPartnerSecurity.MaintainerRole, BusinessPartnerSecurity.ApproverRole,
                GLAccountSecurity.MaintainerRole, GLAccountSecurity.ApproverRole,
                ItemSecurity.MaintainerRole, ItemSecurity.ApproverRole,
                CostCenterSecurity.MaintainerRole, CostCenterSecurity.ApproverRole,
                TaxCodeSecurity.MaintainerRole, TaxCodeSecurity.ApproverRole },
        new[] { manageSecurityDuty, BusinessPartnerSecurity.MaintainerDuty, BusinessPartnerSecurity.ApproverDuty,
                GLAccountSecurity.MaintainerDuty, GLAccountSecurity.ApproverDuty,
                ItemSecurity.MaintainerDuty, ItemSecurity.ApproverDuty,
                CostCenterSecurity.MaintainerDuty, CostCenterSecurity.ApproverDuty,
                TaxCodeSecurity.MaintainerDuty, TaxCodeSecurity.ApproverDuty });
});
builder.Services.AddSingleton<Platform.Security.IAuthorizationService, AuthorizationService>();

// Platform.Security's Segregation of Duties engine: one real conflict rule is registered —
// BusinessPartnerSecurity.MaintainerApproverConflict, the "Create Vendor vs. Approve Vendor Payment"
// example from docs/architecture/03-platform-services.md #2.2. Not yet checked against a real role
// *assignment* anywhere (there is no role-assignment admin surface in this application yet — see
// Modules.MasterData/README.md's deferred list) but the rule and the engine that checks it are both real
// and tested; a future module registers its own conflict rules into this same list.
builder.Services.AddSingleton<ISodExceptionLog, InMemorySodExceptionLog>();
builder.Services.AddSingleton<ISodEngine>(sp =>
    new SodEngine(new[]
    {
        BusinessPartnerSecurity.MaintainerApproverConflict,
        GLAccountSecurity.MaintainerApproverConflict,
        ItemSecurity.MaintainerApproverConflict,
        CostCenterSecurity.MaintainerApproverConflict,
        TaxCodeSecurity.MaintainerApproverConflict,
    }, sp.GetRequiredService<ISodExceptionLog>()));

// Platform.Security's actor-to-role resolution: a temporary stand-in for real SSO/OIDC (see
// Platform.Security/README.md's "Deferred" section) — maps the bare actor-id strings every module
// currently hardcodes (e.g. BusinessPartnersController's MaintainerActor/ApproverActor) to real Role keys,
// so authorization/workflow-eligibility checks have real data instead of an unconditional grant.
builder.Services.AddSingleton<IActorRoleAssignmentStore>(_ => new InMemoryActorRoleAssignmentStore(
    new Dictionary<string, IReadOnlyCollection<string>>
    {
        ["system/ui"] = new[]
        {
            BusinessPartnerSecurity.MaintainerRoleKey, GLAccountSecurity.MaintainerRoleKey,
            ItemSecurity.MaintainerRoleKey, CostCenterSecurity.MaintainerRoleKey,
            TaxCodeSecurity.MaintainerRoleKey,
        },
        ["system/approver"] = new[]
        {
            BusinessPartnerWorkflow.ApproverRoleKey, GLAccountWorkflow.ApproverRoleKey,
            ItemWorkflow.ApproverRoleKey, CostCenterWorkflow.ApproverRoleKey,
            TaxCodeWorkflow.ApproverRoleKey,
        },
    }));

// Platform.Workflow: one real approval workflow is registered — Modules.MasterData's Business Partner
// onboarding approval (BusinessPartnerWorkflow.SubmitApprovalDefinition). A future module registers its
// own definitions into this same catalog when it's built — nothing here needs to change.
builder.Services.AddSingleton<IWorkflowDefinitionCatalog>(_ =>
    new InMemoryWorkflowDefinitionCatalog(new[]
    {
        BusinessPartnerWorkflow.SubmitApprovalDefinition,
        GLAccountWorkflow.SubmitApprovalDefinition,
        ItemWorkflow.SubmitApprovalDefinition,
        CostCenterWorkflow.SubmitApprovalDefinition,
        TaxCodeWorkflow.SubmitApprovalDefinition,
    }));
builder.Services.AddSingleton<IDelegationRegistry, InMemoryDelegationRegistry>();
builder.Services.AddSingleton<IWorkflowEligibilityService, RoleBasedWorkflowEligibilityService>();
builder.Services.AddSingleton<IWorkflowEngine, WorkflowEngine>();

// Platform.Events: the outbox + bus pipeline. One real, permanent operational event is registered here
// (application startup) — this proves the pipeline end-to-end without inventing a fake business-module
// event; a real module registers its own events into this same catalog when it's built.
builder.Services.AddSingleton<IEventCatalog>(_ =>
{
    var catalog = new InMemoryEventCatalog();
    catalog.Register(GatewayApiEventTypes.ApplicationStarted, "Raised once when the application finishes starting.");
    return catalog;
});
builder.Services.AddSingleton<IOutboxStore, InMemoryOutboxStore>();
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();
builder.Services.AddSingleton<IIntegrationEventPublisher, IntegrationEventPublisher>();
builder.Services.AddSingleton<OutboxRelay>();

// Platform.Audit: the permanent, hash-chained, append-only change log
// (docs/architecture/03-platform-services.md #5). The log is append-only by contract and (in a real
// deployment) by DB-role hardening; InMemoryAuditLog proves the chain mechanics first. A real module's
// Application layer calls IAuditRecorder at each lifecycle transition; nothing here needs to change when
// that module is built.
builder.Services.AddSingleton<IAuditLog, InMemoryAuditLog>();
builder.Services.AddSingleton<IAuditRecorder, AuditRecorder>();

// Platform.Configuration: the multi-level override hierarchy (docs/architecture/04-data-and-api.md #3).
// Two real, permanent settings registered here — a module registers its own the same way when built.
builder.Services.AddSingleton<IConfigurationCatalog>(_ =>
{
    var catalog = new InMemoryConfigurationCatalog();
    catalog.Register(new ConfigurationKeyDefinition(
        "Platform.DefaultLanguage", "The language a new session starts in before the user picks one.",
        new[] { ConfigurationLevel.System, ConfigurationLevel.Tenant }, DefaultValue: "en"));
    catalog.Register(new ConfigurationKeyDefinition(
        "Features.VerboseSystemStatus", "Whether /api/v1/system/status includes the detailed events/audit breakdown.",
        new[] { ConfigurationLevel.System }, DefaultValue: "true"));
    return catalog;
});
builder.Services.AddSingleton<IConfigurationStore, InMemoryConfigurationStore>();
builder.Services.AddSingleton<IConfigurationResolver, ConfigurationResolver>();
builder.Services.AddSingleton<IFeatureFlagService, FeatureFlagService>();

// Modules.MasterData: the first real, persisted business module — Postgres-backed, not in-memory, since
// real master data can't disappear on a restart the way platform-kernel demo data can.
// ConnectionStrings:Default comes from .NET User Secrets in Development (never a committed file — see
// HOW-TO-RUN.md) and would come from a real secret store in production.
var masterDataConnectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Missing ConnectionStrings:Default. Run `dotnet user-secrets set \"ConnectionStrings:Default\" " +
        "\"Host=localhost;Port=5432;Database=erp_platform_dev;Username=postgres;Password=...\"` " +
        "in src/Gateway/Gateway.Api — see HOW-TO-RUN.md.");

builder.Services.AddDbContext<MasterDataDbContext>(options => options.UseNpgsql(masterDataConnectionString));
builder.Services.AddScoped<IBusinessPartnerRepository, EfBusinessPartnerRepository>();
builder.Services.AddScoped<IWorkflowInstanceRepository, EfWorkflowInstanceRepository>();
// Platform.Attachments: file bytes are stored in Postgres via Modules.MasterData's own DbContext for now
// (see Platform.Attachments/README.md's "Deferred" section on real blob storage) — scoped, same lifetime
// as the DbContext it depends on.
builder.Services.AddScoped<IAttachmentRepository, EfAttachmentRepository>();
builder.Services.AddScoped<IAttachmentService, AttachmentService>();
// Platform.Notes: same "one real datastore now" reasoning as Attachments above.
builder.Services.AddScoped<INoteRepository, EfNoteRepository>();
builder.Services.AddScoped<INoteService, NoteService>();
builder.Services.AddScoped<BusinessPartnerService>();
builder.Services.AddScoped<IGLAccountRepository, EfGLAccountRepository>();
builder.Services.AddScoped<GLAccountService>();
builder.Services.AddScoped<IItemRepository, EfItemRepository>();
builder.Services.AddScoped<ItemService>();
builder.Services.AddScoped<ICostCenterRepository, EfCostCenterRepository>();
builder.Services.AddScoped<CostCenterService>();
builder.Services.AddScoped<ITaxCodeRepository, EfTaxCodeRepository>();
builder.Services.AddScoped<TaxCodeService>();
builder.Services.AddScoped<INumberRangeService>(sp => new EfCoreNumberRangeService(
    sp.GetRequiredService<MasterDataDbContext>(),
    new[]
    {
        new NumberRangeDefinition(BusinessPartnerService.NumberRangeKey, "MD", "BP"),
        new NumberRangeDefinition(GLAccountService.NumberRangeKey, "MD", "GL"),
        new NumberRangeDefinition(ItemService.NumberRangeKey, "MD", "ITM"),
        new NumberRangeDefinition(CostCenterService.NumberRangeKey, "MD", "CC"),
        new NumberRangeDefinition(TaxCodeService.NumberRangeKey, "MD", "TAX")
    }));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Local development intentionally runs HTTP-only (matches the Vite dev server, also plain HTTP) —
    // forcing an HTTPS redirect here has no HTTPS endpoint to redirect to and only produces a confusing
    // startup warning. Real deployments terminate TLS (a load balancer/ingress, or a configured
    // certificate) and this redirect becomes meaningful again outside Development.
    app.UseHttpsRedirection();
}

app.UseCors(appsShellDevCorsPolicy);
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

// Prove the events pipeline works end-to-end at boot: subscribe a logger, publish the one real event
// this application raises so far, then relay the outbox once. The actual "run the relay every few
// seconds" scheduler is a hosted background service, not built yet (see Platform.Events/README.md) —
// this one-time relay at startup is enough to prove enqueue -> outbox -> bus -> subscriber works.
var eventBus = app.Services.GetRequiredService<IEventBus>();
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
eventBus.Subscribe(GatewayApiEventTypes.ApplicationStarted, (integrationEvent, _) =>
{
    var payload = integrationEvent.DeserializePayload<ApplicationStartedPayload>();
    startupLogger.LogInformation(
        "Received {EventType}: {Application} started at {StartedAtUtc}",
        integrationEvent.EventType, payload?.Application, payload?.StartedAtUtc);
    return Task.CompletedTask;
});

var integrationEventPublisher = app.Services.GetRequiredService<IIntegrationEventPublisher>();
integrationEventPublisher.Enqueue(IntegrationEvent.Create(
    GatewayApiEventTypes.ApplicationStarted,
    new ApplicationStartedPayload("HadionERP", DateTimeOffset.UtcNow)));

var outboxRelay = app.Services.GetRequiredService<OutboxRelay>();
await outboxRelay.RelayPendingAsync();

// Prove the audit pipeline works live at boot: record one real, permanent operational entry ("application
// started"), then verify the chain is intact. This is the same "prove the mechanism at boot" pattern used
// for the events pipeline above. A real business module records its own audit entries at each lifecycle
// transition via IAuditRecorder; nothing here needs to change when that module is built.
var auditRecorder = app.Services.GetRequiredService<IAuditRecorder>();
var auditLog = app.Services.GetRequiredService<IAuditLog>();
auditRecorder.RecordCreate(
    businessObject: new BusinessObjectReference(Guid.NewGuid(), "Platform", "Startup"),
    actorPrincipalKey: "system/startup",
    summary: "Application started.",
    source: Environment.MachineName);
var chainBroken = auditLog.VerifyChain();
if (chainBroken is not null)
{
    startupLogger.LogError("Audit chain verification failed at boot on entry {AuditEntryId}.", chainBroken.Id);
}

app.Run();
