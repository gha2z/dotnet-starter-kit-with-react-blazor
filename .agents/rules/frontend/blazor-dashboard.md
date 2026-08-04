# Blazor — dashboard app (`clients/dashboard-blazor`)

Tenant-facing app. Read `blazor-shared.md` first; this file covers only the divergences.

- **Port** 5176 · proxy target `http://localhost:5030` · localStorage prefix `fsh.dashboard.*` (theme under **`fsh.theme`**) · login header `X-FSH-App: dashboard`.

## No route-level permission gating

Dashboard has a flatter permission model. Use `<FshPermissionGate>` at the component level only. Server 403 is the backstop — no `permissionsHydrated` flash prevention is needed.

## SSE integration

- `SseService` (in `BlazorShared/Sse/`) is wired in the dashboard's `Program.cs` only (scoped, resolves the `FSH.Api` HttpClient).
- **Two-step auth flow** — browsers' `EventSource` cannot send an `Authorization` header, so the server issues a short-lived opaque token first:
  1. `POST /api/v1/sse/token` (JWT-authenticated) → `{ token }` (single-use, 30s TTL).
  2. `GET /api/v1/sse/stream?token={guid}` (anonymous endpoint, consumes the token) → `text/event-stream`, 15s heartbeat.
- **Lifecycle is auth-driven** — dashboard `App.razor.cs`:
  - Subscribe to `Messages` **once** in `OnInitializedAsync`.
  - Connect only when `ITokenStore.GetAccessTokenAsync()` is non-null: at startup (persisted session) and on `TokensChanged` (login/refresh). A re-entrancy flag prevents double starts.
  - `StopAsync()` when tokens are cleared (logout).
  - `SseService` auto-reconnects on stream drop with capped backoff (1s → 2s → … → 30s, reset on success).

## Impersonation

- Detect impersonation from JWT claims: `act_sub`, `act_tenant`, `act_name`.
- `ITokenStore` exposes `BeginImpersonation(stashedToken)` / `StopImpersonation()` that stash/restore the operator's tokens.
- `EndImpersonation` endpoint mints fresh operator tokens (no refresh token for impersonation sessions).
- Cross-app handoff: admin opens `{dashboardUrl}/login#impersonation:{token}` — the hash is read in `App.razor` via JS interop (`window.location.hash`).

## Terminal pages — PENDING (Phase 7, gap 3.15)

The React app has `/tenant-deactivated` (navigated on 403 with tenant-deactivated problem detail) and
`/impersonation-ended` (navigated on 401 with impersonation-revoked). **These Blazor pages do not
exist yet** — when built, they must be routed **outside** `AppShell` and `AuthorizeView`. Until then
the auth-flow code simply renders them as 404s.

## AppShell layout

Custom shell (React parity — no `MudLayout/MudDrawer/MudAppBar`), same `fsh.css` shell as admin:

```
<div class="d-flex">                       // height:100vh, overflow:hidden
├── <aside class="fsh-sidebar">            // 240px | 52px collapsed, persisted (fsh.sidebar.collapsed = "true"/"false")
│   ├── brand row (mark + wordmark, collapse toggle)
│   ├── <nav> accordion sections           // FshNavSection per section (mirrors nav-data.ts 1:1)
│   │   Top:    Overview · Chat · My Files
│   │   Operations: Live activity · Subscription · Wallet · Invoices
│   │   Catalog: Products · Brands · Categories
│   │   Helpdesk: Tickets
│   │   Identity: Users · Roles · Groups
│   │   System:  Health · Audit trail · Sessions · Trash
│   │   Settings (bottom, outside sections)
│   └── footer (expand toggle / version line)
├── <div class="d-flex flex-column">
│   ├── <header class="fsh-topbar">        // mobile menu, FshNotificationBell,
│   │                                      //   theme menu (Light/Dark/System), user menu
│   └── <main> @Body (with per-route Loading skeleton)
└── <div class="fsh-sidebar-backdrop">     // mobile drawer backdrop (below 900px)
```

- **Nav parity:** `MainLayout` mirrors `clients/dashboard/src/components/layout/nav-data.ts` 1:1 —
  full-parity policy: every React entry exists, `perm` AND `anyPerm` semantics via `CanSee`; entries
  whose pages are not built yet stay and land on the 404 page until their phase lands. Sections render
  only if at least one item passes.
- **Theme menu:** Light/Dark/System (`ThemeMode`) persisted under `fsh.theme` (default `System`,
  OS-following) — React parity. Use `SetModeAsync`; render the current mode with an inline check mark
  (MudBlazor 9.7 has no `MudMenuItem.EndIcon`).
- **SSE status:** the user menu shows a live status row with `.fsh-sse-dot` bound to
  `SseService.ConnectionChanged` (`connected` class when `IsConnected`).

## Command palette / accent switcher — DEFERRED

The React apps have a command palette (Ctrl+K) and an accent-color/font/density settings menu.
These are **not yet implemented** in Blazor (deferred parity items — see `opencode/addBlazorFrontends/00-Index.md`).
`MudAutocomplete`-based palette and dynamic palette accent are the planned approaches.

## Add-a-page deltas (on top of blazor-shared steps)

- No `@attribute [Authorize(Policy = "...")]` on pages — auth-only via `@attribute [Authorize]`.
- No `FshPermissionGate` for route-level checks.
- Wrap page in `<MudSkeleton>` loading state via `_isLoading` property.
- No permission constant mirroring needed.
- SSE/subscription setup in `OnInitializedAsync` for live-data pages.

## Identity pages — permission-gated actions (since 3.7)

The identity pages (`/identity/users`, `/identity/roles`, `/identity/groups` + details) are the exception:
they wrap *in-page actions* in `<FshPermissionGate>` so only users holding the matching permission see
create/edit/delete/session controls (e.g. "Register user" behind `IdentityPermissions.Users.Create`,
the sessions panel behind `SessionsPermissions.ViewAll`, member removal behind `GroupsPermissions.Update`).
Pattern to follow for future privileged CRUD pages:
- Gate the *control*, never the route — the page renders for any authenticated user; denied actions simply
  don't render.
- Keep the gate around the smallest fragment (a button, a panel), not the whole page.
- bUnit tests assert gated content by calling `Authorization.SetAuthorized("admin")` +
  `Authorization.SetPolicies(...)` on the dashboard `TestSetup` (auth services are registered there since 3.7).
