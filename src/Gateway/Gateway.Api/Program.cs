using System.Text;
using Gateway.Api.Events;
using Gateway.Api.Localization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Modules.Construction.Application;
using Modules.Finance.Application;
using Modules.Finance.Contracts;
using Modules.Identity.Application;
using Modules.Identity.Infrastructure;
using Modules.MasterData.Application;
using Modules.MasterData.Contracts;
using Modules.MasterData.Infrastructure;
using Modules.Procurement.Application;
using Modules.ProjectManagement.Application;
using Modules.ProjectManagement.Contracts;
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

// Real authentication (`ARCHITECTURE-AUDIT.md` Part 1 §1) — every request needs a valid bearer token by
// default (the global AuthorizeFilter below) except AuthController.Login, the one endpoint that produces a
// token in the first place. Signing key comes from .NET User Secrets in Development (never a committed
// file — see HOW-TO-RUN.md), same handling as ConnectionStrings:Default.
var jwtSigningKey = builder.Configuration["Identity:JwtSigningKey"]
    ?? throw new InvalidOperationException(
        "Missing Identity:JwtSigningKey. Run `dotnet user-secrets set \"Identity:JwtSigningKey\" " +
        "\"<a random string at least 32 characters long>\"` in src/Gateway/Gateway.Api — see HOW-TO-RUN.md.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = JwtTokenService.Issuer,
            ValidAudience = JwtTokenService.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

// Global default-deny: every controller action requires a valid, authenticated principal unless it opts
// out with [AllowAnonymous] (only AuthController.Login does) — avoids the class of bug where a new
// controller simply forgets to add [Authorize].
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
builder.Services.AddControllers(options => options.Filters.Add(new AuthorizeFilter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Lets Swagger UI actually call protected endpoints during manual testing — "Authorize" button accepts
    // a raw bearer token, same convention every JWT-protected API's Swagger doc uses.
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Paste the token returned by POST /api/v1/identity/auth/login (no \"Bearer \" prefix needed).",
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                { Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});
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
                TaxCodeSecurity.MaintainerRole, TaxCodeSecurity.ApproverRole,
                JournalEntrySecurity.MaintainerRole, JournalEntrySecurity.ApproverRole,
                APInvoiceSecurity.MaintainerRole, APInvoiceSecurity.ApproverRole,
                VendorPrequalificationSecurity.MaintainerRole,
                VendorPrequalificationSecurity.CommercialReviewerRole, VendorPrequalificationSecurity.LegalReviewerRole,
                VendorPrequalificationSecurity.TechnicalReviewerRole, VendorPrequalificationSecurity.HseReviewerRole,
                VendorPrequalificationSecurity.QualityReviewerRole,
                PurchaseRequisitionSecurity.MaintainerRole, PurchaseRequisitionSecurity.ApproverRole,
                RequestForQuotationSecurity.MaintainerRole, RequestForQuotationSecurity.ApproverRole,
                PurchaseOrderSecurity.MaintainerRole, PurchaseOrderSecurity.ApproverRole,
                GoodsReceiptNoteSecurity.MaintainerRole, GoodsReceiptNoteSecurity.ApproverRole,
                ProjectSecurity.MaintainerRole, ProjectSecurity.ApproverRole,
                LookupSecurity.AdministratorRole, IdentitySecurity.AdministratorRole,
                BankAccountSecurity.MaintainerRole, BankAccountSecurity.ApproverRole,
                PaymentSecurity.MaintainerRole, PaymentSecurity.ApproverRole,
                ContractSecurity.MaintainerRole, ContractSecurity.ApproverRole,
                SubcontractSecurity.MaintainerRole, SubcontractSecurity.ApproverRole },
        new[] { manageSecurityDuty, BusinessPartnerSecurity.MaintainerDuty, BusinessPartnerSecurity.ApproverDuty,
                GLAccountSecurity.MaintainerDuty, GLAccountSecurity.ApproverDuty,
                ItemSecurity.MaintainerDuty, ItemSecurity.ApproverDuty,
                CostCenterSecurity.MaintainerDuty, CostCenterSecurity.ApproverDuty,
                TaxCodeSecurity.MaintainerDuty, TaxCodeSecurity.ApproverDuty,
                JournalEntrySecurity.MaintainerDuty, JournalEntrySecurity.ApproverDuty,
                APInvoiceSecurity.MaintainerDuty, APInvoiceSecurity.ApproverDuty,
                VendorPrequalificationSecurity.MaintainerDuty,
                VendorPrequalificationSecurity.CommercialReviewerDuty, VendorPrequalificationSecurity.LegalReviewerDuty,
                VendorPrequalificationSecurity.TechnicalReviewerDuty, VendorPrequalificationSecurity.HseReviewerDuty,
                VendorPrequalificationSecurity.QualityReviewerDuty,
                PurchaseRequisitionSecurity.MaintainerDuty, PurchaseRequisitionSecurity.ApproverDuty,
                RequestForQuotationSecurity.MaintainerDuty, RequestForQuotationSecurity.ApproverDuty,
                PurchaseOrderSecurity.MaintainerDuty, PurchaseOrderSecurity.ApproverDuty,
                GoodsReceiptNoteSecurity.MaintainerDuty, GoodsReceiptNoteSecurity.ApproverDuty,
                ProjectSecurity.MaintainerDuty, ProjectSecurity.ApproverDuty,
                LookupSecurity.AdministratorDuty, IdentitySecurity.AdministratorDuty,
                BankAccountSecurity.MaintainerDuty, BankAccountSecurity.ApproverDuty,
                PaymentSecurity.MaintainerDuty, PaymentSecurity.ApproverDuty,
                ContractSecurity.MaintainerDuty, ContractSecurity.ApproverDuty,
                SubcontractSecurity.MaintainerDuty, SubcontractSecurity.ApproverDuty });
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
        JournalEntrySecurity.MaintainerApproverConflict,
        APInvoiceSecurity.MaintainerApproverConflict,
        VendorPrequalificationSecurity.MaintainerCommercialReviewerConflict,
        VendorPrequalificationSecurity.MaintainerLegalReviewerConflict,
        VendorPrequalificationSecurity.MaintainerTechnicalReviewerConflict,
        VendorPrequalificationSecurity.MaintainerHseReviewerConflict,
        VendorPrequalificationSecurity.MaintainerQualityReviewerConflict,
        PurchaseRequisitionSecurity.MaintainerApproverConflict,
        RequestForQuotationSecurity.MaintainerApproverConflict,
        PurchaseOrderSecurity.MaintainerApproverConflict,
        GoodsReceiptNoteSecurity.MaintainerApproverConflict,
        ProjectSecurity.MaintainerApproverConflict,
        BankAccountSecurity.MaintainerApproverConflict,
        PaymentSecurity.MaintainerApproverConflict,
        ContractSecurity.MaintainerApproverConflict,
        SubcontractSecurity.MaintainerApproverConflict,
    }, sp.GetRequiredService<ISodExceptionLog>()));

