# Implementation Summary — Phase D: MAUI Hybrid rules refresh

- **Date:** 2026-08-06 03:55:14
- **Session:** sess-maui
- **Scope:** `.agents/rules/frontend/maui-hybrid.md` (docs-only)

## What was delivered

Rewrote `.agents/rules/frontend/maui-hybrid.md` so it matches the code delivered in Phase B (`bc00bea5`) instead of the pre-implementation draft:

- **Structure tree** — now lists the real layout: `Main.razor` root + `HybridNavigationBridge`, `Pages/` Blazor pages (Overview, Files, Auth/Login), `Shared/`, `Auth/FshPolicies.cs`, all 13 services incl. `OfflineQueueProcessor`/`OfflineDelegatingHandler`/`MauiAuthStateProvider`/`HybridRuntimeConfigService`, and the `FSH.Hybrid.Tests` project.
- **Auth** — corrected the over-claim: token is stored via SecureStorage; biometrics gate session **resume** (`Lock()` on `OnSleep`, `AuthenticateAsync("Unlock FSH Hybrid")` on `OnResume` when the Preferences flag is set), it does not encrypt the token.
- **MauiProgram wiring** — real registrations incl. named clients with the exact handler order (`FSH.Api` = OfflineDelegatingHandler outermost → AuthDelegatingHandler; `FSH.Auth` has none; `FSH.Storage` for presigned PUTs) and theme parity (`fsh.theme`, `ThemeMode.System`).
- **Offline queue** — replay triggers are `Window.Created` + `OnResume` (not `ConnectivityChanged` as the draft claimed), retry cap 3, `IAsyncDisposable`.
- **Push** — documented as compile-gated (`#if ANDROID && FSH_FIREBASE`), referencing `push-setup.md`; `Plugin.Firebase.CloudMessaging` is added only when enabling push.
- **Deep links** — real flow: `OnAppLinkRequestReceived` → `IDeepLinkService.Parse` → shell route or `HybridNavigationBridge.PendingPath` → `Main.razor`.
- **Build/verify** — real commands (conditional windows TFM) + `verify-hybrid.ps1` + test-project gotchas (PlatformTarget x64, `EnableMaui*Processing=false`, Microsoft.Extensions.Http ≥ 10.0.10, dispose queue before temp-DB delete).
- **Packages** — actual versions (CommunityToolkit.Maui 13.0.0, Mvvm 8.4.2, MudBlazor 9.7.0, sqlite-net-pcl 1.9.172, bundle_green 2.1.11 with NU1903 pin note, AndroidX.Biometric android-only).

## Verification

Docs-only change — no build required. Tree re-verified against `git status` (only the two intended files staged).

## Known issues / notes
- `.github/**` MAUI CI (5.10) remains deferred (frozen) — unchanged.
