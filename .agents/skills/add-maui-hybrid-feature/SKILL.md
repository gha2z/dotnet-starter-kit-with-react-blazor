---
name: add-maui-hybrid-feature
description: Add a native MAUI feature (push notifications, biometric auth, camera, offline queue, deep linking) to the MAUI Blazor Hybrid app. See .agents/rules/frontend/maui-hybrid.md.
argument-hint: "[push|biometric|camera|offline|deeplink|connectivity]"
---

# Add MAUI Hybrid Feature

Read `.agents/rules/frontend/maui-hybrid.md` first.

## Step 1 — Create the platform service interface

Define the interface in `BlazorShared/Services/` (or MAUI-specific location for truly native-only services):

```csharp
public interface IBiometricService
{
    Task<bool> AuthenticateAsync(string reason);
    Task<bool> IsAvailableAsync();
}
```

## Step 2 — Platform implementation

```csharp
// FSH.Hybrid/Services/BiometricService.cs
public sealed class BiometricService : IBiometricService
{
    public async Task<bool> AuthenticateAsync(string reason)
    {
        try
        {
            var result = await Platform.Current.AuthenticateAsync(
                new AuthenticationRequest { Reason = reason });
            return result.Authenticated;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        var level = await Platform.Current.GetAuthenticationAvailabilityAsync();
        return level switch
        {
            BiometricAvailability.Available => true,
            _ => false
        };
    }
}
```

## Step 3 — Register in MauiProgram.cs

```csharp
builder.Services.AddSingleton<IBiometricService, BiometricService>();
```

## Step 4 — Use from Blazor

```razor
@inject IBiometricService Biometric

<MudButton OnClick="AuthenticateAsync" Color="Color.Primary" StartIcon="@Icons.Material.Filled.Fingerprint">
    @if (_isAvailable)
    {
        <text>Authenticate with Biometrics</text>
    }
    else
    {
        <text>Biometrics not available</text>
    }
</MudButton>

@code {
    private bool _isAvailable;

    protected override async Task OnInitializedAsync()
    {
        _isAvailable = await Biometric.IsAvailableAsync();
    }

    private async Task AuthenticateAsync()
    {
        var result = await Biometric.AuthenticateAsync("Unlock your account");
        if (result) { /* proceed */ }
    }
}
```

## Feature-specific guidance

| Feature | Interface | Implementation | Platform Details |
|---|---|---|---|
| Push notifications | `IPushNotificationService` | Firebase (Android) / APNs (iOS) | Register in `MainActivity` (Android) / `AppDelegate` (iOS) |
| Camera/Media | `IMediaPickerService` | `MediaPicker.Default.CapturePhotoAsync()` | Android `CAMERA` permission, iOS `NSCameraUsageDescription` |
| Offline queue | `IOfflineQueueService` | SQLite + `Connectivity.Current.NetworkAccess` | Serialize failed requests to `http-queue.db` |
| Deep linking | `IDeepLinkService` | URI scheme `fsh://` + universal links | Android `IntentFilter`, iOS `CFBundleURLSchemes` |

## Validation

- [ ] Feature works on Android emulator
- [ ] Feature works on iOS simulator
- [ ] Feature degrades gracefully when unavailable (hardware/API level)
- [ ] MAUI-compile-only code guarded with `#if ANDROID` / `#if IOS`
- [ ] No crash on platforms where feature is not supported