// Platform.Security's actor-to-role resolution — used to be a hardcoded in-memory dictionary mapping
// "system/ui"/"system/approver" to fixed role sets (a temporary stand-in disclosed in
// `ARCHITECTURE-AUDIT.md` Part 1 §1). Real authentication now exists (see the JWT/AddAuthentication setup
// above and Modules.Identity below) — this resolves a REAL logged-in username's assigned roles from the
// database instead. Scoped, not Singleton, since it now depends on a scoped DbContext.
builder.Services.AddScoped<IActorRoleAssignmentStore, EfActorRoleAssignmentStore>();

// Every role key any module currently registers — used only to grant the bootstrap admin (see
// IdentitySeeder below) full access on first run, so the system is immediately usable after a fresh
// deploy with no separate manual setup step. Not a security boundary in itself; real per-user role
// assignment happens through the Users admin UI (Modules.Identity) from this point on.
var allRegisteredRoleKeys = new[]
{
    BusinessPartnerSecurity.MaintainerRoleKey, BusinessPartnerWorkflow.ApproverRoleKey,
    GLAccountSecurity.MaintainerRoleKey, GLAccountWorkflow.ApproverRoleKey,
    ItemSecurity.MaintainerRoleKey, ItemWorkflow.ApproverRoleKey,
    CostCenterSecurity.MaintainerRoleKey, CostCenterWorkflow.ApproverRoleKey,
    TaxCodeSecurity.MaintainerRoleKey, TaxCodeWorkflow.ApproverRoleKey,
    JournalEntrySecurity.MaintainerRoleKey, JournalEntryWorkflow.ApproverRoleKey,
    APInvoiceSecurity.MaintainerRoleKey, APInvoiceWorkflow.ApproverRoleKey,
    VendorPrequalificationSecurity.MaintainerRoleKey,
    VendorPrequalificationWorkflow.CommercialReviewerRoleKey, VendorPrequalificationWorkflow.LegalReviewerRoleKey,
    VendorPrequalificationWorkflow.TechnicalReviewerRoleKey, VendorPrequalificationWorkflow.HseReviewerRoleKey,
    VendorPrequalificationWorkflow.QualityReviewerRoleKey,
    PurchaseRequisitionSecurity.MaintainerRoleKey, PurchaseRequisitionWorkflow.ApproverRoleKey,
    RequestForQuotationSecurity.MaintainerRoleKey, RequestForQuotationWorkflow.ApproverRoleKey,
    PurchaseOrderSecurity.MaintainerRoleKey, PurchaseOrderWorkflow.ApproverRoleKey,
    GoodsReceiptNoteSecurity.MaintainerRoleKey, GoodsReceiptNoteWorkflow.ApproverRoleKey,
    ProjectSecurity.MaintainerRoleKey, ProjectWorkflow.ApproverRoleKey,
    LookupSecurity.AdministratorRoleKey, IdentitySecurity.AdministratorRoleKey,
    BankAccountSecurity.MaintainerRoleKey, BankAccountWorkflow.ApproverRoleKey,
    PaymentSecurity.MaintainerRoleKey, PaymentWorkflow.ApproverRoleKey,
};

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
        JournalEntryWorkflow.SubmitApprovalDefinition,
        APInvoiceWorkflow.SubmitApprovalDefinition,
        VendorPrequalificationWorkflow.SubmitApprovalDefinition,
        PurchaseRequisitionWorkflow.SubmitApprovalDefinition,
        RequestForQuotationWorkflow.SubmitApprovalDefinition,
        PurchaseOrderWorkflow.SubmitApprovalDefinition,
        GoodsReceiptNoteWorkflow.SubmitApprovalDefinition,
        ProjectWorkflow.SubmitApprovalDefinition,
        BankAccountWorkflow.SubmitApprovalDefinition,
        PaymentWorkflow.SubmitApprovalDefinition,
        ContractWorkflow.SubmitApprovalDefinition,
        SubcontractWorkflow.SubmitApprovalDefinition,
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
    catalog.Register(new ConfigurationKeyDefinition(
        VendorPrequalificationService.ValidityMonthsConfigurationKey,
        "How many months an Approved Vendor Prequalification certificate stays valid from its approval date.",
        new[] { ConfigurationLevel.System, ConfigurationLevel.Tenant, ConfigurationLevel.Company }, DefaultValue: "24"));
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
// The admin-configurable lookup engine (Countries/Business Role Types/Address Types/Units of Measure/
// Trades and any future admin-created type) — registered before BusinessPartnerService/ItemService below
// since both now validate references against it via constructor injection.
builder.Services.AddScoped<ILookupRepository, EfLookupRepository>();
builder.Services.AddScoped<LookupService>();
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
// Modules.MasterData.Contracts: the published, read-only lookups Modules.Finance depends on instead of
// reaching into this module's Domain/Infrastructure/Application internals directly
// (docs/architecture/01-architecture-foundation.md #3.2).
builder.Services.AddScoped<IGLAccountLookup, EfGLAccountLookup>();
builder.Services.AddScoped<IBusinessPartnerLookup, EfBusinessPartnerLookup>();
builder.Services.AddScoped<ITaxCodeLookup, EfTaxCodeLookup>();
builder.Services.AddScoped<ICostCenterLookup, EfCostCenterLookup>();
builder.Services.AddScoped<IItemLookup, EfItemLookup>();
builder.Services.AddScoped<ILookupCatalog, EfLookupCatalog>();
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

