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
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var baseAddress = builder.HostEnvironment.BaseAddress;

// Runtime config (load /config.json)
builder.Services.AddScoped<IRuntimeConfigService, RuntimeConfigService>();
builder.Services.AddScoped(sp =>
{
    var config = sp.GetRequiredService<IRuntimeConfigService>();
    _ = config.LoadAsync();
    return config;
});

// Auth
builder.Services.AddSingleton<ITokenStore>(sp =>
    new DashboardTokenStore(sp.GetRequiredService<IJSRuntime>()));
builder.Services.AddScoped<AuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<AuthStateProvider>());
builder.Services.AddScoped<IPermissionsProvider, PermissionsProvider>();

// Auth HTTP client (no auth handler — used for login/refresh only)
builder.Services.AddHttpClient("FSH.Auth", client =>
    client.BaseAddress = new Uri(baseAddress));

// Auth service
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddAuthorizationCore();

// HTTP client with auth handler (for all authenticated API calls)
builder.Services.AddTransient<AuthDelegatingHandler>();
builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<AuthDelegatingHandler>();
    return new HttpClient(handler) { BaseAddress = new Uri(baseAddress) };
});

// MudBlazor
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PreventDuplicates = true;
    config.SnackbarConfiguration.ShowCloseIcon = true;
});

// Realtime + SSE
builder.Services.AddScoped<IHubConnectionService, HubConnectionService>();
builder.Services.AddScoped<ISseService, SseService>();

await builder.Build().RunAsync();
