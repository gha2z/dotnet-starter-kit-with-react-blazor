using Microsoft.Extensions.Logging;

namespace FSH.Hybrid.Services;

public interface IPushNotificationService
{
    /// <summary>Requests the notification permission and returns the FCM/APNs device token (or null when unavailable).</summary>
    Task<string?> RegisterAsync(CancellationToken ct = default);
}

/// <summary>
/// Phase 5.4 — push notifications. BLOCKED on external setup: a Firebase project +
/// google-services.json and a server-side sender. Everything is compile-gated behind
/// <c>FSH_FIREBASE</c> so the app keeps building without that configuration.
/// Follow <c>opencode/addBlazorFrontends/Phase-05-MAUI-Hybrid/push-setup.md</c> to enable.
/// </summary>
public sealed class PushNotificationService(ILogger<PushNotificationService> logger) : IPushNotificationService
{
    public Task<string?> RegisterAsync(CancellationToken ct = default)
    {
#if ANDROID && FSH_FIREBASE
        return RegisterAndroidAsync();
#else
        logger.LogInformation("Push notifications not configured (FSH_FIREBASE unset or non-Android)");
        return Task.FromResult<string?>(null);
#endif
    }

#if ANDROID && FSH_FIREBASE
    private static async Task<string?> RegisterAndroidAsync()
    {
        var permissionStatus = await Microsoft.Maui.ApplicationModel.Permissions
            .RequestAsync<Permissions.PostNotifications>();
        if (permissionStatus != PermissionStatus.Granted)
        {
            return null;
        }

        // Plugin.Firebase.CloudMessaging surfaces the token via
        // CrossFirebaseCloudMessaging.Current.Token after the plugin's
        // FirebaseMessagingService has registered. Add google-services.json to the
        // Android project and define FSH_FIREBASE to compile this path.
        await Plugin.Firebase.CloudMessaging.CrossFirebaseCloudMessaging.Current
            .CheckIfValidAsync();
        return Plugin.Firebase.CloudMessaging.CrossFirebaseCloudMessaging.Current.Token;
    }
#endif
}
