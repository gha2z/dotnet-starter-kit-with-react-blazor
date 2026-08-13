---
name: add-permission-csharp
description: Mirror a backend permission constant to C# + register the authorization policy. Use when the server adds a new endpoint permission. See .agents/rules/frontend/blazor-admin.md.
argument-hint: "[Module] [Resource] [Action]"
---

# Add Permission (C#)

## Step 1 — Add the constant

```csharp
// BlazorShared/Permissions/{Module}Permissions.cs
public static class {Module}Permissions
{
    public static class {Resources}
    {
        public const string View = "Permissions.{Resources}.View";
        public const string Create = "Permissions.{Resources}.Create";
        public const string Edit = "Permissions.{Resources}.Edit";
        public const string Delete = "Permissions.{Resources}.Delete";
        public const string Export = "Permissions.{Resources}.Export";
    }
}
```

## Step 2 — Register the policy

```csharp
// FSH.Admin.Wasm/Program.cs
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy({Module}Permissions.{Resources}.View,
        policy => policy.RequireClaim("permission", {Module}Permissions.{Resources}.View));
    options.AddPolicy({Module}Permissions.{Resources}.Create,
        policy => policy.RequireClaim("permission", {Module}Permissions.{Resources}.Create));
    option.AddPolicy({Module}Permissions.{Resources}.Edit,
        policy => policy.RequireClaim("permission", {Module}Permissions.{Resources}.Edit));
});
```

## Step 3 — Apply to page

```razor
@attribute [Authorize(Policy = "{Module}Permissions.{Resources}.View")]
```

## Step 4 — Component-level gate

```razor
<FshPermissionGate Perms="@(new[] { {Module}Permissions.{Resources}.Create })">
    <MudButton @onclick="OpenCreate">Create</MudButton>
</FshPermissionGate>
```

## Step 5 — Nav gating

In `NavService`, add the permission requirement to the nav entry so `MudNavMenu` items are hidden for unauthorized users.

## Validation

- [ ] Constant matches the server permission string exactly
- [ ] Policy registered before any page uses it
- [ ] `@attribute [Authorize]` applied to pages
- [ ] Nav item hidden for users without permission
- [ ] `FshPermissionGate` correctly hides/shows guarded content
