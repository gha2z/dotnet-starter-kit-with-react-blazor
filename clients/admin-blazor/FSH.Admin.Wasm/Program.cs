using FSH.Admin.Wasm;
using FSH.Admin.Wasm.Auth;
using FSH.BlazorShared.Auth;
using FSH.BlazorShared.Infrastructure;
using FSH.BlazorShared.Realtime;
using FSH.BlazorShared.Services;
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
    new AdminTokenStore(sp.GetRequiredService<IJSRuntime>()));
builder.Services.AddScoped<AuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<AuthStateProvider>());
builder.Services.AddScoped<IPermissionsProvider, PermissionsProvider>();

// Auth HTTP client (no auth handler — used for login/refresh only)
builder.Services.AddHttpClient("FSH.Auth", (sp, client) =>
{
    var config = sp.GetRequiredService<IRuntimeConfigService>();
    client.BaseAddress = RuntimeConfigService.ResolveApiBase(baseAddress, config.ApiBaseUrl);
});

// Auth service
builder.Services.AddScoped<IAuthService, AuthService>();

// Health probes are anonymous (no auth handler) so load balancers can scrape them.
builder.Services.AddHttpClient("FSH.Health", (sp, client) =>
{
    var config = sp.GetRequiredService<IRuntimeConfigService>();
    client.BaseAddress = RuntimeConfigService.ResolveApiBase(baseAddress, config.ApiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(8);
});
builder.Services.AddScoped<IHealthService>(sp =>
    new HealthService(sp.GetRequiredService<IHttpClientFactory>().CreateClient("FSH.Health")));

// Authorization policies
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("Permissions.Users.View", p => p.RequireClaim("permission", "Permissions.Users.View"));
    options.AddPolicy("Permissions.Users.Create", p => p.RequireClaim("permission", "Permissions.Users.Create"));
    options.AddPolicy("Permissions.Users.Update", p => p.RequireClaim("permission", "Permissions.Users.Update"));
    options.AddPolicy("Permissions.Users.Delete", p => p.RequireClaim("permission", "Permissions.Users.Delete"));
    options.AddPolicy("Permissions.Roles.View", p => p.RequireClaim("permission", "Permissions.Roles.View"));
    options.AddPolicy("Permissions.Roles.Create", p => p.RequireClaim("permission", "Permissions.Roles.Create"));
    options.AddPolicy("Permissions.Roles.Update", p => p.RequireClaim("permission", "Permissions.Roles.Update"));
    options.AddPolicy("Permissions.Roles.Delete", p => p.RequireClaim("permission", "Permissions.Roles.Delete"));
    options.AddPolicy("Permissions.Tenants.View", p => p.RequireClaim("permission", "Permissions.Tenants.View"));
    options.AddPolicy("Permissions.Tenants.Create", p => p.RequireClaim("permission", "Permissions.Tenants.Create"));
    options.AddPolicy("Permissions.Tenants.Update", p => p.RequireClaim("permission", "Permissions.Tenants.Update"));
    options.AddPolicy("Permissions.Tenants.UpgradeSubscription", p => p.RequireClaim("permission", "Permissions.Tenants.UpgradeSubscription"));
    options.AddPolicy("Permissions.Tenants.ViewTheme", p => p.RequireClaim("permission", "Permissions.Tenants.ViewTheme"));
    options.AddPolicy("Permissions.Tenants.UpdateTheme", p => p.RequireClaim("permission", "Permissions.Tenants.UpdateTheme"));
    options.AddPolicy("Permissions.Billing.View", p => p.RequireClaim("permission", "Permissions.Billing.View"));
    options.AddPolicy("Permissions.Billing.Manage", p => p.RequireClaim("permission", "Permissions.Billing.Manage"));
    options.AddPolicy("Permissions.Webhooks.View", p => p.RequireClaim("permission", "Permissions.Webhooks.View"));
    options.AddPolicy("Permissions.Webhooks.Create", p => p.RequireClaim("permission", "Permissions.Webhooks.Create"));
    options.AddPolicy("Permissions.Webhooks.Delete", p => p.RequireClaim("permission", "Permissions.Webhooks.Delete"));
    options.AddPolicy("Permissions.Webhooks.Test", p => p.RequireClaim("permission", "Permissions.Webhooks.Test"));
    options.AddPolicy("Permissions.AuditTrails.View", p => p.RequireClaim("permission", "Permissions.AuditTrails.View"));
    options.AddPolicy("Permissions.AuditTrails.ViewCrossTenant", p => p.RequireClaim("permission", "Permissions.AuditTrails.ViewCrossTenant"));
    options.AddPolicy("Permissions.Users.Impersonate", p => p.RequireClaim("permission", "Permissions.Users.Impersonate"));
    options.AddPolicy("Permissions.Impersonation.View", p => p.RequireClaim("permission", "Permissions.Impersonation.View"));
    options.AddPolicy("Permissions.Impersonation.Revoke", p => p.RequireClaim("permission", "Permissions.Impersonation.Revoke"));
});

// HTTP client with auth handler (for all authenticated API calls)
builder.Services.AddTransient<AuthDelegatingHandler>();
builder.Services.AddTransient<RetryAfterHandler>();
builder.Services.AddHttpClient("FSH.Api", (sp, client) =>
{
    var config = sp.GetRequiredService<IRuntimeConfigService>();
    client.BaseAddress = RuntimeConfigService.ResolveApiBase(baseAddress, config.ApiBaseUrl);
})
.AddHttpMessageHandler<AuthDelegatingHandler>()
// Innermost: RetryAfterHandler retries a single 429 (idempotent verbs only) after
// honoring Retry-After, so the auth handler sees the retried response.
.AddHttpMessageHandler<RetryAfterHandler>();
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("FSH.Api"));

// MudBlazor
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = true;
    config.SnackbarConfiguration.ShowCloseIcon = true;
});

// Theme (dark default, persisted to localStorage - React parity)
builder.Services.AddSingleton(sp => new FshThemeService(sp.GetRequiredService<IJSRuntime>(), "fsh.admin.theme"));

// Connectivity (React parity: offline banner) — singleton so the banner and any
// page share one browser event subscription.
builder.Services.AddSingleton<INetworkStatus>(sp => new FshNetworkStatus(sp.GetRequiredService<IJSRuntime>()));

// API services (all use the scoped "FSH.Api" HttpClient with auth handler)
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IImpersonationService, ImpersonationService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<ITenantThemeService, TenantThemeService>();
builder.Services.AddScoped<ITwoFactorService, TwoFactorService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IWebhookService, WebhookService>();

// Realtime
builder.Services.AddScoped<IHubConnectionService, HubConnectionService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

var host = builder.Build();

// Load runtime config before the app starts (no race on first request).
await host.Services.GetRequiredService<IRuntimeConfigService>().LoadAsync();

// Restore the persisted theme before the first render (avoids a light->dark flash).
await host.Services.GetRequiredService<FshThemeService>().InitializeAsync();

// Hydrate permissions before the first render so the initial auth state already
// carries permission claims (React parity: the JWT only carries roles - permissions
// are resolved server-side per role). Without this, policy-gated routes fail and
// bounce to /login. Only when a token exists - while signed out there is nothing
// to hydrate and the request would 401.
if (!string.IsNullOrEmpty(await host.Services.GetRequiredService<ITokenStore>().GetAccessTokenAsync()))
{
    await host.Services.GetRequiredService<IPermissionsProvider>().GetPermissionsAsync();
}

await host.RunAsync();
