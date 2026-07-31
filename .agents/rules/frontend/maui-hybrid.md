# MAUI Blazor Hybrid — conventions

Read `blazor-shared.md` first — this file covers MAUI-only divergences.

## Stack

.NET 10 MAUI · BlazorWebView · BlazorShared RCL · MudBlazor · SQLite (offline queue) · CommunityToolkit.Mvvm · CommunityToolkit.Maui.

## Project structure

```
clients/FSH.Hybrid/
├── FSH.Hybrid/
│   ├── Platforms/
│   │   ├── Android/                    # MainActivity, MainApplication, AndroidManifest
│   │   ├── iOS/                        # AppDelegate, Info.plist
│   │   ├── MacCatalyst/                # AppDelegate, Info.plist
│   │   └── Windows/                    # App.xaml, Package.appxmanifest
│   ├── Services/                       # Native service implementations
│   │   ├── MauiTokenStore.cs           # SecureStorage-based token store
│   │   ├── PushNotificationService.cs  # Firebase (Android) / APNs (iOS)
│   │   ├── BiometricService.cs         # Biometric auth via platform APIs
│   │   ├── MediaPickerService.cs       # Camera/gallery via MAUI Essentials
│   │   ├── ConnectivityService.cs      # Network connectivity monitoring
│   │   ├── OfflineQueueService.cs      # SQLite-backed request queue
│   │   └── DeepLinkService.cs          # URI scheme / universal link handling
│   ├── Pages/                          # MAUI Shell pages (minimal)
│   │   ├── MainPage.xaml               # Hosts BlazorWebView
│   │   ├── SettingsPage.xaml
│   │   └── AboutPage.xaml
│   ├── App.xaml / App.xaml.cs
│   ├── AppShell.xaml / AppShell.xaml.cs
│   ├── MauiProgram.cs
│   ├── Resources/
│   │   ├── AppIcon/
│   │   ├── Splash/
│   │   ├── Fonts/
│   │   └── Raw/
│   └── FSH.Hybrid.csproj
└── FSH.Hybrid.Tests/
```

## Auth difference — SecureStorage

- `ITokenStore` implementation uses `Microsoft.Maui.Storage.SecureStorage` instead of localStorage (JS interop).
- Biometric unlock: `BiometricService` wraps platform biometric APIs (Android `BiometricManager` / iOS `LAContext`).
- If biometric is enabled, the access token is encrypted at rest and requires biometric verification to decrypt.

## Native service registration in MauiProgram.cs

```csharp
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    builder.UseMauiApp<App>()
           .UseMauiCommunityToolkit()
           .ConfigureFonts(fonts => { fonts.AddFont("Geist-Regular.ttf", "Geist"); });

    // Override WASM services with MAUI native implementations
    builder.Services.AddSingleton<ITokenStore, MauiTokenStore>();
    builder.Services.AddSingleton<IBiometricService, BiometricService>();
    builder.Services.AddSingleton<IPushNotificationService, PushNotificationService>();
    builder.Services.AddSingleton<IConnectivityService, ConnectivityService>();
    builder.Services.AddSingleton<IOfflineQueueService, OfflineQueueService>();
    builder.Services.AddTransient<MediaPickerService>();

    // Shared services from BlazorShared
    builder.Services.AddBlazorSharedServices();

    // Blazor WebView
    builder.Services.AddBlazorWebView();
#if DEBUG
    builder.Services.AddBlazorWebViewDeveloperTools();
#endif

    return builder.Build();
}
```

## Offline queue

- `OfflineQueueService` intercepts failed HTTP calls (via a separate `OfflineDelegatingHandler`), serializes the request to SQLite, and replays when connectivity is restored.
- Queue is processed FIFO on `ConnectivityChanged += (s, e) => { if (e.NetworkAccess == NetworkAccess.Internet) ProcessQueue(); }`.

## Push notifications

- Android: Firebase Cloud Messaging (FCM) via `Plugin.Firebase.CloudMessaging`.
- iOS: APNs via `UserNotifications` framework.
- Notification tap opens a deep link to the relevant page (e.g., `fsh://tickets/{id}`).

## Deep linking

- Android: Intent filter for `fsh://` scheme in `AndroidManifest.xml`.
- iOS: `CFBundleURLSchemes` in `Info.plist` + `Universal Links` via `apple-app-site-association`.
- `DeepLinkService` parses incoming URIs and navigates via `Shell.Current.GoToAsync()`.

## Build & deploy

```bash
# Android
dotnet build clients/FSH.Hybrid/FSH.Hybrid -t:Run -f net10.0-android

# iOS (requires Mac build host)
dotnet build clients/FSH.Hybrid/FSH.Hybrid -t:Run -f net10.0-ios

# Windows
dotnet build clients/FSH.Hybrid/FSH.Hybrid -t:Run -f net10.0-windows10.0.19041.0

# Mac Catalyst
dotnet build clients/FSH.Hybrid/FSH.Hybrid -t:Run -f net10.0-maccatalyst
```

## Key MAUI NuGet packages

```xml
<PackageVersion Include="CommunityToolkit.Maui" Version="10.0.0" />
<PackageVersion Include="CommunityToolkit.Mvvm" Version="8.4.0" />
<PackageVersion Include="Plugin.Firebase.CloudMessaging" Version="3.1.0" />
<PackageVersion Include="sqlite-net-pcl" Version="1.9.172" />
```

## Hybrid-specific patterns

- **BlazorWebView hosting**: `MainPage.xaml` contains `<blazor:BlazorWebView HostPage="wwwroot/index.html" />` referencing the Blazor WASM app shell.
- **JS interop bridge**: MAUI registers .NET methods callable from JS (e.g., `getDeviceToken`, `getBatteryLevel`).
- **App lifecycle**: `OnSleep`/`OnResume` disconnect/reconnect SignalR, flush offline queue.
- **Safe area**: MAUI `SafeArea` layout accounts for notches/status bars on mobile.
- **Splash screen**: Configured in `.csproj` with `MauiSplashScreen` properties.
