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

1. **Auth guard at route level**: `@attribute [Authorize]` on all protected pages.
2. **Component-level gate**: `<FshPermissionGate Perms="@(new[] { Permissions.Users.View })">` renders content only if user has the permission.
3. **Nav item filter**: `NavService` provides permission-gated `MudNavMenu` items.

Permissions are **fetched** from `GET /api/v1/identity/permissions` (not from JWT). `AuthStateProvider` hydrates them after login and caches in localStorage under `fsh.admin.permissions`.

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

```
MudLayout
├── MudAppBar (top bar: logo, search, theme toggle, user menu)
├── MudDrawer (sidebar: MudNavMenu with permission-gated items)
│   └── MudNavMenu → MudNavLink per module (Tenants, Identity, Billing, Webhooks, Audits, Notifications, Health, Settings)
└── MudMainContent
    └── @Body (page content)
```

## Add-a-page deltas (on top of blazor-shared steps)

- Use `MudForm` + data annotations for any form.
- Add permission constant to `BlazorShared/Permissions/`.
- Apply `@attribute [Authorize(Policy = "Permissions.{Resource}.View")]` on the page.
- Wrap guarded elements in `<FshPermissionGate>`.
- Register the policy in `Program.cs`: `options.AddPolicy("Permissions.Users.View", policy => policy.RequireClaim("permission", "Permissions.Users.View"));`

## Realtime

- `RealtimeHubConnection` wired for `["NotificationCreated"]` events only.
- Notifications appear as `MudAlert` toasts via `MudSnackbar`.
- Badge count on notifications nav item updates in real-time.
