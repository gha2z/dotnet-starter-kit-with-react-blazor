# React → Blazor Migration Guide

The playbook for porting a page from the React apps (`clients/admin`, `clients/dashboard`)
to the Blazor WASM twins (`clients/admin-blazor`, `clients/dashboard-blazor`). Follow this
for every new page so the two front-ends stay in parity.

## Concept map

| React | Blazor WASM (this repo) |
|---|---|
| Page component `src/pages/…tsx` | Razor page in `FSH.Admin.Pages/Pages/…` or `FSH.Dashboard.Pages/Pages/…` with `@page "/route"` + `@attribute [Authorize]` |
| Route in `src/router.tsx` (lazy) | Route discovery is automatic (`@page` directive); pages live in a **lazy-loaded RCL** (`FSH.Admin.Pages` / `FSH.Dashboard.Pages`) |
| API module `src/api/…ts` (`apiFetch`) | Service in `FSH.BlazorShared/Services/*Service.cs` (+ `I*Service`) — hand-written, `HttpClient` injected, same `/api/v1/…` endpoints |
| TanStack Query `useQuery` | `OnInitializedAsync` + local `_loading` / `_items` / `_error` state, `StateHasChanged` after awaits |
| TanStack Query `useMutation` | Method on the injected service called from a button `OnClick`; refresh the list by re-invoking the load |
| Zod schema + react-hook-form | `MudForm` + `MudTextField`s + validation (`MudForm.Validate()`, `HasErrors`) — see `implement-blazor-form` skill |
| Permission guard `RouteGuard` | `@attribute [Authorize(Policy = "Permissions.X.Y")]` or `<FshPermissionGate Permission="…">` |
| Error state / toast | `FshErrorBand` (inline) or `MudSnackbar` (transient); wrap lists with `<FshErrorBoundary>` at the app shell |
| `useTheme` | `FshThemeService` (injectable singleton, `IsDarkMode`, `SetModeAsync`) |
| shadcn `Card`/`Button` etc. | `MudPaper`/`MudCard`, `MudButton`, `MudText`, `MudIcon` — see the component table in `clients/BlazorShared/README.md` |

## Checklist per page

1. **Service first** — add `I{Name}Service` + `{Name}Service` in `FSH.BlazorShared/Services/`
   (records for DTOs live in `FSH.BlazorShared/Models/…`). Register in **both** apps'
   `Program.cs` (`AddScoped<I…, …>()`).
2. **Page** — `@page "/route"`, `@attribute [Authorize]`, `FshPageHeader`, list/detail layout
   mirroring the React screen; reuse `FshPager`, `FshEmptyState`, `FshFilterBar`, `FshStatusPill`.
3. **Route + nav** — dashboard: nothing to register (lazy RCL auto-discovers), but add the item
   to `NavSpec.cs` in the WASM project; admin: same, plus the authorization policy exists already
   in `Program.cs` (mirror of the server permission).
4. **Loading/error states** — `_loading` skeleton rows (`fsh-skeleton`), `_error` → `<FshErrorBand Message="@_error" />`.
5. **Tests** — bUnit page tests in `FSH.Admin.Wasm.Tests` / `FSH.Dashboard.Wasm.Tests`
   (substitute the service, assert list render + empty + error states).
6. **Accessibility** — buttons need `aria-label` when icon-only; list rows are clickable with a
   real link/button or keyboard handler (`@onkeydown`).

## Conventions (non-negotiable)

- Services return `Task<T>` with `CancellationToken ct = default`; `ConfigureAwait(false)` on awaits.
- `public sealed class` handlers/services; records for DTOs; file-scoped namespaces.
- No string interpolation in log messages (message templates only).
- Never reference a module's runtime project from another module — contracts only (BlazorShared
  is the shared shell for both WASM apps; it is **Main-stream-owned**).
- Styling: `fsh.css` classes + MudBlazor CSS variables (theme-driven), not hard-coded colors.
- localStorage keys are per app: `fsh.admin.*` (admin), `fsh.dashboard.*` (dashboard).

## Real-time / offline parity

- SignalR notifications: `IHubConnectionService` (bell in the topbar, already wired).
- SSE (dashboard): `ISseService` + `SseService`.
- Offline banner: `<FshOfflineBanner />` (both layouts already include it).
- 429 resilience: `RetryAfterHandler` is registered in both `FSH.Api` chains — no per-page work.

## Where to look

- `clients/BlazorShared/README.md` — component library + areas
- `opencode/addBlazorFrontends/Phase-06-Polish-And-Perf/plan.md` — phase status and gotchas
- `opencode/addBlazorFrontends/live/sess-main.md` — current stream state (heartbeat, waves)
