# sess-main
identity: opencode/deepseek-v4-flash-free (concrete modelID, verified 2026-08-06) | started: 2026-08-06 | state: active | heartbeat: 2026-08-06 07:15

scope: clients/dashboard-blazor/** · clients/BlazorShared/** (Main-only) · clients/admin-blazor/** (Main-owned by default; app-code edits by other sessions need a board sign-off per task) · STATUS.md · 00-Index.md · Phase-02/03/04/06/07 plan files · opencode/addBlazorFrontends coordination docs (readme.md, verify.ps1, coordination.ps1) · implementation summaries

## Current task
M2 (Phase 6 dashboard polish/perf) IN PROGRESS — first wave landed:
- 6.1 trimming: `BlazorWebAssemblyEnableTrimming=true` (was false) + measured untrimmed 79.52 MB vs trimmed 22.29 MB (4.97 MB gz) — ~3.6x. **.NET 10 gotcha**: `false` alone never disabled trimming (`PublishTrimmed` defaults true) — admin-blazor csproj has the same latent config; flag to sess-maui.
- 6.2 tickets search fixed: `Immediate` field had NO reload handler (typing did nothing server-side). Added debounced `OnSearchChangedAsync` (250 ms + CTS, mirrors UsersListPage) + bUnit test.
- 6.3 branded splash (index.html: FSH mark + spinner + copy, `role="status"`).
- 6.4 shell a11y: skip-to-content link (JS interop — Blazor SPA interceptor swallows fragment nav, so `@onclick:preventDefault` + `js/fshSkipLink.js` moves focus to `main#fsh-main`), aria-labels on ViewSidebar/Menu icon buttons, nav landmark `aria-label="Primary"`.
- 6.8 mobile E2E (375×812): Overview/Users/Products no horizontal overflow + drawer trigger visible.
Gate green: dashboard bUnit 179/179, E2E 17/17 (added ShellA11yTests ×3 + MobileViewportTests ×3), build 0 warnings.
Next: virtualized lists audit + remaining a11y (tables/dialogs), trimmed-Release smoke, AOT benchmark, then docs/README.

## Touched files (uncommitted in repo root — OFF-LIMITS for other sessions)
- opencode/addBlazorFrontends/live/sess-main.md (this file)
- opencode/addBlazorFrontends/STATUS.md (Phase 6 row + next task)
- opencode/addBlazorFrontends/Phase-06-Polish-And-Perf/plan.md (status + completed checkboxes + measurements + gotchas)
- clients/dashboard-blazor/FSH.Dashboard.Wasm/FSH.Dashboard.Wasm.csproj (trimming true)
- clients/dashboard-blazor/FSH.Dashboard.Wasm/Pages/Tickets/TicketsListPage.razor(.cs) (debounced search + IDisposable + search empty-state copy)
- clients/dashboard-blazor/FSH.Dashboard.Wasm.Tests/Pages/Tickets/TicketsListPageTests.cs (+1 debounce test)
- clients/dashboard-blazor/FSH.Dashboard.Wasm/Shared/MainLayout.razor (skip link + aria-labels + main#fsh-main)
- clients/dashboard-blazor/FSH.Dashboard.Wasm/wwwroot/index.html (branded splash + fshSkipLink.js script)
- clients/dashboard-blazor/FSH.Dashboard.Wasm/wwwroot/css/app.css (splash styles + skip link)
- clients/dashboard-blazor/FSH.Dashboard.Wasm/wwwroot/js/fshSkipLink.js (new)
- clients/dashboard-blazor/FSH.Dashboard.Wasm.E2E.Tests/ShellA11yTests.cs (new, ×3)
- clients/dashboard-blazor/FSH.Dashboard.Wasm.E2E.Tests/MobileViewportTests.cs (new, ×3)

## Notes for other sessions
- **Admin csproj heads-up (sess-maui):** `BlazorWebAssemblyEnableTrimming=false` in FSH.Admin.Wasm.csproj is ineffective in .NET 10 — the app already ships trimmed (PublishTrimmed defaults true). Consider setting `true` explicitly; verified numbers in Phase-06 plan (79.52 → 22.29 MB).
- On the protocol as of 2026-08-06 03:26 (was seeded stale by sess-maui; identity re-verified fresh, heartbeat live).
- Runs verify.ps1 at task boundaries — see the verify.lock convention in the readme (create live/locks/verify.lock before, remove after).
- Last landed commits: 3.14 palette (dcf17028) + maui Phase 5 merge (bc00bea5) + admin palette Phase C (671d2f80) + board row #3 (c229c95e) + M1 E2E suite (ffe2609a).

## Blockers / requests to other sessions
- [x] 3.14 landed + gate green → Phase C admin palette is UNPARKED; you may start it (see readme admin-blazor rule: board sign-off per task — row #1 resolution covers it). Request you run coordination.ps1 before staging and stamp heartbeat every user turn.
- [x] Board row #2: accent/font/density sign-off posted 04:20 for sess-maui — claim task line in Phase-07 plan before starting.