// Modules.Finance: the second real, persisted business module. Own "finance" Postgres schema in the same
// physical database as MasterData's — schema-per-module is the boundary being enforced, not one database
// per module (docs/architecture/01-architecture-foundation.md #3.2). Depends on
// Modules.MasterData.Contracts only for cross-module lookups (IGLAccountLookup/ICostCenterLookup above),
// never on Modules.MasterData.Domain/Infrastructure/Application directly.
builder.Services.AddDbContext<Modules.Finance.Infrastructure.FinanceDbContext>(options => options.UseNpgsql(masterDataConnectionString));
builder.Services.AddScoped<Modules.Finance.Application.IJournalEntryRepository, Modules.Finance.Infrastructure.EfJournalEntryRepository>();
// JournalEntryService's own IWorkflowInstanceRepository/INumberRangeService are constructed directly here,
// not registered as shared container services — Modules.MasterData already registers those same
// interfaces bound to ITS OWN DbContext/schema, and a second AddScoped for the same interface would just
// have the last registration silently win for both modules. Each module's copy stays bound to its own
// DbContext this way, exactly as the "own schema, own tables" design in FinanceDbContext intends.
builder.Services.AddScoped<Modules.Finance.Application.JournalEntryService>(sp => new Modules.Finance.Application.JournalEntryService(
    sp.GetRequiredService<Modules.Finance.Application.IJournalEntryRepository>(),
    new Modules.Finance.Infrastructure.EfCoreNumberRangeService(
        sp.GetRequiredService<Modules.Finance.Infrastructure.FinanceDbContext>(),
        new[] { new NumberRangeDefinition(JournalEntryService.NumberRangeKey, "FIN", "JE") }),
    sp.GetRequiredService<IAuditRecorder>(),
    sp.GetRequiredService<IWorkflowEngine>(),
    new Modules.Finance.Infrastructure.EfWorkflowInstanceRepository(sp.GetRequiredService<Modules.Finance.Infrastructure.FinanceDbContext>()),
    sp.GetRequiredService<Platform.Security.IAuthorizationService>(),
    sp.GetRequiredService<IActorRoleAssignmentStore>(),
    sp.GetRequiredService<IGLAccountLookup>(),
    sp.GetRequiredService<ICostCenterLookup>()));
