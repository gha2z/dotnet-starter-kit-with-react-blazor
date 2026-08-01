# Blazor — admin app (`clients/admin-blazor`)

Operator/SuperAdmin console. Read `blazor-shared.md` first; this file covers only the divergences.

- **Port** 5175 · proxy target `http://localhost:5030` · localStorage prefix `fsh.admin.*` · login header `X-FSH-App: admin`.

## Env

Runtime config (`/config.json`): `{ apiBase, defaultTenant, dashboardUrl }`. `dashboardUrl` is used for the one-way impersonation handoff into the dashboard app (opens new browser tab).

## Forms — MudForm with DataAnnotations

Admin Blazor forms use `MudForm` + `EditForm` + data annotations:

```razor
<MudForm @ref="_form" Model="@_model" Validation="@(new DataAnnotationsValidator())">
    <MudTextField @bind-Value="_model.Email"
                  Label="Email"
                  For="@(() => _model.Email)"
                  Required="true"
                  RequiredError="Email is required." />
    <MudTextField @bind-Value="_model.Password"
                  Label="Password"
                  InputType="InputType.Password"
                  For="@(() => _model.Password)" />
    <MudButton ButtonType="ButtonType.Submit"
               Variant="Variant.Filled"
               Color="Color.Primary"
               Disabled="@_isSubmitting">
        Save
    </MudButton>
</MudForm>
```

Model classes use `[Required]`, `[StringLength]`, `[EmailAddress]`, `[Compare]`, etc. For cross-field validation (e.g., password match), implement `IValidatableObject`.

Submit pattern:
```csharp
private async Task SubmitAsync()
{
    await _form.Validate();
    if (_form.IsValid)
    {
        _isSubmitting = true;
        try { await _service.CreateAsync(_model); /* success toast + close */ }
        catch (ApiRequestException ex) { /* show error */ }
        finally { _isSubmitting = false; }
    }
}
```

## Permissions — three gating layers

1. **Auth guard at route level**: `@attribute [Authorize]` on all protected pages; `[Authorize(Policy = "Permissions.X.View")]` where an entire surface is permission-gated.
2. **Component-level gate**: `<FshPermissionGate Perms="@(new[] { Permissions.Users.View })">` renders content only if user has the permission.
3. **Nav item filter**: `MainLayout` mirrors `clients/admin/src/components/layout/nav-items.ts` 1:1 —
   **full React parity policy** (supersedes the old "remove dead links" rule): every React menu entry
   exists in Blazor, permission-gated via the principal's `permission` claims; entries whose pages are
   not built yet stay in the menu and land on the 404 page until their phase lands. The layout
   **subscribes to `AuthenticationStateChanged`** and recomputes on every change — a layout instance
   survives login/logout, so a one-shot `OnInitializedAsync` computation goes stale and leaves the nav
   empty after login.

Permissions are **fetched** from `GET /api/v1/identity/permissions` (not from the JWT — the JWT only carries roles). `AuthStateProvider` hydrates them into the principal as `permission` claims via `IPermissionsProvider` (memory → localStorage `fsh.admin.permissions` → endpoint) and maps `role`/`roles` → `ClaimTypes.Role`. `Program.cs` warms the cache **before the first render** so the initial auth state already passes policy checks — a cold-cache first render is the classic "logged in but every route bounces to /login" bug. `RedirectToLogin` preserves `returnUrl` and shows a 403 surface (not a redirect) when the user is already authenticated but lacks the permission. `AuthStateProvider.RefreshAsync()` re-fetches claims after a role permission change.

### Permission constants

Mirror server permissions by hand in `BlazorShared/Permissions/`:

```csharp
public static class IdentityPermissions
{
    public static class Users
    {
        public const string View = "Permissions.Users.View";
        public const string Create = "Permissions.Users.Create";
        public const string Edit = "Permissions.Users.Edit";
        public const string Delete = "Permissions.Users.Delete";
        public const string Export = "Permissions.Users.Export";
    }
    public static class Roles
    {
        public const string View = "Permissions.Roles.View";
        // ...
    }
}
```

## Routing

- `@page "/{area}"` — e.g., `@page "/users"`, `@page "/users/{Id:guid}"`.
- All pages under `AppShell` (which wraps `<AuthorizeView>`).
- `NotFound` route at `@page "/404"` with redirect from wildcard `@page "/{*}"`.

## AppShell layout

Custom shell (React-19 parity — no `MudLayout/MudDrawer/MudAppBar`):

```
<div class="d-flex">                       // height:100vh, overflow:hidden
├── <aside class="fsh-sidebar">            // 240px | 52px collapsed, persisted (fsh.admin.sidebar.collapsed = "true"/"false")
│   ├── brand row (mark + wordmark, collapse toggle)
│   ├── <nav> accordion sections           // FshNavSection per section: header + animated body;
│   │                                      //   collapsed mode = flat icon stack with title tooltips
│   │   Overview (top, outside sections)
│   │   Tenants  · Identity (Users, Roles, Impersonation) · Operations (Billing, Webhooks, Audits, Health)
│   │   Settings (bottom, outside sections)
│   │   // single-select accordion, re-synced from the current route on navigation
│   └── footer (expand toggle / version line)
├── <div class="d-flex flex-column">
│   ├── <header class="fsh-topbar">        // mobile menu, FshNotificationBell, theme toggle (FshThemeService), user menu (MudMenu)
│   └── <main> @Body
└── <div class="fsh-sidebar-backdrop">     // mobile drawer backdrop (below 900px)
```

### Nav parity table (source of truth: `clients/admin/src/components/layout/nav-items.ts`)

| React section | Items | Blazor page | Perm (claim) |
|---|---|---|---|
| Top | Overview | `/` (exists) | — |
| Tenants | Tenants | `/tenants` (exists) | `Tenants.View` |
| Identity | Users | `/users` (exists) | `Users.View` |
| Identity | Roles | `/roles` (exists) | `Roles.View` |
| Identity | Impersonation | `/impersonation` (**pending**) | `Impersonation.View` |
| Operations | Billing | `/billing` (**pending**) | `Billing.Subscriptions.View` |
| Operations | Webhooks | `/webhooks` (**pending**) | `Webhooks.View` |
| Operations | Audits | `/audits` (**pending**) | `Auditing.View` |
| Operations | Health | `/health` (**pending**) | `Health.View` |
| Bottom | Settings | `/settings` (**pending**) | `Settings.View` |

Items marked **pending** stay in the menu (full-parity policy) and render the 404 page until built.
Sections render only if at least one item passes its permission filter.

## Add-a-page deltas (on top of blazor-shared steps)

- Use `MudForm` + data annotations for any form.
- Add permission constant to `BlazorShared/Permissions/`.
- Apply `@attribute [Authorize(Policy = "Permissions.{Resource}.View")]` on the page.
- Wrap guarded elements in `<FshPermissionGate>`.
- Register the policy in `Program.cs`: `options.AddPolicy("Permissions.Users.View", policy => policy.RequireClaim("permission", "Permissions.Users.View"));`

## Realtime

- `HubConnectionService` lifecycle is tied to auth state: connect after login, disconnect on logout.
- **`FshNotificationBell`** in the topbar subscribes to `NotificationCreated` (server pushes to
  `user:{userId}` on `AppHub`), shows the unread count (cap 99+), lists the latest 20, marks read on
  click (and navigates the link), and offers mark-all-read. Unread count/list/actions via
  `INotificationService`. Requires `NotificationPermissions.Inbox.View` / `MarkRead`.
- Notifications can also surface as `MudAlert` toasts via `MudSnackbar`.
