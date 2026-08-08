namespace FSH.Hybrid.Services;

public sealed record DeepLinkTarget(string ShellRoute, string? BlazorPath);

public interface IDeepLinkService
{
    DeepLinkTarget? Parse(Uri uri);
}

/// <summary>
/// Maps `fsh://` URIs to shell routes and optionally a Blazor route inside the webview.
/// fsh://home|settings|about → shell pages; any other path (fsh://files, fsh://tickets/123)
/// opens the shell home and hands the path to the Blazor router via
/// <see cref="HybridNavigationBridge"/>.
/// </summary>
public sealed class DeepLinkService : IDeepLinkService
{
    public DeepLinkTarget? Parse(Uri uri)
    {
        if (!string.Equals(uri.Scheme, "fsh", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var target = (uri.Host + uri.AbsolutePath).Trim('/');
        if (target.Length == 0)
        {
            return new DeepLinkTarget("home", "/");
        }

        return target switch
        {
            "home" => new DeepLinkTarget("home", "/"),
            "settings" => new DeepLinkTarget("settings", null),
            "about" => new DeepLinkTarget("about", null),
            _ => new DeepLinkTarget("home", "/" + target),
        };
    }
}

/// <summary>
/// Carries a deep-linked Blazor route from the native shell into the Blazor router.
/// The root component (Main.razor) consumes <see cref="PendingPath"/> during init and
/// subscribes to <see cref="BlazorPathReceived"/> for links arriving while the app is running.
/// </summary>
public static class HybridNavigationBridge
{
    public static string? PendingPath { get; set; }

    public static event Action<string?>? BlazorPathReceived;

    public static void RaiseBlazorPath(string? path)
    {
        PendingPath = path;
        BlazorPathReceived?.Invoke(path);
    }
}