builder.Services.AddScoped<Modules.Finance.Application.IAPInvoiceRepository, Modules.Finance.Infrastructure.EfAPInvoiceRepository>();
// APInvoiceService reuses JournalEntryService directly (both Modules.Finance.Application — an intra-module
// dependency, not a cross-module one, so no Contracts package is needed here) to generate and reverse the
// linked posting through JournalEntryService.CreateSystemGeneratedAsync/ReverseAsync — see APInvoice's own
// doc comment for why Posting an invoice creates a real second document rather than just flipping a flag.
builder.Services.AddScoped<Modules.Finance.Application.APInvoiceService>(sp => new Modules.Finance.Application.APInvoiceService(
    sp.GetRequiredService<Modules.Finance.Application.IAPInvoiceRepository>(),
    new Modules.Finance.Infrastructure.EfCoreNumberRangeService(
        sp.GetRequiredService<Modules.Finance.Infrastructure.FinanceDbContext>(),
        new[] { new NumberRangeDefinition(Modules.Finance.Application.APInvoiceService.NumberRangeKey, "FIN", "AP") }),
    sp.GetRequiredService<IAuditRecorder>(),
    sp.GetRequiredService<IWorkflowEngine>(),
    new Modules.Finance.Infrastructure.EfWorkflowInstanceRepository(sp.GetRequiredService<Modules.Finance.Infrastructure.FinanceDbContext>()),
    sp.GetRequiredService<Platform.Security.IAuthorizationService>(),
    sp.GetRequiredService<IActorRoleAssignmentStore>(),
    sp.GetRequiredService<IBusinessPartnerLookup>(),
    sp.GetRequiredService<IGLAccountLookup>(),
    sp.GetRequiredService<ICostCenterLookup>(),
    sp.GetRequiredService<ITaxCodeLookup>(),
    sp.GetRequiredService<Modules.Finance.Application.JournalEntryService>(),
    sp.GetRequiredService<Modules.Finance.Application.IPaymentRepository>()));

// Bank Accounts & Payments — closes ARCHITECTURE-AUDIT.md Part 2 §16 ("no way anywhere in this system to
// record that an AP invoice was actually paid"). Both live in Modules.Finance (same reasoning as JournalEntry/
// APInvoice — real SAP/Dynamics keep House Bank/Payment documents in Finance, not a separate module).
builder.Services.AddScoped<Modules.Finance.Application.IBankAccountRepository, Modules.Finance.Infrastructure.EfBankAccountRepository>();
builder.Services.AddScoped<Modules.Finance.Application.BankAccountService>(sp => new Modules.Finance.Application.BankAccountService(
    sp.GetRequiredService<Modules.Finance.Application.IBankAccountRepository>(),
    new Modules.Finance.Infrastructure.EfCoreNumberRangeService(
        sp.GetRequiredService<Modules.Finance.Infrastructure.FinanceDbContext>(),
        new[] { new NumberRangeDefinition(Modules.Finance.Application.BankAccountService.NumberRangeKey, "FIN", "BANK") }),
    sp.GetRequiredService<IAuditRecorder>(),
    sp.GetRequiredService<IWorkflowEngine>(),
    new Modules.Finance.Infrastructure.EfWorkflowInstanceRepository(sp.GetRequiredService<Modules.Finance.Infrastructure.FinanceDbContext>()),
    sp.GetRequiredService<Platform.Security.IAuthorizationService>(),
    sp.GetRequiredService<IActorRoleAssignmentStore>(),
    sp.GetRequiredService<IGLAccountLookup>()));

