# Implementation Summary — MAUI Hybrid Phase 5 (5.4–5.9) + Coordination Protocol

- **Date:** 2026-08-06 03:11:35
- **Session:** sess-maui (`opencode/deepseek-v4-flash-free`), parallel to sess-main (dashboard 3.14 Command Palette)
- **Model:** opencode/deepseek-v4-flash-free
- **Scope:** `clients/FSH.Hybrid/**` · coordination package in `opencode/addBlazorFrontends/` · `verify-hybrid.ps1`
- **Out of scope:** `BlazorShared` (read-only, sess-main's), `dashboard-blazor/**` (sess-main's), `.github/**` (frozen — MAUI CI deferred), admin command palette (parked)

## What was delivered

### A. Multi-session coordination protocol (`opencode/addBlazorFrontends/`)
- `readme.md`: new **Multi-Session Coordination Protocol** (session identity, single-writer `live/sess-<id>.md`, append-only `live/board.md`, task claims, read-fresh ceremony, 12h heartbeat staleness rule, verify.ps1 contention rule, same-checkout default), explicit-path staging policy (never `git add -A`), session-id suffix on summary filenames, amended Scope Restriction (admin-blazor parallel-safe, `README.md` allowed, `.github/**` frozen).
- `live/`: `README.md`, `board.md` (seeded with admin-palette heads-up), `_template.md`, `sess-main.md` (seeded), `sess-maui.md` (this session, heartbeat kept fresh).

### B. MAUI Hybrid native features (Phase 5)
- **5.5 Offline queue** — SQLite-backed FIFO (`sqlite-net-pcl 1.9.172`), `IOfflineQueueService` + `OfflineDelegatingHandler` (queues POST/PUT/PATCH/DELETE when offline, throws `OfflineException`; GETs and auth client pass through) + `IOfflineQueueProcessor` (FIFO replay via "FSH.Api" pipeline, retry cap 3 then drop). Wired: `MauiProgram.cs` (outermost handler on FSH.Api), `App.xaml.cs` (replay on window created + resume). `OfflineQueueService` is `IAsyncDisposable`.
- **5.6 Media picker + upload** — `IMediaPickerService` (FilePicker/MediaPicker + content-type map) + `IHybridFileUploadService` presigned flow (`OwnerType: "MyFiles"` → PUT via "FSH.Storage" client with RequiredHeaders minus Content-Type/Length → `FinalizeUploadAsync`). Proof page **FilesPage** at `/files` (MudTable, FshFormat/FshStatusPill, SizeBytes→human formatter).
- **5.7 Deep links** — `fsh://` scheme: Android `[IntentFilter]` on MainActivity, iOS `Info.plist` CFBundleURLTypes, `OnAppLinkRequestReceived` → `HybridNavigationBridge.PendingPath` → `Main.razor` navigates.
- **5.9 Splash** — `Resources/Splash/splash.svg` now branded (dark-teal `#1B2A2C`, accent `#0FB5AE`, F monogram).
- **5.4 Push (compile-gated)** — `IPushNotificationService`/`PushNotificationService`; the real Firebase path lives behind `#if ANDROID && FSH_FIREBASE` and requires user's Firebase project + `google-services.json` + backend sender (none exists in FSH yet). Enablement guide: `Phase-05-MAUI-Hybrid/push-setup.md`. Android `POST_NOTIFICATIONS` + `INTERNET` permissions declared.

### C. Tests — `clients/FSH.Hybrid/FSH.Hybrid.Tests/` (new)
- xunit net10.0-windows project; NSubstitute connectivity; per-class temp DBs, queues disposed (learned: `SQLiteAsyncConnection` holds the file → `IAsyncDisposable` on the service).
- 12 tests: FIFO order/payload, cross-reopen persistence, remove, retry increment, drop at cap, offline queue+throw, GET passthrough, online normal send, mid-flight drop, replay FIFO+remove, failed replay retries, offline skip.

## Verification

```
hybrid (net10.0-windows10.0.19041.0) build: 0 errors / 33 warnings
hybrid (net10.0-android) build: 0 errors / 77 warnings
hybrid tests: Passed!  - Failed: 0, Passed: 12, Skipped: 0, Total: 12
```
(`pwsh opencode/addBlazorFrontends/verify-hybrid.ps1` — clean obj/bin, both TFMs, test suite.)

## Known issues / notes
- **NU1903 (SQLitePCLRaw.lib.e_sqlite3, CVE-2025-6965):** pinned `SQLitePCLRaw.bundle_green 2.1.11` (latest). Advisory says **patched version: none** — upstream must release 2.1.12+. Practical risk nil: queue DB is a fixed local schema; no untrusted SQL. Revisit on next package refresh.
- **NU1608 warnings:** pre-existing transitive AndroidX constraint noise (CommunityToolkit.Maui/MAUI), not introduced here.
- **MSB3270 (test project):** test project pinned `PlatformTarget=x64` to match the app's win-x64 output.
- **Test project gotchas:** must set `EnableMaui*Processing=false` (transitive resizetizer would otherwise fail on the app's duplicate `appicon`), plus `Microsoft.Extensions.Http` 10.0.10 (BlazorShared floor).
- **Push (5.4):** blocked on external Firebase setup; nothing to run until then. Backend FCM sender is out of Phase 5 scope.
- **Phase C (admin command palette):** parked until sess-main's 3.14 lands + verify passes (board row #1) — do not start from a shared checkout while their verify.ps1 may run.

## Suggested commit message
`feat(hybrid): MAUI Phase 5 — offline queue, media upload, deep links, splash branding, push skeleton (gated); + coordination protocol + FSH.Hybrid.Tests (12/12)`
