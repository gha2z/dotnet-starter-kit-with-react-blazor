using FSH.Hybrid.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Hybrid;

public sealed partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());

#if WINDOWS
        window.HandlerChanged += OnWindowHandlerChanged;
#endif
        window.Created += (_, _) =>
        {
            // Start the offline queue: subscribe to connectivity + flush anything pending.
            if (Current?.Handler?.MauiContext?.Services.GetService<IOfflineQueueProcessor>() is { } processor)
            {
                _ = processor.ProcessQueueAsync();
            }
        };

        return window;
    }

    protected override void OnAppLinkRequestReceived(Uri uri)
    {
        base.OnAppLinkRequestReceived(uri);
        HandleAppLink(uri);
    }

    public void HandleAppLink(Uri uri)
    {
        var target = Current?.Handler?.MauiContext?.Services.GetService<IDeepLinkService>()?.Parse(uri);
        if (target is null)
        {
            return;
        }

        if (target.BlazorPath is not null)
        {
            HybridNavigationBridge.RaiseBlazorPath(target.BlazorPath);
        }

        _ = Shell.Current.GoToAsync($"//{target.ShellRoute}");
    }

    protected override void OnSleep()
    {
        if (Current?.Handler?.MauiContext?.Services.GetService<MauiAuthStateProvider>() is { } auth)
        {
            auth.Lock();
        }

        base.OnSleep();
    }

    protected override void OnResume()
    {
        if (Current?.Handler?.MauiContext?.Services.GetService<MauiAuthStateProvider>() is { } auth)
        {
            _ = auth.OnAppResumedAsync();
        }

        if (Current?.Handler?.MauiContext?.Services.GetService<IOfflineQueueProcessor>() is { } processor)
        {
            _ = processor.ProcessQueueAsync();
        }

        base.OnResume();
    }

#if WINDOWS
    private static void OnWindowHandlerChanged(object? sender, EventArgs e)
    {
        if (sender is Window { Handler.PlatformView: Microsoft.UI.Xaml.Window win })
        {
            win.SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
        }
    }
#endif
}
