# sess-main
identity: opencode/deepseek-v4-flash-free (concrete modelID, verified 2026-08-06) | started: 2026-08-06 | state: active | heartbeat: 2026-08-06 08:10

scope: clients/dashboard-blazor/** · clients/BlazorShared/** (Main-only) · clients/admin-blazor/** (Main-owned by default; app-code edits by other sessions need a board sign-off per task) · STATUS.md · 00-Index.md · Phase-02/03/04/06/07 plan files · opencode/addBlazorFrontends coordination docs (readme.md, verify.ps1, coordination.ps1) · implementation summaries

## Current task
M2 (Phase 6 dashboard polish/perf) — wave 2 done, wave 1 committed as `e5bb0367`:
- 6.1 trimming verified via **static-hosted publish smoke** (publish → `npx serve -s` 5180 → Playwright): splash shown, boot 3.47 s, login rendered, 0 JS errors, no error-ui.
- 6.1 **AOT benchmarked and REJECTED**: trim+AOT 56.60 MB/11.51 gz + 6.95 s boot vs trim-only 22.29/4.97 + 3.47 s — 2.5x size, 2x boot; `RunAOTCompilation` stays off (revisit for per-page AOT later).
- 6.1 **Deployability bug found + fixed on BOTH apps**: `OverrideHtmlAssetPlaceholders=true` left the literal `_framework/blazor.webassembly.js` in the published index.html while publish only emits the content-hashed name → static hosting (CDN/nginx) 404s and the app never boots; dev server masked it. Removed the flag from dashboard AND admin csproj (also set admin trimming `true`).
- 6.2 virtualization audit: all high-volume lists already server-paged; Roles/Groups bounded client-side sets (parity); Activity capped 200; Trash full-set (parity) — no changes needed.
- 6.4 icon-button audit: 17/20 already labeled; added 3 — StockDialog ±1, Chat send, Audits view-detail.
Gate green: dashboard bUnit 179/179, E2E 17/17, dashboard+admin builds 0 warnings.
Next: table/dialog-level a11y (MudTable aria-labels, dialog labelledby) + contrast spot-check, 6.7 READMEs, 6.8 load/slow-network tests.

## Touched files (uncommitted in repo root — OFF-LIMITS for other sessions)
- opencode/addBlazorFrontends/live/sess-main.md (this file)
- opencode/addBlazorFrontends/STATUS.md (Phase 6 row + next task)
- opencode/addBlazorFrontends/Phase-06-Polish-And-Perf/plan.md (status + checkboxes + benchmark + gotchas)
- clients/dashboard-blazor/FSH.Dashboard.Wasm/FSH.Dashboard.Wasm.csproj (trim true; OverrideHtmlAssetPlaceholders removed)
- clients/admin-blazor/FSH.Admin.Wasm/FSH.Admin.Wasm.csproj (trim true; OverrideHtmlAssetPlaceholders removed — deployability fix, no appearance changes)
- clients/dashboard-blazor/FSH.Dashboard.Wasm/Pages/Catalog/StockDialog.razor (aria-labels ±1)
- clients/dashboard-blazor/FSH.Dashboard.Wasm/Pages/Chat/ChatPage.razor (aria-label Send message)
- clients/dashboard-blazor/FSH.Dashboard.Wasm/Pages/System/AuditsPage.razor (aria-label View audit detail)

## Notes for other sessions
- **sess-maui**: FSH.Admin.Wasm.csproj edited by Main — trim `true` + `OverrideHtmlAssetPlaceholders` removed (deployability fix, admin app code untouched). If you have a local uncommitted copy of that csproj, re-read it before staging.
- **`OverrideHtmlAssetPlaceholders=true` breaks static hosting** on both apps — do NOT re-add it (see Phase-06 plan gotchas).
- On the protocol as of 2026-08-06 03:26 (identity re-verified fresh, heartbeat live).
- Runs verify.ps1 at task boundaries — see the verify.lock convention in the readme (create live/locks/verify.lock before, remove after).
- Last landed commits: … e5bb0367 (Phase 6 wave 1) + 70dd9af2 (hybrid fix).

## Blockers / requests to other sessions
- [x] 3.14 landed + gate green → Phase C admin palette UNPARKED (row #1 resolution covers it). Run coordination.ps1 before staging, stamp heartbeat every user turn.
- [x] Board row #2: accent/font/density sign-off posted 04:20 for sess-maui — claim task line in Phase-07 plan before starting.
