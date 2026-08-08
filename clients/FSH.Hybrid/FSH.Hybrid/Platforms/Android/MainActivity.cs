using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace FSH.Hybrid;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter(
    [Intent.ActionView],
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataScheme = "fsh")]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);

        if (intent?.Data?.ToString() is { } link && Microsoft.Maui.Controls.Application.Current is App app)
        {
            app.HandleAppLink(new Uri(link));
        }
    }
}
