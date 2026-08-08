# sess-maui
identity: opencode/deepseek-v4-flash-free | started: 2026-08-06 02:43 | state: active | heartbeat: 2026-08-08 06:13

scope: clients/FSH.Hybrid/** · clients/admin-blazor/** (per amended scope) · root README.md ·
.agents/rules/frontend/maui-hybrid.md · opencode/addBlazorFrontends/live/sess-maui.md ·
opencode/addBlazorFrontends/verify-hybrid.ps1 · 00_summary summaries

## Current task
Device demo loop (emulator pos-testing): DONE - login + dashboard verified end-to-end. Fixed the cold-start double Blazor.start() (manual poll script removed from index.html; native Android WebKitWebViewClient.OnPageFinished owns startup) and the dashboard FshErrorBoundary (FshPageHeader Subtitle -> Description in OverviewPage + FilesPage). Dashboard renders "Acme Corp / SUBSCRIPTION / Active" from live API on cold boot. Ready to commit.

## Verification round 2 (2026-08-08, emulator pos-testing)
- Safe area verified on device: body pad 24px top / 32px bottom, fsh-topbar top=48 h=56, innerH 712, dpr 2 (committed 40bede5a; screenshot safearea-check.png deleted at teardown)
- Deep links FIXED + verified: fsh://files on warm app -> /files (SingleTop + OnNewIntent -> App.HandleAppLink -> HybridNavigationBridge.BlazorPathReceived; committed 5045e07c)
- Files 403 root cause: acme Manager role lacked Files claims in demo seed. DemoSeeder.cs fixed + seed-demo re-run; fresh token -> 200 (committed 3b12a2fc - OUT OF ZONE, user-approved)
- Theme toggle FIXED + verified: Main.razor now subscribes Theme.Changed -> MudThemeProvider live re-renders (dark bg rgb(18,18,22) via trusted CDP click; committed 5045e07c). Known nit: localStorage persistence of theme silently no-ops in hybrid webview (service's best-effort catch) - cosmetic.
- FSH.Hybrid.Tests 12/12 green (lock taken/released). Biometric: emu finger OK (no biometric flow in app - N/A)
- API pid 23400 stopped at teardown; containers fsh-dev-* left running for sess-main

## Touched files (update as you go)
- opencode/addBlazorFrontends/Phase-05-MAUI-Hybrid/plan.md (checklist refresh + Next Up + arch-decision fix)
- opencode/addBlazorFrontends/live/board.md (row #3 heads-up)
- clients/admin-blazor/FSH.Admin.Wasm/Pages/Settings/AppearancePage.razor (Density card + Active badge + Light→Dark order + aria-pressed)
- clients/admin-blazor/FSH.Admin.Wasm/Shared/CommandPalette.razor (new)
- clients/admin-blazor/FSH.Admin.Wasm/Shared/CommandPalette.razor.cs (new)
- clients/admin-blazor/FSH.Admin.Wasm/Shared/MainLayout.razor (NavSpec consumption + search button + palette host)
- clients/admin-blazor/FSH.Admin.Wasm.Tests/Components/CommandPaletteTests.cs (new, 8 tests)
- opencode/addBlazorFrontends/readme.md
- opencode/addBlazorFrontends/live/README.md (new)
- opencode/addBlazorFrontends/live/board.md (new)
- opencode/addBlazorFrontends/live/_template.md (new)
- opencode/addBlazorFrontends/live/sess-main.md (new, seeded)
- opencode/addBlazorFrontends/live/sess-maui.md (new)
- opencode/addBlazorFrontends/STATUS.md (Phase 5 row only)
- opencode/addBlazorFrontends/Phase-05-MAUI-Hybrid/plan.md (status line)
- opencode/addBlazorFrontends/Phase-05-MAUI-Hybrid/push-setup.md (new)
- opencode/addBlazorFrontends/verify-hybrid.ps1 (new)
- clients/FSH.Hybrid/FSH.Hybrid/FSH.Hybrid.csproj (sqlite-net-pcl + SQLitePCLRaw.bundle_green 2.1.11 pin)
- clients/FSH.Hybrid/FSH.Hybrid/MauiProgram.cs (DI: connectivity/queue/processor/deeplink/mediapicker/push + FSH.Storage client + File/Health/Audit services)
- clients/FSH.Hybrid/FSH.Hybrid/App.xaml.cs (queue process on create/resume, OnAppLinkRequestReceived)
- clients/FSH.Hybrid/FSH.Hybrid/Main.razor (deep-link PendingPath consumption)
- clients/FSH.Hybrid/FSH.Hybrid/Pages/FilesPage.razor (new, /files)
- clients/FSH.Hybrid/FSH.Hybrid/Services/ConnectivityService.cs (new)
- clients/FSH.Hybrid/FSH.Hybrid/Services/OfflineQueueService.cs (new + IAsyncDisposable)
- clients/FSH.Hybrid/FSH.Hybrid/Services/OfflineDelegatingHandler.cs (new, OfflineException)
- clients/FSH.Hybrid/FSH.Hybrid/Services/OfflineQueueProcessor.cs (new)
- clients/FSH.Hybrid/FSH.Hybrid/Services/MediaPickerService.cs (new: picker/camera + HybridFileUploadService)
- clients/FSH.Hybrid/FSH.Hybrid/Services/DeepLinkService.cs (new)
- clients/FSH.Hybrid/FSH.Hybrid/Services/PushNotificationService.cs (new, FSH_FIREBASE-gated)
- clients/FSH.Hybrid/FSH.Hybrid/Platforms/Android/MainActivity.cs (fsh:// intent filter)
- clients/FSH.Hybrid/FSH.Hybrid/Platforms/Android/AndroidManifest.xml (new: POST_NOTIFICATIONS + INTERNET)
- clients/FSH.Hybrid/FSH.Hybrid/Platforms/iOS/Info.plist (new: fsh URL scheme)
- clients/FSH.Hybrid/FSH.Hybrid/Resources/Splash/splash.svg (F monogram)
- clients/FSH.Hybrid/FSH.Hybrid.Tests/ (new project: 12 tests)
- clients/FSH.Hybrid/FSH.Hybrid/wwwroot/index.html (removed manual Blazor.start() poll script - double-start fix)
- clients/FSH.Hybrid/FSH.Hybrid/Pages/OverviewPage.razor (FshPageHeader Subtitle -> Description)
- clients/FSH.Hybrid/FSH.Hybrid/Pages/FilesPage.razor (FshPageHeader Subtitle -> Description)
- clients/FSH.Hybrid/FSH.Hybrid/Pages/SettingsPage.xaml.cs (missing InitializeComponent() - crash fix)
- clients/FSH.Hybrid/FSH.Hybrid/Shared/RedirectToLogin.razor (NavigateTo without forceReload)

## Blockers / requests to other sessions
- [x] Admin palette (Phase C) parked until sess-main's 3.14 lands + verify gate passes → board row #1
- [x] 5.4 push: blocked on Firebase project + google-services.json + backend sender — compile-gated + push-setup.md written
- [ ] Demo interactivity: needs human on emulator (login) — first-run device loop otherwise complete

## Device demo state (live, 2026-08-06)
- DEMO LOOP COMPLETE: login -> dashboard verified on emulator-5554 (cold boot -> Overview "Acme Corp / SUBSCRIPTION / Active", no FshErrorBoundary); fixes committed 78606f20
- Teardown DONE 11:58: API pid 22024 stopped; fsh-dev-postgres/redis/minio stopped (Exited 0); adb reverse/forward rules lapsed with process cleanup
- AVD `pos-testing` booted (emulator-5554); hybrid app installed (com.fullstackhero.hybrid, pid live)
- Dev data plane: `fsh-dev-postgres` (5432), `fsh-dev-redis` (6379), `fsh-dev-minio` (9000/9001, bucket `local/fsh`); DB migrated+seeded (root/acme/globex tenants)
- API pid 31172 on http://0.0.0.0:5030 (dev env, S3→MinIO); `adb reverse tcp:5030` + `tcp:9000`
- `fsh.hybrid.apiBase` preference seeded → http://10.0.2.2:5030
- First-run crash fixes (committed 70dd9af2): AppShell TabBar→Tab (FlyoutItem can't hold TabBar), ACCESS_NETWORK_STATE permission (ConnectivityService), AddMauiBlazorWebView (MAUI 10 renamed UseMauiBlazor)
- Teardown when done: containers fsh-dev-*, kill API pid 31172

## Done today
- [x] Phase A coordination package
- [x] MAUI 5.5 offline queue (+ FSH.Hybrid.Tests 12/12)
- [x] MAUI 5.6 media picker + presigned upload proof page
- [x] MAUI 5.7 deep linking
- [x] MAUI 5.9 splash/icon audit (splash F monogram)
- [x] MAUI 5.4 push (blocked item — doc + compile-gated code)
- [x] verify-hybrid.ps1 run
- [x] Phase C admin palette (committed e4b3dcb7, 155/155)
- [x] Phase D docs (maui-hybrid.md refresh, committed 6a5d84e2; verify-hybrid 12/12 + admin 155/155 re-run green)
- [x] Board row #2: admin accent/font/density settings (AppearancePage parity, committed 2026-08-06 04:47)
- [x] Phase-05 plan-state refresh (5.1–5.7/5.9 ✅ + blocked/external annotations) + board row #3
- [x] Device demo loop: login -> dashboard verified on emulator; fixed cold-start double Blazor.start() + FshPageHeader Subtitle bug (pending commit)
- [ ] refresh summary + explicit-path staging (awaiting user approval; never `git add -A`)
