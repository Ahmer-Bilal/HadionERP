using Gateway.Api.Localization;
using Platform.Localization.Translation;
using Platform.Security;
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

// Platform.Security: seeded with the one baseline role every real deployment needs at first login.
// Business-module roles/duties get registered the same way as those modules are built.
builder.Services.AddSingleton<ISecurityCatalog>(_ =>
{
    var manageSecurityDuty = new Duty(
        "ManagePlatformSecurity",
        "Manage roles, duties, and users",
        new[] { PrivilegeGrant.Unconditional("Platform.Security.Manage") });

    var administratorRole = new Role("PlatformAdministrator", "Platform administrator", new[] { manageSecurityDuty.Key });

    return new InMemorySecurityCatalog(new[] { administratorRole }, new[] { manageSecurityDuty });
});
builder.Services.AddSingleton<Platform.Security.IAuthorizationService, AuthorizationService>();

// Platform.Workflow: no approval workflows are registered yet because no business module exists yet to
// own one (e.g. a real "PurchaseOrder.Submit" workflow belongs to Modules.Procurement, not built yet).
// The engine itself is fully wired so a module's own startup registration just adds definitions to this
// same catalog when that module is built — nothing here needs to change.
builder.Services.AddSingleton<IWorkflowDefinitionCatalog>(_ =>
    new InMemoryWorkflowDefinitionCatalog(Array.Empty<WorkflowDefinition>()));
builder.Services.AddSingleton<IDelegationRegistry, InMemoryDelegationRegistry>();
builder.Services.AddSingleton<IWorkflowEligibilityService, RoleBasedWorkflowEligibilityService>();
builder.Services.AddSingleton<IWorkflowEngine, WorkflowEngine>();

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

app.Run();
