using FSH.BlazorShared.Permissions;
using MudBlazor;

namespace FSH.Admin.Wasm.Shared;

/// <summary>
/// Single source of nav destinations for the sidebar and the command palette.
/// Mirrors the React admin app (clients/admin/src/components/layout/nav-items.ts).
/// </summary>
public static class NavSpec
{
    public sealed record NavItem(string Href, string Icon, string Label, string? Permission, IReadOnlyList<string>? AnyPermissions);

    public sealed record NavSection(string Id, string Caption, string Icon, IReadOnlyList<NavItem> Items);

    public static readonly NavItem[] TopItems =
    [
        new("/", Icons.Material.Filled.SpaceDashboard, "Overview", null, null),
    ];

    public static readonly NavItem[] BottomItems =
    [
        new("/settings", Icons.Material.Filled.Settings, "Settings", null, null),
    ];

    public static readonly NavSection[] Sections =
    [
        new("multitenancy", "Tenants", Icons.Material.Filled.Business,
        [
            new("/tenants", Icons.Material.Filled.Business, "Tenants", MultitenancyPermissions.Tenants.View, null),
        ]),
        new("identity", "Identity", Icons.Material.Filled.People,
        [
            new("/users", Icons.Material.Filled.People, "Users", IdentityPermissions.Users.View, null),
            new("/roles", Icons.Material.Filled.AdminPanelSettings, "Roles", IdentityPermissions.Roles.View, null),
            new("/impersonation", Icons.Material.Filled.ManageAccounts, "Impersonation", IdentityPermissions.Impersonation.View, null),
        ]),
        new("operations", "Operations", Icons.Material.Filled.Insights,
        [
            new("/billing", Icons.Material.Filled.Receipt, "Billing", BillingPermissions.View, null),
            new("/webhooks", Icons.Material.Filled.DeviceHub, "Webhooks", WebhooksPermissions.Subscriptions.View, null),
            new("/audits", Icons.Material.Filled.Article, "Audits", AuditingPermissions.AuditTrails.View, null),
            new("/health", Icons.Material.Filled.MonitorHeart, "Health", null, null),
        ]),
    ];
}
