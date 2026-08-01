using CommunityToolkit.Maui;
using FSH.BlazorShared.Auth;
using FSH.BlazorShared.Infrastructure;
using FSH.BlazorShared.Realtime;
using FSH.Hybrid.Services;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;

namespace FSH.Hybrid;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Platform auth
        builder.Services.AddSingleton<ITokenStore, MauiTokenStore>();

        // Runtime config (no config.json in the hybrid shell — defaults to same-origin)
        builder.Services.AddSingleton<IRuntimeConfigService>(sp =>
            new RuntimeConfigService(new HttpClient(), sp.GetRequiredService<ILogger<RuntimeConfigService>>()));

        // HTTP
        builder.Services.AddTransient<AuthDelegatingHandler>();
        builder.Services.AddScoped(sp =>
        {
            var handler = sp.GetRequiredService<AuthDelegatingHandler>();
            handler.InnerHandler = new SocketsHttpHandler();
            return new HttpClient(handler);
        });

        // Blazor WebView
        builder.Services.AddMudServices();
        builder.Services.AddScoped<IHubConnectionService, HubConnectionService>();
        builder.Services.AddBlazorWebView();

#if DEBUG
        builder.Logging.AddDebug();
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        return builder.Build();
    }
}