builder.Services.AddScoped<Modules.Finance.Application.IPaymentRepository, Modules.Finance.Infrastructure.EfPaymentRepository>();
// Depends on IAPInvoiceRepository/IBankAccountRepository directly (both intra-module, same reasoning as
// APInvoiceService reusing JournalEntryService directly) plus MasterData.Contracts' ILookupCatalog for the
// one real cross-module contract call this slice adds (PaymentMethod validation).
builder.Services.AddScoped<Modules.Finance.Application.PaymentService>(sp => new Modules.Finance.Application.PaymentService(
    sp.GetRequiredService<Modules.Finance.Application.IPaymentRepository>(),
    sp.GetRequiredService<Modules.Finance.Application.IAPInvoiceRepository>(),
    sp.GetRequiredService<Modules.Finance.Application.IBankAccountRepository>(),
    new Modules.Finance.Infrastructure.EfCoreNumberRangeService(
        sp.GetRequiredService<Modules.Finance.Infrastructure.FinanceDbContext>(),
        new[] { new NumberRangeDefinition(Modules.Finance.Application.PaymentService.NumberRangeKey, "FIN", "PAY") }),
    sp.GetRequiredService<IAuditRecorder>(),
    sp.GetRequiredService<IWorkflowEngine>(),
    new Modules.Finance.Infrastructure.EfWorkflowInstanceRepository(sp.GetRequiredService<Modules.Finance.Infrastructure.FinanceDbContext>()),
    sp.GetRequiredService<Platform.Security.IAuthorizationService>(),
    sp.GetRequiredService<IActorRoleAssignmentStore>(),
    sp.GetRequiredService<IBusinessPartnerLookup>(),
    sp.GetRequiredService<ILookupCatalog>(),
    sp.GetRequiredService<Modules.Finance.Application.JournalEntryService>()));

// Modules.Finance.Contracts: the published, synchronous cross-module contract call
// docs/architecture/01-architecture-foundation.md §3.2 names as its own worked example ("Procurement asks
// Finance's IBudgetCheckService before releasing a PO"). PassThroughBudgetCheckService always allows for now
// — Budget Control itself is deferred Finance depth (PROGRESS.md), not built yet — see that class's own doc
// comment for why this is disclosed rather than faking enforcement against numbers that don't exist.
builder.Services.AddScoped<IBudgetCheckService, Modules.Finance.Infrastructure.PassThroughBudgetCheckService>();
// Modules.Finance.Contracts' other publication: the 3-way match's read-only view into an AP Invoice's
// vendor/amount, same dependency direction as IBudgetCheckService above.
builder.Services.AddScoped<Modules.Finance.Contracts.IAPInvoiceLookup, Modules.Finance.Infrastructure.EfAPInvoiceLookup>();

// Modules.Procurement: the third real, persisted business module, starting Phase 2. Own "procurement"
// Postgres schema in the same physical database as MasterData's/Finance's. Depends on
// Modules.MasterData.Contracts only (IBusinessPartnerLookup above), never on MasterData's own Domain/
// Infrastructure/Application directly.
builder.Services.AddDbContext<Modules.Procurement.Infrastructure.ProcurementDbContext>(options => options.UseNpgsql(masterDataConnectionString));
builder.Services.AddScoped<IVendorPrequalificationRepository, Modules.Procurement.Infrastructure.EfVendorPrequalificationRepository>();
// Same "constructed inline, not container-registered" reasoning as Modules.Finance's own
// EfCoreNumberRangeService/EfWorkflowInstanceRepository/IAttachmentService above — MasterData already
// registers IAttachmentRepository/IAttachmentService bound to ITS OWN DbContext, so Procurement's copy
// stays bound to its own ProcurementDbContext instead of colliding with that registration.
builder.Services.AddScoped<VendorPrequalificationService>(sp => new VendorPrequalificationService(
    sp.GetRequiredService<IVendorPrequalificationRepository>(),
    new Modules.Procurement.Infrastructure.EfCoreNumberRangeService(
        sp.GetRequiredService<Modules.Procurement.Infrastructure.ProcurementDbContext>(),
        new[] { new NumberRangeDefinition(VendorPrequalificationService.NumberRangeKey, "PROC", "VPQ") }),
    sp.GetRequiredService<IAuditRecorder>(),
    sp.GetRequiredService<IWorkflowEngine>(),
    new Modules.Procurement.Infrastructure.EfWorkflowInstanceRepository(sp.GetRequiredService<Modules.Procurement.Infrastructure.ProcurementDbContext>()),
    sp.GetRequiredService<Platform.Security.IAuthorizationService>(),
    sp.GetRequiredService<IActorRoleAssignmentStore>(),
    sp.GetRequiredService<IBusinessPartnerLookup>(),
    sp.GetRequiredService<IConfigurationResolver>(),
    new AttachmentService(new Modules.Procurement.Infrastructure.EfAttachmentRepository(sp.GetRequiredService<Modules.Procurement.Infrastructure.ProcurementDbContext>()))));

