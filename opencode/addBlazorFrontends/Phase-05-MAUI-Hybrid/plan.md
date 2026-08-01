# Phase 5 — MAUI Blazor Hybrid

> **Target:** Cross-platform MAUI Blazor Hybrid app (Android/iOS/Windows/macOS) sharing code with WASM via BlazorShared RCL. Push notifications, biometric auth, camera, offline queue, deep linking, in-app purchases.

## Status

- Phase 5: **🟨 Scaffold exists, workload resolved — app has ZERO `.razor` pages** (bare shell project only)
- Prerequisites: Phase 2 ✅ + Phase 3 ✅ (BlazorShared RCL stable, all pages known) — Phases 2/3 are **not** complete; Hybrid build-out starts once pages land
- **MAUI workload (resolved Aug 2026):** `dotnet workload install maui` must run **elevated** (UAC). A non-elevated attempt corrupted the workload store (deleted manifest packs under `sdk-manifests\10.0.300\`, breaking every build with `MSB4242`). Recovery recipe: elevated → delete stale `workloadsets\10.0.302` → **recreate the empty folder** (installer requires it) → elevated `dotnet workload install maui`. Full story in `Phase-07-Parity-Completion/hands-on-phase-7.md` §7.6.
- **Parity note:** Hybrid reuses `BlazorShared` (shell CSS, `FshThemeService`, components). `FshThemeService`/`ThemeMode` are not yet referenced by Hybrid — wire `("fsh.theme", ThemeMode.System)`-style registration when the shell is built (same storage contract; MAUI should use SecureStorage-backed token store but localStorage-backed theme is acceptable).

## Task Checklist

### 5.1 Project Scaffold & Setup
- [ ] Create `clients/FSH.Hybrid/FSH.Hybrid/` directory structure
- [ ] Create `FSH.Hybrid.csproj` — MAUI project targeting net10.0-android, net10.0-ios, net10.0-maccatalyst, net10.0-windows
  - Reference `FSH.BlazorShared` RCL
  - Add NuGet: `CommunityToolkit.Mvvm`, `CommunityToolkit.Maui`
- [ ] Create `MauiProgram.cs` — DI registration matching WASM bootstrap pattern
  - Custom `ITokenStore` → `MauiTokenStore` (SecureStorage)
  - Custom `IAuthService` → `MauiAuthService` (biometric unlock)
  - Register all BlazorShared services, HttpClient pipelines
- [ ] Create `App.xaml` / `App.xaml.cs` — MAUI Application, Shell navigation
- [ ] Create `MainPage.xaml` — BlazorWebView host
  - `<BlazorWebView HostPage="wwwroot/index.html">`
  - `<RootComponent Selector="#app" ComponentType="{x:Type local:Main}" />`
- [ ] Create `wwwroot/index.html` — Minimal host page, load MudBlazor CSS/JS, app CSS
- [ ] Create `Platforms/` folder structure (Android, iOS, MacCatalyst, Windows)
- [ ] Configure `Properties/launchSettings.json` for each platform

### 5.2 Auth — Native Token Storage
- [ ] **MauiTokenStore** — `ITokenStore` using `SecureStorage`
  - `GetAsync()` / `SetAsync()` / `ClearAsync()`
  - Platform-specific keychain (KeyChain on Android, Keychain on iOS)
- [ ] **Biometric service** — Interface + platform implementations
  - Android: `BiometricPrompt` API
  - iOS: `LAContext` LocalAuthentication
  - Windows: Windows Hello
- [ ] **MauiAuthStateProvider** — Extends shared AuthStateProvider
  - Optionally require biometric unlock on app resume
- [ ] **MauiAuthService** — Wraps IAuthService + biometric unlock

### 5.3 Native Navigation Shell
- [ ] **AppShell.xaml** — MAUI Shell with:
  - FlyoutItem entries for each major section
  - TabBar for top-level navigation (phone) / Flyout (tablet)
  - Register routes for all pages
- [ ] **Shell navigation wiring** — Map Blazor routes to MAUI Shell routes
  - Blazor pages render in BlazorWebView content area
  - Shell items do NOT wrap every page (Hybrid: Shell handles global nav, Blazor handles inner nav)
- [ ] **Platform-specific styling**
  - Android: Material 3 colors, status bar
  - iOS: Safe area insets, navigation bar appearance
  - Windows: Mica backdrop, title bar customization

### 5.4 Push Notifications
- [ ] **IPushNotificationService** — Interface
- [ ] **Android implementation** — Firebase Cloud Messaging
  - `FirebaseMessagingService` subclass
  - Handle `OnNewToken`, `OnMessageReceived`
  - Display notification channel
- [ ] **iOS implementation** — APNs
  - Register for remote notifications
  - `DidReceiveRemoteNotification`
- [ ] **Permission handling** — Request notification permission on first launch
- [ ] **Deep link from notification** — Navigate to relevant page on tap

### 5.5 Offline Queue
- [ ] **IQueueService** — Interface (enqueue, dequeue, peek, retry)
- [ ] **SQLite implementation** — Store failed HTTP calls
  - Table: queue items (method, url, headers, body, createdAt, retryCount)
  - Max retries: 3
- [ ] **ConnectivityService** — Monitor `Connectivity.Current.NetworkAccess`
  - Fires `OnConnectivityChanged` event
  - On reconnect: process queue items sequentially
- [ ] **QueueProcessor** — Dequeue → retry → success: remove, fail: increment retry
- [ ] **DelegatingHandler integration** — On HTTP failure, check connectivity → queue instead of throw

### 5.6 Camera & File Access
- [ ] **ICameraService** — Interface
- [ ] **Implementation** — `MediaPicker.Default.CapturePhotoAsync()`, `CaptureVideoAsync()`
- [ ] **IFilePickerService** — Interface
- [ ] **Implementation** — `FilePicker.Default.PickAsync()` with file type filters
- [ ] **Integration** — Pass picked file to the upload flow (presigned URL upload)
  - In WASM: file picked via browser `<input>`
  - In MAUI: picked via native picker → Stream → HttpClient upload

### 5.7 Deep Linking
- [ ] **URL scheme registration** — `fsh://` custom scheme
  - Android: Intent filter in `AndroidManifest.xml`
  - iOS: `CFBundleURLTypes` in `Info.plist`
  - Windows: Protocol handler registration
- [ ] **Universal Links** (iOS) / **App Links** (Android)
  - Associate website (`/.well-known/assetlinks.json`, `apple-app-site-association`)
- [ ] **Deep link handler** — Parse URL → navigate to Blazor page
  - `fsh://login?token=...` → autologin
  - `fsh://ticket/123` → navigate to ticket detail
  - Link from notification → navigate to relevant page

### 5.8 In-App Purchases
- [ ] **ISubscriptionService (MAUI)** — Wraps platform billing
  - Android: `Google.BillingClient` (via CommunityToolkit.Maui.InAppPurchases or manual)
  - iOS: `StoreKit` (via `SKProductsRequest`, `SKPaymentQueue`)
  - Windows: `Microsoft.Windows.ApplicationModel.Store`
- [ ] **Sync with server** — Verify purchase server-side (Google Play Billing receipt validation, iOS receipt)
  - MAUI calls platform → gets purchase token → sends to API → API verifies → upgrades subscription
- [ ] **Restore purchases** — Button to restore previously purchased items

### 5.9 Splash Screen & App Icon
- [ ] **Splash screen** — FSH brand logo, centered on brand background
  - Android: `mipmap-*` resources
  - iOS: `LaunchScreen.storyboard` or `UILaunchStoryboardName`
- [ ] **App icon** — Generated icons for all platforms/resolutions
  - `Icon/` in MAUI project with `AppIcon` in `.csproj`

### 5.10 Build & Sign Configuration
- [ ] **Android signing** — Keystore config in csproj for release builds
- [ ] **iOS provisioning** — Entitlements.plist, code signing
- [ ] **CI/CD** — GitHub Actions workflow for MAUI builds
  - Android: `dotnet publish -f net10.0-android -c Release`
  - iOS: requires Mac runner + Apple Developer account

## Next Up

**Task 5.1**: Scaffold MAUI Hybrid project.

1. `dotnet workload install maui` (one-time)
2. Create `clients/FSH.Hybrid/FSH.Hybrid/` directory tree
3. Create `.csproj` targeting net10.0-android/ios/maccatalyst/windows
4. Add reference to `clients/BlazorShared/FSH.BlazorShared.csproj`
5. Create `MauiProgram.cs` with DI registration
6. Create `App.xaml` + `MainPage.xaml` with BlazorWebView
7. Verify `dotnet build` succeeds for at least net10.0-windows
8. Run: `dotnet build -t:Run -f net10.0-windows`

## Architecture Decisions

| Decision | Choice | Why |
|---|---|---|
| MAUI version | .NET 10 MAUI | Matches project target; latest stable |
| Token storage | SecureStorage (not localStorage) | Native encrypted storage; no JS interop needed |
| Auth method | Biometric + SecureStorage | Face ID / Fingerprint unlock before showing token |
| Offline DB | SQLite (via Microsoft.Data.Sqlite) | Lightweight, works on all platforms, no EF needed for simple queue |
| Push notifications | Firebase (Android) + APNs (iOS) | Standard; CommunityToolkit.Maui has helpers for both |
| IAP | Platform-specific APIs | No cross-platform IAP library is mature for .NET 10 MAUI |
| Shell navigation | MAUI Shell + Blazor inner nav | Hybrid approach: Shell for top-level, Blazor for page-level |
| File picker | MAUI MediaPicker / FilePicker | Native pickers rather than browser input |

## Notes & Gotchas

- **workload install**: `dotnet workload install maui` is required. This installs Android SDK, iOS SDK, and MAUI workloads. May need admin on Windows.
- **Platform-specific code**: Use `#if ANDROID`, `#if IOS`, `#if WINDOWS`, `#if MACCATALYST` preprocessor directives in shared code files. Name files with platform suffix (`BiometricService.Android.cs`).
- **SecureStorage caveats**: Android backups may include SecureStorage. Use `Android\Security\SecureStorage` with `KeyStore` if available. Android keystore requires API 23+.
- **BlazorWebView lifecycle**: Pages in BlazorWebView do NOT receive MAUI lifecycle events. Use `Application.Current` or `Page` events for app foreground/background.
- **SignalR in MAUI**: `HubConnection` uses `WebSocket` via `HttpClient`. On Android, must target .NET 10's default handler (no custom socket needed).
- **SSE in MAUI**: Works natively with `HttpClient.GetStreamAsync()` + `StreamReader` — no JS interop needed for SSE on native HTTP stack.
- **App size**: MAUI apps with BlazorWebView + MudBlazor are large (30-50MB base). Enable assembly trimming for release builds: `<PublishTrimmed>true</PublishTrimmed>`.
- **Hot reload**: `dotnet watch` works with MAUI for rapid iteration. Use `dotnet build -t:Run -f net10.0-android` for device testing.

## Blocker Checklist

- [ ] `dotnet workload install maui` completed successfully
- [ ] BlazorShared RCL is stable (no WASM-specific dependencies)
- [ ] Phase 2 + 3 pages defined and working in WASM (MAUI reuses same BlazorShared components)
- [ ] .NET 10 MAUI template available (`dotnet new maui`)
- [ ] Android SDK installed (if targeting Android) — check `dotnet workload list`
- [ ] iOS requires Mac build host (skip iOS tasks if no Mac available)
