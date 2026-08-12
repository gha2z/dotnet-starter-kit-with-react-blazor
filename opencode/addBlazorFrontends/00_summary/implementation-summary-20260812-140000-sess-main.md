## Wave Summary

**Wave:** W1 — Theme instant-switch fix (Phase 09, user-approved defect)

**Creator:** opencode (model: auto/coding — unresolved)

**Duration:** ~2h (investigation + fix + probe + verification)

### What was done

- **Root cause found + fixed** (`323d1060`): MudBlazor's `ObserveSystemDarkModeChange` defaults to **true**, giving `MudThemeProvider` its own JS `matchMedia` OS-watcher — a second dark-mode resolver whose `SystemDarkModeChangedAsync` callback overrides the app's `IsDarkMode` parameter state and fires `IsDarkModeChanged` → `FshThemeService.SetModeAsync`, silently rewriting the user's stored mode on OS scheme changes. Disabled on both `MudThemeProvider`s in `App.razor` (admin + dashboard); `FshThemeService` is now the single source of truth (React parity).
- **Regression probe** (`7b39a093`): added `MudThemeProviderReapplyProbeTests` — renders the real MudBlazor 9.7 provider with the exact `App.razor` wiring (theme instance + `IsDarkMode` parameter) and asserts the `<style>` block regenerates when the mode flips. Passes.
- **Verification (fresh runs):** dashboard **233/233** bUnit + admin **164/164** bUnit, both apps 0 warnings.

### Files touched

| File | Change |
|------|--------|
| `clients/BlazorShared/Theming/FshThemeService.cs` | No change (service layer correct) |
| `clients/admin-blazor/FSH.Admin.Wasm/App.razor` | Added `ObserveSystemDarkModeChange="false"` to `MudThemeProvider` |
| `clients/dashboard-blazor/FSH.Dashboard.Wasm/App.razor` | Added `ObserveSystemDarkModeChange="false"` to `MudThemeProvider` |
| `clients/dashboard-blazor/FSH.Dashboard.Wasm.Tests/Theming/MudThemeProviderReapplyProbeTests.cs` | New regression probe |

### Verification

- Build: both WASM apps compile with 0 errors, pre-existing MUD0002 warnings only
- Test suite (dashboard): 233/233 passed
- Test suite (admin): 164/164 passed
- Probe: `MudThemeProviderReapplyProbeTests.Style_Block_Regenerates_When_Mode_Flips` — PASS (style block text changes when `IsDarkMode` parameter flips via the full `Service → Changed → App → Provider` chain)

### Wave DAG

```
W1: [323d1060 fix + 7b39a093 probe] → verified → committed
```

## Lessons / Process Improvements

- **Offset-tunneling triggers the PRM hard stop.** Context management drops `read` outputs, causing re-read loops. Fix: batch whole-file reads (the `read` tool defaults to 2000 lines) and edit immediately. Never re-read a file you just read — if it was dropped, use `grep` with context for the specific anchor, not another `read` with offset.
- **The probe must mirror the real App.razor wiring exactly** (including `IsDarkModeChanged` two-way binding) or it's an unfaithful mirror. The first probe iteration omitted `IsDarkModeChanged="@OnThemeChanged"` and passed — which would have hidden the real defect if the two-way loopback were the cause.
- **MudBlazor 9 defaults are a trap for single-source-of-truth architectures.** `ObserveSystemDarkModeChange=true` means MudThemeProvider independently manages dark mode via JS, fighting any app-level state manager. Default it to `false` whenever the app owns the theme resolution (as `FshThemeService` does).