builder.Services.AddScoped<IPurchaseRequisitionRepository, Modules.Procurement.Infrastructure.EfPurchaseRequisitionRepository>();
builder.Services.AddScoped<PurchaseRequisitionService>(sp => new PurchaseRequisitionService(
    sp.GetRequiredService<IPurchaseRequisitionRepository>(),
    new Modules.Procurement.Infrastructure.EfCoreNumberRangeService(
        sp.GetRequiredService<Modules.Procurement.Infrastructure.ProcurementDbContext>(),
        new[] { new NumberRangeDefinition(PurchaseRequisitionService.NumberRangeKey, "PROC", "PR") }),
    sp.GetRequiredService<IAuditRecorder>(),
    sp.GetRequiredService<IWorkflowEngine>(),
    new Modules.Procurement.Infrastructure.EfWorkflowInstanceRepository(sp.GetRequiredService<Modules.Procurement.Infrastructure.ProcurementDbContext>()),
    sp.GetRequiredService<Platform.Security.IAuthorizationService>(),
    sp.GetRequiredService<IActorRoleAssignmentStore>(),
    sp.GetRequiredService<IItemLookup>(),
    sp.GetRequiredService<ICostCenterLookup>()));

builder.Services.AddScoped<IRequestForQuotationRepository, Modules.Procurement.Infrastructure.EfRequestForQuotationRepository>();
// Depends on IPurchaseRequisitionRepository directly (an intra-module dependency, not cross-module — both
// live in Modules.Procurement.Application, so no Contracts package is needed, same reasoning as
// APInvoiceService reusing JournalEntryService directly within Modules.Finance).
builder.Services.AddScoped<RequestForQuotationService>(sp => new RequestForQuotationService(
    sp.GetRequiredService<IRequestForQuotationRepository>(),
    sp.GetRequiredService<IPurchaseRequisitionRepository>(),
    new Modules.Procurement.Infrastructure.EfCoreNumberRangeService(
        sp.GetRequiredService<Modules.Procurement.Infrastructure.ProcurementDbContext>(),
        new[] { new NumberRangeDefinition(RequestForQuotationService.NumberRangeKey, "PROC", "RFQ") }),
    sp.GetRequiredService<IAuditRecorder>(),
    sp.GetRequiredService<IWorkflowEngine>(),
    new Modules.Procurement.Infrastructure.EfWorkflowInstanceRepository(sp.GetRequiredService<Modules.Procurement.Infrastructure.ProcurementDbContext>()),
    sp.GetRequiredService<Platform.Security.IAuthorizationService>(),
    sp.GetRequiredService<IActorRoleAssignmentStore>(),
    sp.GetRequiredService<IBusinessPartnerLookup>()));

builder.Services.AddScoped<IPurchaseOrderRepository, Modules.Procurement.Infrastructure.EfPurchaseOrderRepository>();
// Depends on IRequestForQuotationRepository/IPurchaseRequisitionRepository directly (both intra-module, same
// reasoning as RequestForQuotationService reusing IPurchaseRequisitionRepository) plus Modules.Finance's
// published IBudgetCheckService for the one real cross-module contract call this slice adds.
builder.Services.AddScoped<PurchaseOrderService>(sp => new PurchaseOrderService(
    sp.GetRequiredService<IPurchaseOrderRepository>(),
    sp.GetRequiredService<IRequestForQuotationRepository>(),
    sp.GetRequiredService<IPurchaseRequisitionRepository>(),
    new Modules.Procurement.Infrastructure.EfCoreNumberRangeService(
        sp.GetRequiredService<Modules.Procurement.Infrastructure.ProcurementDbContext>(),
        new[] { new NumberRangeDefinition(PurchaseOrderService.NumberRangeKey, "PROC", "PO") }),
    sp.GetRequiredService<IAuditRecorder>(),
    sp.GetRequiredService<IWorkflowEngine>(),
    new Modules.Procurement.Infrastructure.EfWorkflowInstanceRepository(sp.GetRequiredService<Modules.Procurement.Infrastructure.ProcurementDbContext>()),
    sp.GetRequiredService<Platform.Security.IAuthorizationService>(),
    sp.GetRequiredService<IActorRoleAssignmentStore>(),
    sp.GetRequiredService<IBusinessPartnerLookup>(),
    sp.GetRequiredService<IItemLookup>(),
    sp.GetRequiredService<ICostCenterLookup>(),
    sp.GetRequiredService<IBudgetCheckService>()));

