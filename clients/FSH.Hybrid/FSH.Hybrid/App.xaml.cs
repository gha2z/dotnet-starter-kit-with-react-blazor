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

        return window;
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
