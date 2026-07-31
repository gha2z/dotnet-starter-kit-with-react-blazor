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

        // HTTP
        builder.Services.AddTransient<AuthDelegatingHandler>();
        builder.Services.AddScoped(sp =>
        {
            var handler = sp.GetRequiredService<AuthDelegatingHandler>();
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
