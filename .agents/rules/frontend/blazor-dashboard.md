# Blazor — dashboard app (`clients/dashboard-blazor`)

Tenant-facing app. Read `blazor-shared.md` first; this file covers only the divergences.

- **Port** 5176 · proxy target `http://localhost:5030` · localStorage prefix `fsh.dashboard.*` · login header `X-FSH-App: dashboard`.

## No route-level permission gating

Dashboard has a flatter permission model. Use `<FshPermissionGate>` at the component level only. Server 403 is the backstop — no `permissionsHydrated` flash prevention is needed.

## SSE integration

- `SseService` (in `BlazorShared/Sse/`) is wired in the dashboard's `Program.cs` only.
- Wire in `App.razor`:
  ```csharp
  protected override async Task OnInitializedAsync()
  {
      _sseService = ScopedServices.GetRequiredService<SseService>();
      _sseSubscription = _sseService.Messages.Subscribe(msg => HandleSseEvent(msg));
      await _sseService.StartAsync(CancellationToken.None);
  }
  ```

## Impersonation

- Detect impersonation from JWT claims: `act_sub`, `act_tenant`, `act_name`.
- `ITokenStore` exposes `BeginImpersonation(stashedToken)` / `StopImpersonation()` that stash/restore the operator's tokens.
- `EndImpersonation` endpoint mints fresh operator tokens (no refresh token for impersonation sessions).
- Cross-app handoff: admin opens `{dashboardUrl}/login#impersonation:{token}` — the hash is read in `App.razor` via JS interop (`window.location.hash`).

## Terminal pages

- `@page "/tenant-deactivated"` — navigated to on 403 with tenant-deactivated problem detail.
- `@page "/impersonation-ended"` — navigated to on 401 with impersonation-revoked.
- These routes are **outside** `AppShell` and `AuthorizeView`.

## AppShell layout

```
MudLayout
├── MudAppBar (top bar: logo, command palette (MudAutocomplete), notification bell, user menu)
├── MudDrawer (sidebar: MudNavMenu — flatter, grouped sections)
│   └── Overview, Activity, Subscription, Wallet, Invoices
│       Catalog, Identity, Tickets, Chat, Files, System, Settings
└── MudMainContent
    └── @Body (with per-route Loading skeleton)
```

## Command palette

- `MudAutocomplete` in the `MudAppBar` that searches across all pages.
- Triggered by `Ctrl+K` via JS interop `window.addEventListener("keydown", ...)`.
- Source: `INavService.GetAllRoutes()` returns searchable list of `{label, path, icon}`.

## Accent color switcher

Dashboard supports swappable accent colors. `MudTheme` palette is rebuilt dynamically:

```csharp
public void SetAccentColor(string accent)
{
    _currentAccent = accent switch
    {
        "rose" => RosePalette,
        "indigo" => IndigoPalette,
        // ...
    };
    _ = InvokeAsync(StateHasChanged);
}
```

## Add-a-page deltas (on top of blazor-shared steps)

- No `@attribute [Authorize(Policy = "...")]` on pages — auth-only via `@attribute [Authorize]`.
- No `FshPermissionGate` for route-level checks.
- Wrap page in `<MudSkeleton>` loading state via `_isLoading` property.
- No permission constant mirroring needed.
- SSE/subscription setup in `OnInitializedAsync` for live-data pages.
