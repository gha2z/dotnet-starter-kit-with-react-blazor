# MAUI Blazor Hybrid — conventions

Read `blazor-shared.md` first — this file covers MAUI-only divergences.

## Stack

.NET 10 MAUI · BlazorWebView (`Main.razor` root component) · BlazorShared RCL · MudBlazor 9.7.0 · sqlite-net-pcl (offline queue) · CommunityToolkit.Maui 13.0.0 · CommunityToolkit.Mvvm 8.4.2 · Xamarin.AndroidX.Biometric (android-only).

## Project structure

```
clients/FSH.Hybrid/
├── FSH.Hybrid/
│   ├── Platforms/
│   │   ├── Android/                    # MainActivity (fsh:// intent filter), MainApplication, AndroidManifest (POST_NOTIFICATIONS + INTERNET)
│   │   ├── iOS/                        # AppDelegate, Info.plist (fsh URL scheme)
│   │   ├── MacCatalyst/                # AppDelegate, Info.plist
│   │   └── Windows/                    # App.xaml, Package.appxmanifest
│   ├── Auth/FshPolicies.cs             # authorization policy registration
│   ├── Services/
│   │   ├── MauiTokenStore.cs           # SecureStorage-based ITokenStore
│   │   ├── MauiAuthStateProvider.cs    # biometric lock-on-sleep / unlock-on-resume
│   │   ├── MauiAuthService.cs          # auth orchestration (login/logout/impersonation)
│   │   ├── HybridRuntimeConfigService.cs  # IRuntimeConfigService (API base URL)
│   │   ├── BiometricService.cs         # AndroidX Biometric / LAContext wrappers
│   │   ├── ConnectivityService.cs      # network connectivity monitoring
│   │   ├── OfflineQueueService.cs      # SQLite-backed request queue (IAsyncDisposable)
│   │   ├── OfflineQueueProcessor.cs    # FIFO replay of queued requests
│   │   ├── OfflineDelegatingHandler.cs # queues mutating calls when offline
│   │   ├── MediaPickerService.cs       # camera/gallery + content-type mapping
│   │   ├── HybridFileUploadService.cs  # presigned upload flow (BlazorShared)
│   │   ├── DeepLinkService.cs          # fsh:// URI → shell route + Blazor path
│   │   └── PushNotificationService.cs  # compile-gated behind FSH_FIREBASE (see push-setup.md)
│   ├── Pages/                          # Blazor pages (rendered inside the webview)
│   │   ├── OverviewPage.razor, FilesPage.razor (media-upload proof)
│   │   └── Auth/LoginPage.razor
│   ├── Shared/MainLayout.razor, RedirectToLogin.razor
│   ├── Main.razor                      # Blazor root: navigates via HybridNavigationBridge.PendingPath
│   ├── MainPage.xaml                   # hosts BlazorWebView (Selector #app → Main)
│   ├── SettingsPage.xaml / AboutPage.xaml   # native Shell pages
│   ├── App.xaml / App.xaml.cs          # queue flush + deep links + biometric lock
│   ├── AppShell.xaml / AppShell.xaml.cs
│   ├── MauiProgram.cs
│   ├── Resources/ (AppIcon, Splash, Fonts, Raw/SharedStyles.xaml)
│   └── FSH.Hybrid.csproj
└── FSH.Hybrid.Tests/                   # xunit, net10.0-windows10.0.19041.0
```

Blazor pages live under `Pages/` inside the app project (not the RCL) when they exercise native services.

## Auth difference — SecureStorage + biometric gate

- `ITokenStore` → `MauiTokenStore` uses `Microsoft.Maui.Storage.SecureStorage` (no JS interop).
- `AuthStateProvider` comes from BlazorShared; `MauiAuthStateProvider` adds a biometric gate:
  `OnSleep()` → `Lock()`; `OnResume()` → if `IsBiometricLockEnabled` (Preferences flag) and a token exists, `BiometricService.AuthenticateAsync("Unlock FSH Hybrid", ...)`; failed unlock keeps the session locked. The token itself is NOT encrypted with a biometric key — biometrics gate session resume only.

## Native service registration in MauiProgram.cs

Real registrations (abridged):

```csharp
builder.Services.AddSingleton<ITokenStore, MauiTokenStore>();
builder.Services.AddSingleton<IBiometricService, BiometricService>();
builder.Services.AddSingleton<IRuntimeConfigService, HybridRuntimeConfigService>();
builder.Services.AddSingleton<IConnectivityService, ConnectivityService>();
builder.Services.AddSingleton<IOfflineQueueService>(_ => new OfflineQueueService());
builder.Services.AddSingleton<IOfflineQueueProcessor, OfflineQueueProcessor>();
builder.Services.AddSingleton<IDeepLinkService, DeepLinkService>();
builder.Services.AddSingleton<IMediaPickerService, MediaPickerService>();
builder.Services.AddSingleton<IPushNotificationService, PushNotificationService>();
// auth: AuthStateProvider (BlazorShared) + MauiAuthStateProvider + AuthService + MauiAuthService
builder.Services.AddAuthorizationCore(FshPolicies.Register);
builder.Services.AddMudServices(...);
builder.Services.AddSingleton(sp => new FshThemeService(sp.GetRequiredService<IJSRuntime>(), "fsh.theme", ThemeMode.System));
// realtime: HubConnectionService + SseService (BlazorShared)
builder.Services.AddBlazorWebView();
```

Named HttpClients (order matters):

