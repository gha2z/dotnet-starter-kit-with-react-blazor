# Implementation Summary 2026-Aug-09 19:00

---
**Description:** Session round 3 close-out — cold-start deep link hardening (3/3), SecureStorage theme persistence (device-verified), SQLitePCLRaw NU1903 documented (blocked upstream), verify-hybrid 12/12 green
**Creator:** opencode (auto/coding, model: omniroute/auto/coding)
**Duration:** ~3h
---

## A. Cold-Start Deep Links — Verified 3/3
---
**Root cause:** MAUI's `OnAppLinkRequestReceived` never routes the Android cold-start VIEW intent.

**Fix (two commits):**
1. `MainActivity.OnCreate` stashes `Intent.Data` into `HybridNavigationBridge.InitialAppLink` **before** `base.OnCreate` (empirically `CreateWindow` runs inside `base.OnCreate` — stash-after-base silently lost the link) — `586f1388` (initial), `d5d8f88a` (ordering fix).
2. `App.CreateWindow` drains via `TakeInitialAppLink()` → `HandleAppLink`; hardened with bare `new DeepLinkService().Parse` fallback + null-safe `Shell.Current`.

**Device verification:** cold `fsh://files` VIEW start → `https://0.0.0.1/files` — pids 10763, 10909, 11050, all three landed `/files` without hitting login (token persisted in SecureStorage). Warm links unchanged (`OnNewIntent`).

## B. Theme Persistence — Device-Verified Across Restart
---
**Implementation:** `SecureThemeService` (`Services/SecureThemeService.cs`) — sealed subclass of `FshThemeService`, overrides `ReadStoredAsync`/`WriteStoredAsync` via `SecureStorage.Default` (key `fsh.theme`). Depends on sess-main's wave-16 virtual seams (`be50e8ca`). Registered as `AddSingleton<FshThemeService>(sp => new SecureThemeService(...))` in `MauiProgram.cs` (line ~89).

**Pitfall caught:** Registering only `SecureThemeService` (without base type key) causes `@inject FshThemeService Theme` to fail silently — webview renderer `DeadObjectException`, page stuck at "Loading.../An unhandled error occurred." Fixed by using exact type key.

**Device verification:** toggled theme via MudMenu Light/Dark/System → dark applied (`rgba(18,18,22,1)`) → force-stop → cold restart → dark persisted on login page (bgVar confirmed). Theme toggle uses MudMenu activator (not a direct toggle) — trusted `Input.dispatchMouseEvent` required for reliable interaction.

**Commits:** `a627074e` (SecureThemeService + MauiProgram DI + sess-maui.md register refresh)

## C. SQLitePCLRaw NU1903 — Blocked Upstream
---
**Vulnerability:** `SQLitePCLRaw.lib.e_sqlite3(.android)` 2.1.11 → GHSA-2m69-gcr7-jv3q (High severity). Transitive dependency of `SQLitePCLRaw.bundle_green` 2.1.11.

**Status:** No patched NuGet version exists (2.1.11 is latest). Advisory documented in `docs/security/SQLitePCLRaw-NU1903-GHSA-2m69-gcr7-jv3q.md`. Bump deferred pending upstream 2.2.0 release.

## D. Verification & Close-Out
---
**Verification:** `verify-hybrid.ps1` under lock (`-LockVerify` → verify → `-UnlockVerify`)
- hybrid (net10.0-windows10.0.19041.0) build: **0 errors** / 33 warnings
- hybrid (net10.0-android) build: **0 errors** / 77 warnings
- FSH.Hybrid.Tests: **12/12 PASSED** (0 failed, 0 skipped)

## Verification
---
| Check | Result |
|-------|--------|
| Cold-start deep links | 3/3 passed (`fsh://files` → `/files` without login) |
| Theme persistence | Verified: dark → restart → dark on login page |
| hybrid windows build | 0 errors / 33 warnings (NU1903 + NU1608) |
| hybrid android build | 0 errors / 77 warnings (NU1903 + NU1608) |
| FSH.Hybrid.Tests | 12/12 green |

## Known Limitations
---
- SQLitePCLRaw 2.1.11 vulnerable (NU1903, High) — no upstream fix published yet.
- Theme toggle is MudMenu activator (opens dropdown with Light/Dark/System) — not a one-click toggle. Requires trusted mouse event via CDP for automation.
- A3 file upload verified to scriptable boundary only (SAF picker launch + resume) — actual file selection requires a human on device.
- NU1608 warnings (AndroidX lifecycle constraint mismatches) — cosmetic, do not affect build or runtime.