builder.Services.AddScoped<IGoodsReceiptNoteRepository, Modules.Procurement.Infrastructure.EfGoodsReceiptNoteRepository>();
// Depends on IPurchaseOrderRepository directly (intra-module, same reasoning as PurchaseOrderService
// reusing IRequestForQuotationRepository/IPurchaseRequisitionRepository).
builder.Services.AddScoped<GoodsReceiptNoteService>(sp => new GoodsReceiptNoteService(
    sp.GetRequiredService<IGoodsReceiptNoteRepository>(),
    sp.GetRequiredService<IPurchaseOrderRepository>(),
    new Modules.Procurement.Infrastructure.EfCoreNumberRangeService(
        sp.GetRequiredService<Modules.Procurement.Infrastructure.ProcurementDbContext>(),
        new[] { new NumberRangeDefinition(GoodsReceiptNoteService.NumberRangeKey, "PROC", "GRN") }),
    sp.GetRequiredService<IAuditRecorder>(),
    sp.GetRequiredService<IWorkflowEngine>(),
    new Modules.Procurement.Infrastructure.EfWorkflowInstanceRepository(sp.GetRequiredService<Modules.Procurement.Infrastructure.ProcurementDbContext>()),
    sp.GetRequiredService<Platform.Security.IAuthorizationService>(),
    sp.GetRequiredService<IActorRoleAssignmentStore>()));

// Reads PO/GRN data this module already owns directly, plus Finance's published IAPInvoiceLookup for the
// invoiced-amount side of the check — see ThreeWayMatchService's own doc comment for the dependency-direction
// reasoning.
builder.Services.AddScoped<ThreeWayMatchService>(sp => new ThreeWayMatchService(
    sp.GetRequiredService<IPurchaseOrderRepository>(),
    sp.GetRequiredService<IGoodsReceiptNoteRepository>(),
    sp.GetRequiredService<Modules.Finance.Contracts.IAPInvoiceLookup>()));

// Modules.ProjectManagement: the fourth real, persisted business module, starting Phase 3. Own
// "projectmanagement" Postgres schema in the same physical database as every other module's. Depends on
// Modules.MasterData.Contracts only (IBusinessPartnerLookup, for the optional Customer reference), never on
// MasterData's own Domain/Infrastructure/Application directly.
builder.Services.AddDbContext<Modules.ProjectManagement.Infrastructure.ProjectManagementDbContext>(options => options.UseNpgsql(masterDataConnectionString));
builder.Services.AddScoped<IProjectRepository, Modules.ProjectManagement.Infrastructure.EfProjectRepository>();
builder.Services.AddScoped<ProjectService>(sp => new ProjectService(
    sp.GetRequiredService<IProjectRepository>(),
    new Modules.ProjectManagement.Infrastructure.EfCoreNumberRangeService(
        sp.GetRequiredService<Modules.ProjectManagement.Infrastructure.ProjectManagementDbContext>(),
        new[] { new NumberRangeDefinition(ProjectService.NumberRangeKey, "PM", "PRJ") }),
    sp.GetRequiredService<IAuditRecorder>(),
    sp.GetRequiredService<IWorkflowEngine>(),
    new Modules.ProjectManagement.Infrastructure.EfWorkflowInstanceRepository(sp.GetRequiredService<Modules.ProjectManagement.Infrastructure.ProjectManagementDbContext>()),
    sp.GetRequiredService<Platform.Security.IAuthorizationService>(),
    sp.GetRequiredService<IActorRoleAssignmentStore>(),
    sp.GetRequiredService<IBusinessPartnerLookup>()));
// Modules.ProjectManagement.Contracts: the published, read-only lookup Modules.Construction depends on
// instead of reaching into this module's Domain/Infrastructure/Application internals directly
// (docs/architecture/01-architecture-foundation.md #3.2).
builder.Services.AddScoped<IProjectLookup, Modules.ProjectManagement.Infrastructure.EfProjectLookup>();

// Modules.Identity: the fifth real, persisted business module — real user authentication, replacing the
// hardcoded actor literals every controller used before (`ARCHITECTURE-AUDIT.md` Part 1 §1). Own
// "identity" Postgres schema, same physical database as every other module. UserService's constructor
// dependencies (IAuditRecorder/IAuthorizationService/IActorRoleAssignmentStore/ISecurityCatalog/
// ISodEngine/ISodExceptionLog) are all already registered above, so it's auto-wired like BusinessPartnerService.
builder.Services.AddDbContext<IdentityDbContext>(options => options.UseNpgsql(masterDataConnectionString));
builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddScoped<UserService>();
builder.Services.AddSingleton<ITokenService>(_ => new JwtTokenService(jwtSigningKey));

