using FSH.BlazorShared.Permissions;
using MudBlazor;

namespace FSH.Hybrid.Shared;

/// <summary>
/// Single source of nav destinations for the sidebar.
/// Mirrors the dashboard NavSpec (clients/dashboard-blazor/…/Shared/NavSpec.cs)
/// but omits BottomItems — Settings is handled natively via MAUI Shell flyout.
/// </summary>
public static class NavSpec
{
    public sealed record NavItem(string Href, string Icon, string Label, string? Permission, IReadOnlyList<string>? AnyPermissions);

    public sealed record NavSection(string Id, string Caption, string Icon, IReadOnlyList<NavItem> Items);

    public static readonly NavItem[] TopItems =
    [
        new("/", Icons.Material.Filled.SpaceDashboard, "Overview", null, null),
        new("/chat", Icons.Material.Filled.Chat, "Chat", ChatPermissions.Channels.View, null),
        new("/files", Icons.Material.Filled.FolderOpen, "My Files", FilesPermissions.Upload, null),
    ];

    public static readonly IReadOnlyList<string> TrashPermissions =
    [
        CatalogPermissions.Products.Restore,
        CatalogPermissions.Brands.Restore,
        CatalogPermissions.Categories.Restore,
        TicketsPermissions.Restore,
        FilesPermissions.ViewTrash,
    ];

    public static readonly NavSection[] Sections =
    [
        new("operations", "Operations", Icons.Material.Filled.Insights,
        [
            new("/activity", Icons.Material.Filled.Insights, "Live activity", null, null),
            new("/subscription", Icons.Material.Filled.CreditCard, "Subscription", BillingPermissions.View, null),
            new("/wallet", Icons.Material.Filled.Wallet, "WhatsApp wallet", BillingPermissions.View, null),
            new("/invoices", Icons.Material.Filled.Receipt, "Invoices", BillingPermissions.View, null),
        ]),
        new("catalog", "Catalog", Icons.Material.Filled.Inventory2,
        [
            new("/catalog/products", Icons.Material.Filled.Inventory2, "Products", CatalogPermissions.Products.View, null),
            new("/catalog/brands", Icons.Material.Filled.LocalOffer, "Brands", CatalogPermissions.Brands.View, null),
            new("/catalog/categories", Icons.Material.Filled.FolderShared, "Categories", CatalogPermissions.Categories.View, null),
        ]),
        new("helpdesk", "Helpdesk", Icons.Material.Filled.ConfirmationNumber,
        [
            new("/tickets", Icons.Material.Filled.ConfirmationNumber, "Tickets", TicketsPermissions.View, null),
        ]),
        new("identity", "Identity", Icons.Material.Filled.People,
        [
            new("/identity/users", Icons.Material.Filled.People, "Users", IdentityPermissions.Users.Update, null),
            new("/identity/roles", Icons.Material.Filled.AdminPanelSettings, "Roles", IdentityPermissions.Roles.Update, null),
            new("/identity/groups", Icons.Material.Filled.Groups, "Groups", GroupsPermissions.Update, null),
        ]),
        new("system", "System", Icons.Material.Filled.MonitorHeart,
        [
            new("/system/health", Icons.Material.Filled.MonitorHeart, "Health", null, null),
            new("/system/audits", Icons.Material.Filled.Article, "Audit trail", AuditingPermissions.AuditTrails.View, null),
            new("/system/sessions", Icons.Material.Filled.Wifi, "Sessions", SessionsPermissions.ViewAll, null),
            new("/system/trash", Icons.Material.Filled.Delete, "Trash", null, TrashPermissions),
        ]),
    ];
}
