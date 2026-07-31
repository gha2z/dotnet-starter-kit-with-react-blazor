using FSH.Admin.Wasm;
using FSH.Admin.Wasm.Auth;
using FSH.BlazorShared.Auth;
using FSH.BlazorShared.Infrastructure;
using FSH.BlazorShared.Realtime;
using FSH.BlazorShared.Services;
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
    _ = config.LoadAsync(); // fire-and-forget at startup
    return config;
});

// Auth
builder.Services.AddSingleton<ITokenStore>(sp =>
    new AdminTokenStore(sp.GetRequiredService<IJSRuntime>()));
builder.Services.AddScoped<AuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<AuthStateProvider>());
builder.Services.AddScoped<IPermissionsProvider, PermissionsProvider>();

// Auth HTTP client (no auth handler — used for login/refresh only)
builder.Services.AddHttpClient("FSH.Auth", client =>
    client.BaseAddress = new Uri(baseAddress));

// Auth service
builder.Services.AddScoped<IAuthService, AuthService>();

// Authorization policies
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("Permissions.Users.View", p => p.RequireClaim("permission", "Permissions.Users.View"));
    options.AddPolicy("Permissions.Users.Create", p => p.RequireClaim("permission", "Permissions.Users.Create"));
    options.AddPolicy("Permissions.Users.Edit", p => p.RequireClaim("permission", "Permissions.Users.Edit"));
    options.AddPolicy("Permissions.Users.Delete", p => p.RequireClaim("permission", "Permissions.Users.Delete"));
    options.AddPolicy("Permissions.Roles.View", p => p.RequireClaim("permission", "Permissions.Roles.View"));
    options.AddPolicy("Permissions.Roles.Create", p => p.RequireClaim("permission", "Permissions.Roles.Create"));
    options.AddPolicy("Permissions.Roles.Edit", p => p.RequireClaim("permission", "Permissions.Roles.Edit"));
    options.AddPolicy("Permissions.Roles.Delete", p => p.RequireClaim("permission", "Permissions.Roles.Delete"));
    options.AddPolicy("Permissions.Tenants.View", p => p.RequireClaim("permission", "Permissions.Tenants.View"));
    options.AddPolicy("Permissions.Tenants.Create", p => p.RequireClaim("permission", "Permissions.Tenants.Create"));
    options.AddPolicy("Permissions.Tenants.Edit", p => p.RequireClaim("permission", "Permissions.Tenants.Edit"));
});

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
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = true;
    config.SnackbarConfiguration.ShowCloseIcon = true;
});

// Realtime
builder.Services.AddScoped<IHubConnectionService, HubConnectionService>();

await builder.Build().RunAsync();