// Modules.Construction: the sixth real, persisted business module, Phase 3's commercial layer on top of
// ProjectManagement's WBS backbone (docs/architecture/07-project-accounting-and-financial-architecture.md
// §4). Own "construction" Postgres schema, same physical database as every other module's. Depends on
// Modules.ProjectManagement.Contracts (IProjectLookup, to validate a Contract's Project and each BOQ line's
// WBS element) and Modules.MasterData.Contracts (ILookupCatalog, for ContractType/UnitOfMeasure) only, never
// on either module's own Domain/Infrastructure/Application directly.
builder.Services.AddDbContext<Modules.Construction.Infrastructure.ConstructionDbContext>(options => options.UseNpgsql(masterDataConnectionString));
builder.Services.AddScoped<IContractRepository, Modules.Construction.Infrastructure.EfContractRepository>();
builder.Services.AddScoped<ContractService>(sp => new ContractService(
    sp.GetRequiredService<IContractRepository>(),
    new Modules.Construction.Infrastructure.EfCoreNumberRangeService(
        sp.GetRequiredService<Modules.Construction.Infrastructure.ConstructionDbContext>(),
        new[] { new NumberRangeDefinition(ContractService.NumberRangeKey, "CON", "CONTR") }),
    sp.GetRequiredService<IAuditRecorder>(),
    sp.GetRequiredService<IWorkflowEngine>(),
    new Modules.Construction.Infrastructure.EfWorkflowInstanceRepository(sp.GetRequiredService<Modules.Construction.Infrastructure.ConstructionDbContext>()),
    sp.GetRequiredService<Platform.Security.IAuthorizationService>(),
    sp.GetRequiredService<IActorRoleAssignmentStore>(),
    sp.GetRequiredService<IProjectLookup>(),
    sp.GetRequiredService<ILookupCatalog>()));

// Subcontract: the next Phase 3 slice on top of Contract — references the same IProjectLookup/
// ILookupCatalog, plus IContractRepository directly (same module, no cross-module lookup needed) for the
// optional back-to-back ContractId traceability check, and IBusinessPartnerLookup (already registered
// above under Modules.MasterData.Contracts) to validate the subcontractor.
builder.Services.AddScoped<ISubcontractRepository, Modules.Construction.Infrastructure.EfSubcontractRepository>();
builder.Services.AddScoped<SubcontractService>(sp => new SubcontractService(
    sp.GetRequiredService<ISubcontractRepository>(),
    sp.GetRequiredService<IContractRepository>(),
    new Modules.Construction.Infrastructure.EfCoreNumberRangeService(
        sp.GetRequiredService<Modules.Construction.Infrastructure.ConstructionDbContext>(),
        new[] { new NumberRangeDefinition(SubcontractService.NumberRangeKey, "CON", "SUBCON") }),
    sp.GetRequiredService<IAuditRecorder>(),
    sp.GetRequiredService<IWorkflowEngine>(),
    new Modules.Construction.Infrastructure.EfWorkflowInstanceRepository(sp.GetRequiredService<Modules.Construction.Infrastructure.ConstructionDbContext>()),
    sp.GetRequiredService<Platform.Security.IAuthorizationService>(),
    sp.GetRequiredService<IActorRoleAssignmentStore>(),
    sp.GetRequiredService<IProjectLookup>(),
    sp.GetRequiredService<IBusinessPartnerLookup>(),
    sp.GetRequiredService<ILookupCatalog>()));

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
app.UseAuthentication();
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

// Seed the admin-configurable lookup engine's system-defined types/values (Country/BusinessRoleType/
// AddressType/UnitOfMeasure/Trade) — idempotent, only inserts what's missing, so an administrator's own
// edits through the Lookup Data admin panel are never touched. See LookupSeeder's own doc comment.
using (var seedScope = app.Services.CreateScope())
{
    var lookupDbContext = seedScope.ServiceProvider.GetRequiredService<MasterDataDbContext>();
    await LookupSeeder.SeedAsync(lookupDbContext);
}

// Seed one bootstrap administrator if the `users` table is completely empty — without this, real
// authentication would make the system unable to bootstrap itself (nobody could log in to create the
// first user). Idempotent — never runs again once any user exists. See IdentitySeeder's own doc comment.
var bootstrapAdminPassword = app.Configuration["Identity:BootstrapAdminPassword"]
    ?? throw new InvalidOperationException(
        "Missing Identity:BootstrapAdminPassword. Run `dotnet user-secrets set \"Identity:BootstrapAdminPassword\" " +
        "\"<a strong password>\"` in src/Gateway/Gateway.Api — see HOW-TO-RUN.md.");
using (var identitySeedScope = app.Services.CreateScope())
{
    var identityDbContext = identitySeedScope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await IdentitySeeder.SeedAsync(identityDbContext, bootstrapAdminPassword, allRegisteredRoleKeys);
}

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
