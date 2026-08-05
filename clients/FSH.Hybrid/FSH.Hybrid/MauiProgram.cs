using CommunityToolkit.Maui;
using FSH.BlazorShared.Auth;
using FSH.BlazorShared.Infrastructure;
using FSH.BlazorShared.Realtime;
using FSH.BlazorShared.Services;
using FSH.BlazorShared.Sse;
using FSH.BlazorShared.Theming;
using FSH.Hybrid.Auth;
using FSH.Hybrid.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor.Services;

namespace FSH.Hybrid;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit();

        // Native platform services (override the WASM browser-based implementations)
        builder.Services.AddSingleton<ITokenStore, MauiTokenStore>();
        builder.Services.AddSingleton<IBiometricService, BiometricService>();
        builder.Services.AddSingleton<IRuntimeConfigService, HybridRuntimeConfigService>();
        builder.Services.AddSingleton<IConnectivityService, ConnectivityService>();
        builder.Services.AddSingleton<IOfflineQueueService>(_ => new OfflineQueueService());
        builder.Services.AddSingleton<IOfflineQueueProcessor, OfflineQueueProcessor>();
        builder.Services.AddSingleton<IDeepLinkService, DeepLinkService>();
        builder.Services.AddSingleton<IMediaPickerService, MediaPickerService>();
        builder.Services.AddSingleton<IPushNotificationService, PushNotificationService>();

        // Auth
        builder.Services.AddScoped<AuthStateProvider>();
        builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<AuthStateProvider>());
        builder.Services.AddScoped<IPermissionsProvider, PermissionsProvider>();
        builder.Services.AddScoped<MauiAuthStateProvider>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<MauiAuthService>();

        // HTTP
        builder.Services.AddTransient<AuthDelegatingHandler>();
        builder.Services.AddTransient<OfflineDelegatingHandler>();
        builder.Services.AddHttpClient("FSH.Auth", (sp, client) =>
        {
            client.BaseAddress = new Uri(sp.GetRequiredService<IRuntimeConfigService>().ApiBaseUrl);
        });
        builder.Services.AddHttpClient("FSH.Api", (sp, client) =>
        {
            client.BaseAddress = new Uri(sp.GetRequiredService<IRuntimeConfigService>().ApiBaseUrl);
        })
        .AddHttpMessageHandler<OfflineDelegatingHandler>()
        .AddHttpMessageHandler<AuthDelegatingHandler>();
        builder.Services.AddHttpClient("FSH.Storage");
        builder.Services.AddScoped(sp =>
            sp.GetRequiredService<IHttpClientFactory>().CreateClient("FSH.Api"));

        // Tenant-scoped data services (shared RCL)
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
        builder.Services.AddScoped<IHybridFileUploadService, HybridFileUploadService>();

        builder.Services.AddAuthorizationCore(FshPolicies.Register);

        // MudBlazor + theme (React parity: dashboard key fsh.theme, default System)
        builder.Services.AddMudServices(config =>
        {
            config.SnackbarConfiguration.PreventDuplicates = true;
            config.SnackbarConfiguration.ShowCloseIcon = true;
        });
        builder.Services.AddSingleton(sp =>
            new FshThemeService(sp.GetRequiredService<IJSRuntime>(), "fsh.theme", ThemeMode.System));

        // Realtime
        builder.Services.AddScoped<IHubConnectionService, HubConnectionService>();
        builder.Services.AddScoped<ISseService, SseService>();

        // Blazor WebView
        builder.Services.AddBlazorWebView();

#if DEBUG
        builder.Logging.AddDebug();
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        return builder.Build();
    }
}
