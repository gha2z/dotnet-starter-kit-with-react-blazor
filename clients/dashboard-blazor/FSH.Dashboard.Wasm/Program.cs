using FSH.Dashboard.Wasm;
using FSH.Dashboard.Wasm.Auth;
using FSH.BlazorShared.Auth;
using FSH.BlazorShared.Infrastructure;
using FSH.BlazorShared.Realtime;
using FSH.BlazorShared.Services;
using FSH.BlazorShared.Sse;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;
using FSH.BlazorShared.Theming;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Client-side AuthorizeRouteView policy checks log info-level "Authorization failed"
// for every anonymous route visit — expected noise, not an error. Silence it.
builder.Logging.AddFilter("Microsoft.AspNetCore.Authorization", LogLevel.Warning);

var baseAddress = builder.HostEnvironment.BaseAddress;

// Runtime config (load /config.json from the app origin) — singleton so it can be
// resolved from the root provider inside IHttpClientFactory client factories
// (their configure delegates run in the root scope; scoped services throw
// DirectScopedResolvedFromRootException there). Loaded eagerly in Main below.
builder.Services.AddSingleton<IRuntimeConfigService>(sp =>
    new RuntimeConfigService(
        new HttpClient { BaseAddress = new Uri(baseAddress) },
        sp.GetRequiredService<ILogger<RuntimeConfigService>>()));

// Auth
builder.Services.AddSingleton<ITokenStore>(sp =>
    new DashboardTokenStore(sp.GetRequiredService<IJSRuntime>()));
builder.Services.AddScoped<AuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<AuthStateProvider>());
builder.Services.AddScoped<IPermissionsProvider, PermissionsProvider>();
builder.Services.AddScoped<ImpersonationHandoff>();

// Auth HTTP client (no auth handler — used for login/refresh only)
builder.Services.AddHttpClient("FSH.Auth", (sp, client) =>
{
    var config = sp.GetRequiredService<IRuntimeConfigService>();
    client.BaseAddress = RuntimeConfigService.ResolveApiBase(baseAddress, config.ApiBaseUrl);
});

// Auth service
builder.Services.AddScoped<IAuthService, AuthService>();

// Tenant-scoped data services
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IImpersonationService, ImpersonationService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IHealthService, HealthService>();
builder.Services.AddScoped<IAuditService, AuditService>();

builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("Permissions.Users.View", p => p.RequireClaim("permission", "Permissions.Users.View"));
    options.AddPolicy("Permissions.Users.Create", p => p.RequireClaim("permission", "Permissions.Users.Create"));
    options.AddPolicy("Permissions.Users.Update", p => p.RequireClaim("permission", "Permissions.Users.Update"));
    options.AddPolicy("Permissions.Users.Delete", p => p.RequireClaim("permission", "Permissions.Users.Delete"));
    options.AddPolicy("Permissions.Users.Impersonate", p => p.RequireClaim("permission", "Permissions.Users.Impersonate"));
    options.AddPolicy("Permissions.Roles.View", p => p.RequireClaim("permission", "Permissions.Roles.View"));
    options.AddPolicy("Permissions.Roles.Create", p => p.RequireClaim("permission", "Permissions.Roles.Create"));
    options.AddPolicy("Permissions.Roles.Update", p => p.RequireClaim("permission", "Permissions.Roles.Update"));
    options.AddPolicy("Permissions.Roles.Delete", p => p.RequireClaim("permission", "Permissions.Roles.Delete"));
    options.AddPolicy("Permissions.Groups.View", p => p.RequireClaim("permission", "Permissions.Groups.View"));
    options.AddPolicy("Permissions.Groups.Create", p => p.RequireClaim("permission", "Permissions.Groups.Create"));
    options.AddPolicy("Permissions.Groups.Update", p => p.RequireClaim("permission", "Permissions.Groups.Update"));
    options.AddPolicy("Permissions.Groups.Delete", p => p.RequireClaim("permission", "Permissions.Groups.Delete"));
    options.AddPolicy("Permissions.Sessions.ViewAll", p => p.RequireClaim("permission", "Permissions.Sessions.ViewAll"));
    options.AddPolicy("Permissions.Sessions.RevokeAll", p => p.RequireClaim("permission", "Permissions.Sessions.RevokeAll"));
    options.AddPolicy("Permissions.Sessions.Revoke", p => p.RequireClaim("permission", "Permissions.Sessions.Revoke"));
    options.AddPolicy("Permissions.Tenants.View", p => p.RequireClaim("permission", "Permissions.Tenants.View"));
    options.AddPolicy("Permissions.Tenants.Create", p => p.RequireClaim("permission", "Permissions.Tenants.Create"));
    options.AddPolicy("Permissions.Tenants.Update", p => p.RequireClaim("permission", "Permissions.Tenants.Update"));
    options.AddPolicy("Permissions.Tenants.UpgradeSubscription", p => p.RequireClaim("permission", "Permissions.Tenants.UpgradeSubscription"));
    options.AddPolicy("Permissions.Tenants.ViewTheme", p => p.RequireClaim("permission", "Permissions.Tenants.ViewTheme"));
    options.AddPolicy("Permissions.Tenants.UpdateTheme", p => p.RequireClaim("permission", "Permissions.Tenants.UpdateTheme"));
    options.AddPolicy("Permissions.Billing.View", p => p.RequireClaim("permission", "Permissions.Billing.View"));
    options.AddPolicy("Permissions.Billing.Manage", p => p.RequireClaim("permission", "Permissions.Billing.Manage"));
    options.AddPolicy("Permissions.Catalog.View", p => p.RequireClaim("permission", "Permissions.Catalog.View"));
    options.AddPolicy("Permissions.Catalog.Create", p => p.RequireClaim("permission", "Permissions.Catalog.Create"));
    options.AddPolicy("Permissions.Catalog.Update", p => p.RequireClaim("permission", "Permissions.Catalog.Update"));
    options.AddPolicy("Permissions.Catalog.Delete", p => p.RequireClaim("permission", "Permissions.Catalog.Delete"));
    options.AddPolicy("Permissions.Webhooks.View", p => p.RequireClaim("permission", "Permissions.Webhooks.View"));
    options.AddPolicy("Permissions.Webhooks.Create", p => p.RequireClaim("permission", "Permissions.Webhooks.Create"));
    options.AddPolicy("Permissions.Webhooks.Delete", p => p.RequireClaim("permission", "Permissions.Webhooks.Delete"));
    options.AddPolicy("Permissions.Webhooks.Test", p => p.RequireClaim("permission", "Permissions.Webhooks.Test"));
    options.AddPolicy("Permissions.AuditTrails.View", p => p.RequireClaim("permission", "Permissions.AuditTrails.View"));
    options.AddPolicy("Permissions.AuditTrails.ViewCrossTenant", p => p.RequireClaim("permission", "Permissions.AuditTrails.ViewCrossTenant"));
    options.AddPolicy("Permissions.Impersonation.View", p => p.RequireClaim("permission", "Permissions.Impersonation.View"));
    options.AddPolicy("Permissions.Impersonation.Revoke", p => p.RequireClaim("permission", "Permissions.Impersonation.Revoke"));
    options.AddPolicy("Permissions.Chat.Channels.View", p => p.RequireClaim("permission", "Permissions.Chat.Channels.View"));
    options.AddPolicy("Permissions.Chat.Channels.Create", p => p.RequireClaim("permission", "Permissions.Chat.Channels.Create"));
    options.AddPolicy("Permissions.Chat.Channels.ManageAll", p => p.RequireClaim("permission", "Permissions.Chat.Channels.ManageAll"));
    options.AddPolicy("Permissions.Chat.Messages.Send", p => p.RequireClaim("permission", "Permissions.Chat.Messages.Send"));
    options.AddPolicy("Permissions.Chat.Messages.EditOwn", p => p.RequireClaim("permission", "Permissions.Chat.Messages.EditOwn"));
    options.AddPolicy("Permissions.Chat.Messages.DeleteOwn", p => p.RequireClaim("permission", "Permissions.Chat.Messages.DeleteOwn"));
    options.AddPolicy("Permissions.Chat.Messages.DeleteAny", p => p.RequireClaim("permission", "Permissions.Chat.Messages.DeleteAny"));
    options.AddPolicy("Permissions.Files.Upload", p => p.RequireClaim("permission", "Permissions.Files.Upload"));
    options.AddPolicy("Permissions.Files.View", p => p.RequireClaim("permission", "Permissions.Files.View"));
    options.AddPolicy("Permissions.Files.ViewTrash", p => p.RequireClaim("permission", "Permissions.Files.ViewTrash"));
    options.AddPolicy("Permissions.Files.DeleteOwn", p => p.RequireClaim("permission", "Permissions.Files.DeleteOwn"));
    options.AddPolicy("Permissions.Files.DeleteAny", p => p.RequireClaim("permission", "Permissions.Files.DeleteAny"));
    options.AddPolicy("Permissions.Files.Restore", p => p.RequireClaim("permission", "Permissions.Files.Restore"));
});

// HTTP client with auth handler (for all authenticated API calls)
builder.Services.AddTransient<AuthDelegatingHandler>();
builder.Services.AddTransient<TerminalErrorHandler>();
builder.Services.AddTransient<RetryAfterHandler>();
builder.Services.AddHttpClient("FSH.Api", (sp, client) =>
{
    var config = sp.GetRequiredService<IRuntimeConfigService>();
    client.BaseAddress = RuntimeConfigService.ResolveApiBase(baseAddress, config.ApiBaseUrl);
})
// First registered = outermost: TerminalErrorHandler must see the FINAL response
// (including AuthDelegatingHandler's refresh outcome and thrown ApiRequestException).
.AddHttpMessageHandler<TerminalErrorHandler>()
.AddHttpMessageHandler<AuthDelegatingHandler>()
// Innermost: RetryAfterHandler retries a single 429 (idempotent verbs only) after
// honoring Retry-After, so the outer handlers see the retried response.
.AddHttpMessageHandler<RetryAfterHandler>();
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("FSH.Api"));

// MudBlazor
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PreventDuplicates = true;
    config.SnackbarConfiguration.ShowCloseIcon = true;
});

// Theme (React parity: dashboard key is fsh.theme, default mode is System)
builder.Services.AddSingleton(sp => new FshThemeService(sp.GetRequiredService<IJSRuntime>(), "fsh.theme", ThemeMode.System));

// Connectivity (React parity: offline banner) — singleton so the banner and any
// page share one browser event subscription.
builder.Services.AddSingleton<INetworkStatus>(sp => new FshNetworkStatus(sp.GetRequiredService<IJSRuntime>()));

// Realtime + SSE
builder.Services.AddScoped<IHubConnectionService, HubConnectionService>();
builder.Services.AddScoped<ISseService, SseService>();

var host = builder.Build();

// Load runtime config before the app starts (no race on first request).
await host.Services.GetRequiredService<IRuntimeConfigService>().LoadAsync();

// Restore the persisted theme before the first render (avoids a light->dark flash).
await host.Services.GetRequiredService<FshThemeService>().InitializeAsync();

await host.RunAsync();

