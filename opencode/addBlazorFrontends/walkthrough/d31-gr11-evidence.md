# D31 — Tenant Branding: GR11 Real-Browser Walkthrough Evidence

**Date:** 2026-08-17 · **Session:** sess-main · **Gate:** AGENTS.md GR11 (real-browser parity validation)

## What was validated

Two Playwright drivers against the **live stack** (Aspire apphost: API 7030, dashboard-react 5174, dashboard-blazor 5176):

1. **`probe-react-branding.mjs`** — react reference render probe (input inventory + attribute shapes that later locators depend on).
2. **`d31-branding-interact.mjs`** — full interactive parity driver, both apps, same real tenant account (AA/ACME/acme), same seed state:

| Check | Result |
|---|---|
| Both apps reach `/settings/branding` with theme palette rendered | PASS (39 inputs react / MudBlazor colour swatches blazor) |
| Blazor page: primary/primary-text/secondary/secondary-text chips + preview + brand-assets rows | PASS (visual + DOM) |
| React reset-to-defaults button renders (seed compare point) | PASS |
| **Dirty tracking**: change Light palette primary | react `unsaved` chip appears; blazor `unsaved` chip appears | PASS |
| **Save flow**: Save → snackbar toast (`Branding saved` — blazor, matches react save feedback) | PASS |
| **Reload persistence**: hard reload both apps → saved hex (`#123456`) survives | PASS |
| **Reset flow**: Reset-to-defaults → seed primary restored, no `unsaved` chip, no `default` chip (row kept, `isDefault=false`) | PASS (both apps) |
| Console health: 0 page errors both apps | PASS |
| Network health: 0 failed API calls (SSE `ERR_ABORTED` on navigation excluded — known SSE-stream termination) | PASS |

### Final verdict lines from run

```
PASS  blazor: unsaved chip toggles off after save + snackbar
PASS  blazor: saved value persists across reload (#123456)
PASS  blazor: reset restores seed primary ($123456 -> #2563EB)
PASS  react: reset restores seed primary
PASS  no console errors / no failed api calls
[d31-interact] ALL CHECKS PASSED
```

Plus full dashboard regression probe `probe-blazor-screens.mjs`: **37 screens** walked, all populated (only SSE aborts).

## Locator lesson (also filed in live/lessons.md)

React's shadcn `Input` emits **no `type` attribute** — `input[type="text"]` matches 0 of 39 inputs. Its palette color picker is a real `input[type="color"]`. Rule: `input[type="color"]` for palette values, `input[type="text"]`-free selectors elsewhere; never `getByRole` on inputs (react labels via aria-label).

## Artifacts

- Driver source: `walkthrough/d31-branding-interact.mjs` (checked in, replayable)
- Screenshots: `walkthrough/evidence/` per-run timestamped folders (blazor-branding-*, react-*)
- Logs: `walkthrough/logs/`
- bUnit guard: `SettingsBrandingPageTests` 7/7 · full dashboard suite **242/242** · build 0 warnings (`TreatWarningsAsErrors` clean)

## Verdict

**PARITY CONFIRMED** — D31 branding screen meets GR11: real clicks, real navigation, visual + console + network inspection on both apps, with unit-test guard. React minimum parity achieved (dirty chip, save snackbar, reload persistence, reset semantics all mirrored).