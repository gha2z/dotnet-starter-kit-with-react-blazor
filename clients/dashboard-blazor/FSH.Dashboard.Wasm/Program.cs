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

builder.Services.AddAuthorizationCore();

// HTTP client with auth handler (for all authenticated API calls)
builder.Services.AddTransient<AuthDelegatingHandler>();
builder.Services.AddHttpClient("FSH.Api", (sp, client) =>
{
    var config = sp.GetRequiredService<IRuntimeConfigService>();
    client.BaseAddress = RuntimeConfigService.ResolveApiBase(baseAddress, config.ApiBaseUrl);
})
.AddHttpMessageHandler<AuthDelegatingHandler>();
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

// Realtime + SSE
builder.Services.AddScoped<IHubConnectionService, HubConnectionService>();
builder.Services.AddScoped<ISseService, SseService>();

var host = builder.Build();

// Load runtime config before the app starts (no race on first request).
await host.Services.GetRequiredService<IRuntimeConfigService>().LoadAsync();

// Restore the persisted theme before the first render (avoids a light->dark flash).
await host.Services.GetRequiredService<FshThemeService>().InitializeAsync();

await host.RunAsync();