```csharp
builder.Services.AddHttpClient("FSH.Auth", ...);          // NO handlers
builder.Services.AddHttpClient("FSH.Api", ...)
    .AddHttpMessageHandler<OfflineDelegatingHandler>()     // outermost
    .AddHttpMessageHandler<AuthDelegatingHandler>();
builder.Services.AddHttpClient("FSH.Storage");             // presigned PUT client
// default scoped client = FSH.Api (handlers included)
```

Tenant-scoped data services (`IBillingService`, `IFileService`, …) register `AddScoped` from BlazorShared — keep that pattern for new ones.

## Offline queue

- `OfflineDelegatingHandler` (outermost on `FSH.Api`) queues `POST/PUT/PATCH/DELETE` when offline (`OfflineException`) and passes GETs and the auth client through.
- `OfflineQueueService` (SQLite, sqlite-net-pcl, FIFO, `IAsyncDisposable` — the connection holds the DB file) + `OfflineQueueProcessor` replays FIFO with retry cap 3 (drop after max).
- Replay triggers: `Window.Created` (App.xaml.cs) and `OnResume` — NOT `ConnectivityChanged`.

## Push notifications (Phase 5.4 — blocked, compile-gated)

- `IPushNotificationService.RegisterAsync` — the real path exists only under `#if ANDROID && FSH_FIREBASE`. Without it: logs + returns null.
- Enabling requires: Firebase project + `google-services.json` + `Plugin.Firebase.CloudMessaging` package + a backend FCM sender (none exists in FSH yet). Follow `opencode/addBlazorFrontends/Phase-05-MAUI-Hybrid/push-setup.md`.
- `POST_NOTIFICATIONS` + `INTERNET` are already declared in AndroidManifest.xml.

## Deep linking

- Scheme `fsh://`; Android intent filter in `MainActivity`/manifest, iOS `CFBundleURLTypes` in Info.plist.
- `App.OnAppLinkRequestReceived` → `IDeepLinkService.Parse` → `DeepLinkTarget(ShellRoute, BlazorPath)`: `fsh://home|settings|about` → native shell pages; any other path → shell home + `HybridNavigationBridge.PendingPath` → `Main.razor` navigates the Blazor router (e.g. `fsh://files`, `fsh://tickets/123`).

## Build, run & verify

```bash
# Windows (Windows host only — windows TFM is added conditionally on OS)
dotnet build clients/FSH.Hybrid/FSH.Hybrid/FSH.Hybrid.csproj -f net10.0-windows10.0.19041.0
dotnet build clients/FSH.Hybrid/FSH.Hybrid -t:Run -f net10.0-windows10.0.19041.0

# Android / iOS / Mac Catalyst
dotnet build clients/FSH.Hybrid/FSH.Hybrid -t:Run -f net10.0-android
dotnet build clients/FSH.Hybrid/FSH.Hybrid -t:Run -f net10.0-ios        # requires Mac
dotnet build clients/FSH.Hybrid/FSH.Hybrid -t:Run -f net10.0-maccatalyst

# Full gate: clean build (Windows + Android) + tests
pwsh opencode/addBlazorFrontends/verify-hybrid.ps1
```

Test project gotchas (FSH.Hybrid.Tests): must set `<PlatformTarget>x64</PlatformTarget>`, `EnableMaui*Processing=false` (transitive resizetizer would fail on the app's duplicate appicon), and `Microsoft.Extensions.Http` ≥ 10.0.10 (BlazorShared floor). Dispose `OfflineQueueService` (or call `CloseAsync`) before deleting temp DBs.

## Key MAUI NuGet packages

```xml
<PackageReference Include="CommunityToolkit.Maui" Version="13.0.0" />
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.2" />
<PackageReference Include="MudBlazor" Version="9.7.0" />
<PackageReference Include="sqlite-net-pcl" Version="1.9.172" />
<PackageReference Include="SQLitePCLRaw.bundle_green" Version="2.1.11" />   <!-- NU1903 pin: no patched version exists (CVE-2025-6965) -->
<PackageReference Include="Xamarin.AndroidX.Biometric" Version="1.1.0.33" Condition="android TFM" />
<!-- Plugin.Firebase.CloudMessaging: add ONLY when enabling push (see push-setup.md) -->
```

## Hybrid-specific patterns

- **BlazorWebView hosting**: `MainPage.xaml` hosts `<blazor:BlazorWebView HostPage="wwwroot/index.html">` with `RootComponent Selector="#app"` → `Main.razor`. `Main.razor` checks `HybridNavigationBridge.PendingPath` on init and navigates the Blazor router.
- **Lifecycle**: MAUI events, not Blazor's — queue flush + deep links + biometric lock live in `App.xaml.cs` (`Window.Created`, `OnResume`, `OnSleep`). Blazor pages do NOT receive MAUI lifecycle events.
- **Windows backdrop**: `App.xaml.cs` applies `MicaBackdrop` on `Window.HandlerChanged`.
- **Safe area / splash / icon**: csproj `MauiSplashScreen` (splash.svg, `Color="#1B2A2C"`, `BaseSize="128,128"`) and `MauiIcon` (`Color="#1B2A2C"`); brand colors dark-teal `#1B2A2C` + accent `#0FB5AE`.
- **Theme parity**: `FshThemeService` key `"fsh.theme"`, default `ThemeMode.System` — same as the dashboard app.
- **Do not** use browser APIs via JS interop where a native service exists (`MediaPicker`, `SecureStorage`, connectivity). `#if` + platform-suffixed files for platform-specific code.
