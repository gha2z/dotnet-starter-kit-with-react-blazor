# sess-main
identity: opencode/deepseek-v4-flash-free (concrete modelID, verified 2026-08-06) | started: 2026-08-06 | state: active | heartbeat: 2026-08-06 08:55

scope: clients/dashboard-blazor/** · clients/BlazorShared/** (Main-only) · clients/admin-blazor/** (Main-owned by default; app-code edits by other sessions need a board sign-off per task) · STATUS.md · 00-Index.md · Phase-02/03/04/06/07 plan files · opencode/addBlazorFrontends coordination docs (readme.md, verify.ps1, coordination.ps1) · implementation summaries

## Current task
M2 (Phase 6 dashboard polish/perf) — wave 3 (docs + final E2E):
- Wave 1 committed `e5bb0367` (trim, tickets debounce, splash, shell a11y, mobile E2E); wave 2 committed `6b6fa334` (publish smoke, AOT off, deployability fix both apps, icon labels).
- Wave 3 in progress: READMEs written — dashboard `clients/dashboard-blazor/FSH.Dashboard.Wasm/README.md`, admin `clients/admin-blazor/FSH.Admin.Wasm/README.md`, BlazorShared component docs `clients/BlazorShared/README.md`.
- 6.8 slow-network E2E added (`SlowNetworkTests` — CDP throttle ~1 MB/s + 250 ms latency): splash shown, boot < 180 s, no error-ui → suite now **18/18**.
- 6.4 table/dialog a11y decision recorded in plan (MudTable has no aria-label splat on `<table>`; MudBlazor dialogs native role=dialog + Escape — documented, no code change).
Gate green: dashboard bUnit 179/179, E2E 18/18, builds 0 warnings.
Next: commit wave 3 (READMEs + SlowNetworkTests + plan/STATUS/sess-main), then root README + migration guide (shared zone), PWA/lazy/infinite-scroll 6.5.

## Touched files (uncommitted in repo root — OFF-LIMITS for other sessions)
- opencode/addBlazorFrontends/live/sess-main.md (this file)
- opencode/addBlazorFrontends/STATUS.md (Phase 6 row + next task)
- opencode/addBlazorFrontends/Phase-06-Polish-And-Perf/plan.md (status + checkboxes + benchmark + gotchas)
- clients/dashboard-blazor/FSH.Dashboard.Wasm/README.md (new)
- clients/admin-blazor/FSH.Admin.Wasm/README.md (new)
- clients/BlazorShared/README.md (new)
- clients/dashboard-blazor/FSH.Dashboard.Wasm.E2E.Tests/SlowNetworkTests.cs (new — CDP throttled slow-network boot test)

## Notes for other sessions
- **sess-maui**: FSH.Admin.Wasm.csproj edited by Main — trim `true` + `OverrideHtmlAssetPlaceholders` removed (deployability fix, admin app code untouched). If you have a local uncommitted copy of that csproj, re-read it before staging.
- **`OverrideHtmlAssetPlaceholders=true` breaks static hosting** on both apps — do NOT re-add it (see Phase-06 plan gotchas).
- On the protocol as of 2026-08-06 03:26 (identity re-verified fresh, heartbeat live).
- Runs verify.ps1 at task boundaries — see the verify.lock convention in the readme (create live/locks/verify.lock before, remove after).
- Last landed commits: … e5bb0367 (Phase 6 wave 1) + 6b6fa334 (Phase 6 wave 2) + 70dd9af2 (hybrid fix).

## Blockers / requests to other sessions
- [x] 3.14 landed + gate green → Phase C admin palette UNPARKED (row #1 resolution covers it). Run coordination.ps1 before staging, stamp heartbeat every user turn.
- [x] Board row #2: accent/font/density sign-off posted 04:20 for sess-maui — claim task line in Phase-07 plan before starting.
