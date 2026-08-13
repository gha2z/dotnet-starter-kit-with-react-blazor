using FSH.Hybrid.Pages;

namespace FSH.Hybrid;

public sealed partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(AboutPage), typeof(AboutPage));
    }
}
