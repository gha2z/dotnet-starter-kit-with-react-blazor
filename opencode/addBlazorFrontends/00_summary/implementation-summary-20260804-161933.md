# Implementation Summary 20260804-161933

---
**Description:** Fixed the dashboard-blazor 5176 "error page instead of login dialog" bug — `FshErrorBoundary` wrapped the `<Router>` but was not a real error boundary (no `ChildContent`), silently dropping the entire app. Rewrote it as an `ErrorBoundaryBase` subclass. Consolidated the global error UX (`.fsh-error-*` CSS + `fshError.js`) into `BlazorShared` static assets and applied it to the admin app too. Updated doc/model identity to the actual LLM identifier `opencode/big-pickle`, fixed the summary filename/title, bumped the Phase-07 header, and corrected an overstated Phase-04 claim.
**Creator:** opencode (auto/coding, model: opencode/big-pickle)
**Duration:** 45m
---

## Fix - Dashboard-blazor 5176 error page (FshErrorBoundary)
---
- Step 1: Diagnosed root cause — dashboard `App.razor` wraps `<Router>` in `<FshErrorBoundary>`, but `FshErrorBoundary.razor` only had a `Message` parameter, no `ChildContent`, and no error-boundary base; the nested `<Router>` (and the whole app/login dialog) was silently dropped, leaving only the static "Something went wrong" card.
- Step 2: Rewrote `clients/BlazorShared/Components/FshErrorBoundary.razor` — `@inherits ErrorBoundaryBase`, inherited `ChildContent` renders the app when `CurrentException is null`, styled error card shows only on an actual unhandled exception; implemented `OnErrorAsync` (structured `Logger.LogError`) and kept the Reload flow.
- Step 3: Verified dashboard builds 0 warnings; dashboard suite **92/92** + admin suite **147/147** pass; restarted both WASM dev servers and confirmed http://localhost:5176 and http://localhost:5175 both render the login dialog (`/login`) with no "unhandled error" banner and no page errors.

## Fix - Consolidated global error UX via BlazorShared
---
- Step 1: Moved `wwwroot/js/fshError.js` from the dashboard app into `clients/BlazorShared/wwwroot/js/` (served at `_content/FSH.BlazorShared/js/fshError.js`); removed the dashboard-local copy.
- Step 2: Added the `.fsh-error-ui` / `.fsh-error-visible` / `.fsh-error-content` / `.fsh-error-detail` / `.fsh-reload-btn` rules to `clients/BlazorShared/wwwroot/css/fsh.css` (single source); removed the duplicates from both apps' `app.css`.
- Step 3: Updated dashboard `index.html` to reference `_content/FSH.BlazorShared/js/fshError.js`; updated admin `index.html` to add `fshError.js` and the `.fsh-error-detail` element so the admin app no longer shows the stock "An unhandled error has occurred" banner.
- Step 4: Confirmed both apps serve the shared asset from the build manifest and at runtime.

## Docs - Model identity, summary naming, header + claim corrections
---
- Step 1: Updated `readme.md` — the model-identity guidance now tells agents to fetch the real model from `GET http://localhost:20128/api/usage/call-logs?status=ok&limit=1` (login http://localhost:20128/login, password CHANGEME) and read the `requestedModel` field; example updated to `opencode/big-pickle`.
- Step 2: Updated `00-Index.md` and `Phase-03/plan.md` headers from `model: auto/coding` to `model: opencode/big-pickle` (older historical docs left at their original model per convention).
- Step 3: Renamed the 3.7 summary to `implementation-summary-20260804-093000.md` (matches `<yyyy-MM-dd-hh-mm-ss>.md` convention), fixed its title/Creator, and removed the overstated "Phase-04/plan.md updated" claim (file was never modified).
- Step 4: Bumped `Phase-07/plan.md` Last Update header to the current session.

## Docs - Status pass
---
- Step 1: Confirmed `00-Index.md` status line is correct (Phase 3 3.7 ✅, dashboard 92/92, next 3.8 Tickets) — no change needed beyond the model-identity header fix.
