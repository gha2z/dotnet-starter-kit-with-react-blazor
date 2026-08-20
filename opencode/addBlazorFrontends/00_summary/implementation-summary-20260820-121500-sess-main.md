# Implementation Summary 2026-08-20 12:15

---
**Description:** Post-Phase-9 wave: SW stale-build guard v2.1 (both apps) + chat rail dead-space fix + D32 notifications-bell parity (committed) + chat fixes
**Creator:** opencode (model: opencode/deepseek-v4-flash-free, verified fresh this session)
**Duration:** ~3h across two sessions (SW guard + chat rail; D32 committed `da4be27e`)
---

## SW stale-build guard v2.1 + Chat rail + D32
---
- Step 1: **SW guard v2.1 (admin-blazor + dashboard-blazor)** — `wwwroot/index.html` + `service-worker.js` updated to capture-and-verify a runtime version at claim time, with stale-build detection → reload-with-retry → self-unregister fallback. User-confirmed working after a full cache clear (stale-hash `.pdb` 404s gone).
- Step 2: **D32 notifications-bell parity** — dashboard-blazor `/settings/notifications` now exposes React's "Open notifications bell" button (commit `da4be27e`).
- Step 3: **Chat rail dead-space fix** — root cause: MudBlazor 9.7 `MudDivider`'s DEFAULT `DividerType.FullWidth` renders `.mud-divider-fullwidth` with `flex-grow:1`, so inside the rail's flex column the divider stretched to fill ALL remaining height (322px gap under the search box; DM section below the fold). Fix: replaced `MudDivider` with a deterministic `<hr class="fsh-chat-divider">` (`border-top` + `flex-shrink:0`), added `min-height:0` to the `MudItem` rail + messages column, and made `.fsh-chat-root/.fsh-chat-rail/.fsh-chat-channel-list/.fsh-chat-messages` heights explicit in `fsh.css`. MUD0002 note: `FullWidth`/`Fullwidth` bool params are illegal — the real param is the `DividerType` enum.
- Step 4: Chat fixes carried this wave: `NewDmDialog.razor` (new, untracked), avatar initials, DM list + message list rendering.
- Step 5: Rebuilt dashboard-blazor, restarted DevServer with CWD = Wasm project dir + `ASPNETCORE_URLS` (repo-root CWD makes it fall back to port 5000), real-browser walkthrough verified via `walkthrough/diag-chat-rail.mjs` computed rects: divider 322px→1px, channel list top 539→218, DM section fully visible.

## Verification
---
- dashboard-blazor build: **0 warnings / 0 errors**
- admin-blazor build: **Build succeeded** (pre-existing MUD0002 analyzer warnings in FSH.Admin.Pages dialogs — untouched this wave, see Known Limitations)
- dashboard bUnit suite: **256/256 passed, 0 failed, 0 skipped** (36s)
- Real-browser walkthrough (GR 11): chat rail rects verified before/after via Playwright driver; SW v2.1 verified live by user after cache clear

## Known Limitations
---
- SW guard v2.1 + chat rail + chat fixes are **UNCOMMITTED** — commit only on user approval (track policy).
- admin-blazor build shows 6 pre-existing MUD0002 analyzer warnings (`MaxWidth`/`FullWidth`/`GutterSize` on MudDialog/MudGrid in UserCreateDialog/RoleCreateDialog/TenantCreateDialog) — pre-existing files, not introduced by this wave; follow-up candidate.
- DevServer must be restarted after rebuilds (in-memory `blazor.boot.json` cache → stale-hash 404s) and launched with CWD = project dir.

## Lessons / Process Improvements
---
- MudBlazor 9.7 `MudDivider` default `DividerType.FullWidth` = `flex-grow:1` — never use it inside a flex column for a thin separator; use a plain `<hr>` with deterministic CSS. Proof: computed-rect diff (322px → 1px).
- Restarting the Blazor DevServer from repo root CWD falls back to port 5000 (launchSettings.json lookup fails) — launch with CWD = the Wasm project dir + `ASPNETCORE_URLS`, and always restart after a rebuild (boot manifest cached in memory).
- Real-browser geometry bugs (unused space, below-fold content) are only catchable via computed-rect assertions in the walkthrough driver, not bUnit.