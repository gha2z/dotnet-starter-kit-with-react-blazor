# Implementation Summary 2026-Aug-06 09:40

---
**Description:** Phase 6 wave 4 — dashboard lazy loading (Pages RCL split + .NET 10 lazy-assembly wiring) + pre-existing Release-publish blocker investigation
**Creator:** opencode (auto/coding, model: deepseek-v4-flash-free)
**Duration:** ~4h (incl. deep publish/runtime investigation)
---

## Phase 6 - 6.1b Lazy Loading (dashboard) + 6.9 Release-publish blocker
---
- Step 1: Diagnosed the .NET 10 lazy-load pipeline end-to-end: `BlazorWebAssemblyLazyLoad` item → `_GenerateBuildWasmBootJson` (pack targets `Microsoft.NET.Sdk.WebAssembly.Browser.targets` 10.0.10, L407/L821) → boot config **`resources.lazyAssembly`** (singular array; the old top-level `lazyAssemblies` key no longer exists) inlined in `_framework/dotnet.js`.
- Step 2: Fixed runtime 404: both the csproj lazy item and `LazyAssemblyLoader.LoadAssembliesAsync` must use **`.wasm`** (`.dll` never matches the .NET 10 resources map).
- Step 3: Fixed route discovery: `App.razor` Router now has `AdditionalAssemblies="@_lazyAssemblies"` (loaded assemblies captured from `LoadAssembliesAsync`); without it lazy routes are never discovered → 404 page with zero console errors.
- Step 4: Structure: all dashboard `Pages/` + `Shared/TerminalLayout.razor` moved (git mv) into new RCL `clients/dashboard-blazor/FSH.Dashboard.Pages`; `FSH.Dashboard.Wasm.csproj` gained the lazy item + `TrimmerRootAssembly` + RCL/BlazorShared references; `src/FSH.Starter.slnx` updated.
- Step 5: Verified publish structure: trimmed Release publish defers Pages (`resources.lazyAssembly = FSH.Dashboard.Pages.wasm -> FSH.Dashboard.Pages.75lshaem5b.wasm`, absent from eager `resources.assembly`); **20.34 MB total / 4.57 MB gz** vs wave-2 22.29 / 4.97; lazy file 645.8 KB (198.5 KB gz).
- Step 6: **6.9 investigation — Release publish crashes**: `MONO interpreter: NIY encountered in method Microsoft.Extensions.Localization.LocalizationOptions:.ctor ()` → `Assertion: should not be reached at interp.c:4135` (MINT_NIY; only emit site in runtime source is transform-simd.c for unimplemented `PackedSimd` calls — trivial BCL ctor contains none; mechanism upstream). Exonerated: trimming (untrimmed reproduces), webcil, jiterpreter (`BlazorWebAssemblyJiterpreter=false`), stale obj (clean worktree + clean bin/obj reproduce), **lazy refactor** (clean `a9c78f86` worktree publish fails identically; admin-blazor Release publish fails identically — silent death with zero console output in non-lazy shapes). Debug/DevServer (E2E/bUnit pipeline) fully green.
- Step 7: Upstream tracking: dotnet/runtime #121849 (open, milestone 12.0.0 — same NIY class; reporter's delete-bin/obj fix did not help us), related #121840/#121738; MudBlazor .NET 10 target unshipped (#12049). Filed as plan task 6.9.

## Verification
---
- E2E **18/18 PASSED** (1 m 59 s) — `FSH.Dashboard.Wasm.E2E.Tests`
- bUnit **179/179 PASSED** (4 s) — `FSH.Dashboard.Wasm.Tests`
- App build 0 warnings / 0 errors (Debug)
- Trimmed Release publish: 252 files, 20.34 MB total / 4.57 MB gz; `resources.lazyAssembly` = 1 entry; Pages absent from eager `resources.assembly`
- Debug boot config (served via DevServer): `resources.lazyAssembly = [FSH.Dashboard.Pages.wasm]`; lazy file requested only after navigation (2 requests during OnNavigateAsync, then renders)

## Known Limitations
---
- ⚠️ Release `dotnet publish` output of BOTH WASM apps crashes at boot (interp NIY at `LocalizationOptions:.ctor`) — **pre-existing at `a9c78f86`**, NOT introduced by wave 4. Production deploy blocked upstream (dotnet/runtime #121849, milestone 12.0.0; MudBlazor net10 target unshipped). Re-test after runtime servicing / MudBlazor net10 / AOT. Debug/DevServer pipeline unaffected.
- Lazy load is one assembly covering all Pages; per-feature lazy (admin parity item) would need N RCLs — decision recorded in plan Architecture Decisions.
- `TrimmerRootAssembly Include="FSH.Dashboard.Pages"` kept as belt-and-braces (byte-identical output with/without it — required if lazy item ever disabled, since nothing statically references Pages).
- Manual/upstream verification still pending: real CDN hosting of lazy publishes (fragment hashing verified, serving path covered by smoke probes only).
