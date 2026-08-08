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
- clients/FSH.Hybrid/FSH.Hybrid/App.xaml.cs (CreateWindow drains InitialAppLink; HandleAppLink hardened — fallback parser + null-safe Shell)
- clients/FSH.Hybrid/FSH.Hybrid/Services/DeepLinkService.cs (HybridNavigationBridge: InitialAppLink + TakeInitialAppLink)
- clients/FSH.Hybrid/FSH.Hybrid/Platforms/Android/MainActivity.cs (OnCreate stashes cold-start VIEW intent link; +using FSH.Hybrid.Services)
- NOTE: cdp.ps1 untracked at repo root — temp CDP debug helper, NOT committed, deleted at teardown

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
