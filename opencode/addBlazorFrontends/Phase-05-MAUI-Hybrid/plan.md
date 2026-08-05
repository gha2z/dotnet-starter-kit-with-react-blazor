# Phase 5 — MAUI Blazor Hybrid
Last Update: 2026-Aug-03 18:45:55, by: opencode (auto/coding, model: mimo-v2.5-free).

> **Target:** Cross-platform MAUI Blazor Hybrid app (Android/iOS/Windows/macOS) sharing code with WASM via BlazorShared RCL. Push notifications, biometric auth, camera, offline queue, deep linking, in-app purchases.

## Status

- Phase 5: **🟨 In progress (sess-maui)** — 5.1–5.3 shell scaffold ✅ (`c77c9939`), 5.5 offline queue ✅ (SQLite, tested 12/12), 5.6 media/camera upload ✅ (FilesPage at `/files`), 5.7 deep links ✅ (`fsh://`), 5.9 splash branding ✅ (all `bc00bea5`); 5.4 push compile-gated (needs Firebase — see `push-setup.md`); **blocked/external:** 5.4 iOS APNs, 5.7 universal/app links, 5.8 IAP, 5.10 signing + CI (`.github/**` frozen); docs refresh 2026-08-06 (maui-hybrid.md, `6a5d84e2`)
- Prerequisites: Phase 2 ✅ + Phase 3 ✅ (BlazorShared RCL stable, all pages known)
- **MAUI workload (resolved Aug 2026):** `dotnet workload install maui` must run **elevated** (UAC). A non-elevated attempt corrupted the workload store (deleted manifest packs under `sdk-manifests\10.0.300\`, breaking every build with `MSB4242`). Recovery recipe: elevated → delete stale `workloadsets\10.0.302` → **recreate the empty folder** (installer requires it) → elevated `dotnet workload install maui`. Full story in `Phase-07-Parity-Completion/hands-on-phase-7.md` §7.6.
- **Parity note:** Hybrid registers `FshThemeService` as `("fsh.theme", ThemeMode.System)` — same storage contract as the dashboard app (see `MauiProgram.cs`).

## Task Checklist

### 5.1 Project Scaffold & Setup
- [x] Create `clients/FSH.Hybrid/FSH.Hybrid/` directory structure — `c77c9939`
- [x] Create `FSH.Hybrid.csproj` — MAUI project targeting net10.0-android, net10.0-ios, net10.0-maccatalyst, net10.0-windows10.0.19041.0 (windows TFM added conditionally on Windows hosts)
  - Reference `FSH.BlazorShared` RCL
  - NuGet: `CommunityToolkit.Mvvm`, `CommunityToolkit.Maui` (13.0.0/8.4.2, also MudBlazor 9.7.0 + sqlite-net-pcl 1.9.172)
- [x] Create `MauiProgram.cs` — DI registration matching WASM bootstrap pattern
  - `ITokenStore` → `MauiTokenStore` (SecureStorage)
  - `IAuthService` → `AuthService` + `MauiAuthService`; `MauiAuthStateProvider` (biometric lock on resume)
  - All BlazorShared services, named HttpClient pipelines (`FSH.Auth` / `FSH.Api` w/ Offline+Auth handlers / `FSH.Storage`)
- [x] Create `App.xaml` / `App.xaml.cs` — MAUI Application, Shell navigation
- [x] Create `MainPage.xaml` — BlazorWebView host
  - `<BlazorWebView HostPage="wwwroot/index.html">`
  - `<RootComponent Selector="#app" ComponentType="{x:Type local:Main}" />`
- [x] Create `wwwroot/index.html` — Minimal host page, load MudBlazor CSS/JS, app CSS
- [x] Create `Platforms/` folder structure (Android, iOS, MacCatalyst, Windows)
- [x] Configure `Properties/launchSettings.json` — **N/A**: MAUI templates ship no launchSettings.json; launch via `dotnet build -t:Run` / VS MAUI tooling (see Build & verify in `maui-hybrid.md`)

### 5.2 Auth — Native Token Storage
- [x] **MauiTokenStore** — `ITokenStore` using `SecureStorage` (`GetAccessTokenAsync`/`SetAsync`/`ClearAsync` + refresh/impersonation token stashes) — `c77c9939`
- [x] **Biometric service** — `IBiometricService` + platform impls (`Xamarin.AndroidX.Biometric` on Android; iOS via `LAContext` in `BiometricService`) — `c77c9939`
- [x] **MauiAuthStateProvider** — Extends shared AuthStateProvider; biometric unlock required on resume when `fsh.hybrid.biometricLock` set — `c77c9939`
- [x] **MauiAuthService** — Wraps `IAuthService` + biometric unlock — `c77c9939`

### 5.3 Native Navigation Shell
- [x] **AppShell.xaml** — MAUI Shell with flyout entries (Home / Settings / About) — `c77c9939`
- [x] **Shell navigation wiring** — Shell handles global nav (Home/Settings/About), Blazor handles inner nav (`Main.razor` + `HybridNavigationBridge.PendingPath` for deep links) — `c77c9939`/`bc00bea5`
- [x] **Platform-specific styling**
  - Windows: Mica backdrop (`App.xaml.cs` `OnWindowHandlerChanged`); Android/iOS use MAUI defaults (status bar/safe area) — `c77c9939`

### 5.4 Push Notifications
- [x] **IPushNotificationService** — Interface (`RegisterAsync`) — `bc00bea5`
- [x] **Android implementation** — Firebase Cloud Messaging; compile-gated behind `#if ANDROID && FSH_FIREBASE` (needs Firebase project + `google-services.json` + package; see `push-setup.md`) — `bc00bea5`
- [ ] **iOS implementation** — APNs — **blocked/external**: needs Apple Developer + Mac build host; wire a `#if IOS && FSH_APNS` path when available (mirror the Android gate)
- [x] **Permission handling** — `POST_NOTIFICATIONS` + `INTERNET` declared in AndroidManifest; permission requested inside the gated Android path — `bc00bea5`
- [ ] **Deep link from notification** — **blocked/external**: depends on the FCM/APNs path above; hook `HybridNavigationBridge.PendingPath` on tap when enabled

### 5.5 Offline Queue
- [x] **IOfflineQueueService** — interface (enqueue, remove, mark-failed, snapshot) — `bc00bea5`
- [x] **SQLite implementation** — sqlite-net-pcl table (method, url, headers, body, createdAt, retryCount); max retries 3 then drop; `IAsyncDisposable` — `bc00bea5`
- [x] **ConnectivityService** — `IConnectivityService` monitors `Connectivity.Current.NetworkAccess` — `c77c9939`
- [x] **QueueProcessor** — `OfflineQueueProcessor`: dequeue → retry (FIFO via "FSH.Api" pipeline) → success remove / fail increment — `bc00bea5`
- [x] **DelegatingHandler integration** — `OfflineDelegatingHandler` (outermost on `FSH.Api`): offline + POST/PUT/PATCH/DELETE → queue + `OfflineException`; GETs pass through — `bc00bea5`
- **Deviation from plan:** replay is triggered on `Window.Created` + `OnResume` (App.xaml.cs), NOT on `ConnectivityChanged` — deliberate (documented in `maui-hybrid.md`)

### 5.6 Camera & File Access
- [x] **IMediaPickerService** — capture photo/video via `MediaPicker` with `MediaPickerOptions` — `bc00bea5`
- [x] **File picker** — `FilePicker.Default.PickAsync` + content-type map — `bc00bea5`
- [x] **Integration** — `IHybridFileUploadService` presigned flow (`OwnerType: "MyFiles"` → PUT via `FSH.Storage` → `FinalizeUploadAsync`); proof page **FilesPage** `/files` — `bc00bea5`

### 5.7 Deep Linking
- [x] **URL scheme registration** — `fsh://` custom scheme
  - Android: Intent filter in `MainActivity`/manifest — `bc00bea5`
  - iOS: `CFBundleURLTypes` in `Info.plist` — `bc00bea5`
  - Windows: not registered — optional future
- [ ] **Universal Links** (iOS) / **App Links** (Android) — **blocked/external**: needs a hosted domain with `.well-known/assetlinks.json` + `apple-app-site-association`
- [x] **Deep link handler** — `IDeepLinkService.Parse` (fsh://home|settings|about → shell; other paths → `HybridNavigationBridge.PendingPath` → Blazor router) — `bc00bea5`
  - `fsh://login?token=...` autologin / `fsh://ticket/123` → covered generically (any non-shell path lands in the Blazor router; explicit param parsing is future work)

### 5.8 In-App Purchases
- [ ] **Blocked/external** — platform billing (Google Billing / StoreKit / Store) + server-side receipt validation: no backend IAP endpoints exist in FSH, no Apple/Google billing accounts on this machine. Revisit after backend IAP support lands; then wrap as a compile-gated skeleton like 5.4 push.

### 5.9 Splash Screen & App Icon
- [x] **Splash screen** — `Resources/Splash/splash.svg` branded (dark-teal `#1B2A2C`, accent `#0FB5AE`, F monogram) via `MauiSplashScreen` — `bc00bea5`
- [x] **App icon** — `Resources/AppIcon/appicon.svg` + foreground with `Color="#1B2A2C"` via `MauiIcon`; all platform/resolution variants are generated by MAUI resizetizer from the svg

### 5.10 Build & Sign Configuration
- [ ] **Android signing** — release keystore config — **blocked/external**: needs user's keystore + secrets; add `<AndroidKeyStore>`/`<AndroidSigningKeyStore>` props when provided
- [ ] **iOS provisioning** — Entitlements.plist, code signing — **blocked/external**: needs Apple Developer account + Mac
- [ ] **CI/CD** — GitHub Actions workflow for MAUI builds — **blocked: `.github/**` is frozen** by coordination protocol; un-freeze via board sign-off when the other apps' CI is stable

## Phase C — Admin Command Palette (board row #1 sign-off, admin-blazor app code)

- [x] **Admin command palette (Ctrl+K)** — NavSpec single source + overlay + topbar search button + 8 bUnit tests — claimed by sess-maui @ 2026-08-06 03:50 · done @ 2026-08-06 (e4b3dcb7, admin 155/155)

## Next Up

All in-zone MAUI work is delivered (5.1–5.3 `c77c9939`, 5.4–5.7 + 5.9 `bc00bea5`). Remaining Phase 5 items are **blocked/external** (5.4 iOS APNs, 5.7 universal links, 5.8 IAP, 5.10 signing + CI — see checklists). Follow-up when unblocked:

1. 5.4 iOS: wire `#if IOS && FSH_APNS` path (mirror Android gate) + `push-setup.md` update.
2. 5.8 IAP: compile-gated skeleton once backend receipt endpoints exist.
3. 5.10: signing props (user secrets) + `.github` MAUI CI (needs board sign-off to un-freeze).

## Architecture Decisions

| Decision | Choice | Why |
|---|---|---|
| MAUI version | .NET 10 MAUI | Matches project target; latest stable |
| Token storage | SecureStorage (not localStorage) | Native encrypted storage; no JS interop needed |
| Auth method | Biometric + SecureStorage | Face ID / Fingerprint unlock before showing token |
| Offline DB | SQLite (sqlite-net-pcl 1.9.172 + SQLitePCLRaw.bundle_green 2.1.11) | Lightweight, works on all platforms, no EF needed for simple queue |
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
