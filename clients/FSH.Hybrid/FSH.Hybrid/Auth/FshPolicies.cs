using Microsoft.AspNetCore.Authorization;

namespace FSH.Hybrid.Auth;

public static class FshPolicies
{
    public static void Register(AuthorizationOptions options)
    {
        options.AddPolicy("Permissions.Users.View", p => p.RequireClaim("permission", "Permissions.Users.View"));
        options.AddPolicy("Permissions.Users.Create", p => p.RequireClaim("permission", "Permissions.Users.Create"));
        options.AddPolicy("Permissions.Users.Update", p => p.RequireClaim("permission", "Permissions.Users.Update"));
        options.AddPolicy("Permissions.Users.Delete", p => p.RequireClaim("permission", "Permissions.Users.Delete"));
        options.AddPolicy("Permissions.Users.Impersonate", p => p.RequireClaim("permission", "Permissions.Users.Impersonate"));
        options.AddPolicy("Permissions.Roles.View", p => p.RequireClaim("permission", "Permissions.Roles.View"));
        options.AddPolicy("Permissions.Roles.Create", p => p.RequireClaim("permission", "Permissions.Roles.Create"));
        options.AddPolicy("Permissions.Roles.Update", p => p.RequireClaim("permission", "Permissions.Roles.Update"));
        options.AddPolicy("Permissions.Roles.Delete", p => p.RequireClaim("permission", "Permissions.Roles.Delete"));
        options.AddPolicy("Permissions.Groups.View", p => p.RequireClaim("permission", "Permissions.Groups.View"));
        options.AddPolicy("Permissions.Groups.Create", p => p.RequireClaim("permission", "Permissions.Groups.Create"));
        options.AddPolicy("Permissions.Groups.Update", p => p.RequireClaim("permission", "Permissions.Groups.Update"));
        options.AddPolicy("Permissions.Groups.Delete", p => p.RequireClaim("permission", "Permissions.Groups.Delete"));
        options.AddPolicy("Permissions.Sessions.ViewAll", p => p.RequireClaim("permission", "Permissions.Sessions.ViewAll"));
        options.AddPolicy("Permissions.Sessions.RevokeAll", p => p.RequireClaim("permission", "Permissions.Sessions.RevokeAll"));
        options.AddPolicy("Permissions.Sessions.Revoke", p => p.RequireClaim("permission", "Permissions.Sessions.Revoke"));
        options.AddPolicy("Permissions.Tenants.View", p => p.RequireClaim("permission", "Permissions.Tenants.View"));
        options.AddPolicy("Permissions.Tenants.Create", p => p.RequireClaim("permission", "Permissions.Tenants.Create"));
        options.AddPolicy("Permissions.Tenants.Update", p => p.RequireClaim("permission", "Permissions.Tenants.Update"));
        options.AddPolicy("Permissions.Tenants.UpgradeSubscription", p => p.RequireClaim("permission", "Permissions.Tenants.UpgradeSubscription"));
        options.AddPolicy("Permissions.Tenants.ViewTheme", p => p.RequireClaim("permission", "Permissions.Tenants.ViewTheme"));
        options.AddPolicy("Permissions.Tenants.UpdateTheme", p => p.RequireClaim("permission", "Permissions.Tenants.UpdateTheme"));
        options.AddPolicy("Permissions.Billing.View", p => p.RequireClaim("permission", "Permissions.Billing.View"));
        options.AddPolicy("Permissions.Billing.Manage", p => p.RequireClaim("permission", "Permissions.Billing.Manage"));
        options.AddPolicy("Permissions.Catalog.View", p => p.RequireClaim("permission", "Permissions.Catalog.View"));
        options.AddPolicy("Permissions.Catalog.Create", p => p.RequireClaim("permission", "Permissions.Catalog.Create"));
        options.AddPolicy("Permissions.Catalog.Update", p => p.RequireClaim("permission", "Permissions.Catalog.Update"));
        options.AddPolicy("Permissions.Catalog.Delete", p => p.RequireClaim("permission", "Permissions.Catalog.Delete"));
        options.AddPolicy("Permissions.Webhooks.View", p => p.RequireClaim("permission", "Permissions.Webhooks.View"));
        options.AddPolicy("Permissions.Webhooks.Create", p => p.RequireClaim("permission", "Permissions.Webhooks.Create"));
        options.AddPolicy("Permissions.Webhooks.Delete", p => p.RequireClaim("permission", "Permissions.Webhooks.Delete"));
        options.AddPolicy("Permissions.Webhooks.Test", p => p.RequireClaim("permission", "Permissions.Webhooks.Test"));
        options.AddPolicy("Permissions.AuditTrails.View", p => p.RequireClaim("permission", "Permissions.AuditTrails.View"));
        options.AddPolicy("Permissions.AuditTrails.ViewCrossTenant", p => p.RequireClaim("permission", "Permissions.AuditTrails.ViewCrossTenant"));
        options.AddPolicy("Permissions.Impersonation.View", p => p.RequireClaim("permission", "Permissions.Impersonation.View"));
        options.AddPolicy("Permissions.Impersonation.Revoke", p => p.RequireClaim("permission", "Permissions.Impersonation.Revoke"));
        options.AddPolicy("Permissions.Chat.Channels.View", p => p.RequireClaim("permission", "Permissions.Chat.Channels.View"));
        options.AddPolicy("Permissions.Chat.Channels.Create", p => p.RequireClaim("permission", "Permissions.Chat.Channels.Create"));
        options.AddPolicy("Permissions.Chat.Channels.ManageAll", p => p.RequireClaim("permission", "Permissions.Chat.Channels.ManageAll"));
        options.AddPolicy("Permissions.Chat.Messages.Send", p => p.RequireClaim("permission", "Permissions.Chat.Messages.Send"));
        options.AddPolicy("Permissions.Chat.Messages.EditOwn", p => p.RequireClaim("permission", "Permissions.Chat.Messages.EditOwn"));
        options.AddPolicy("Permissions.Chat.Messages.DeleteOwn", p => p.RequireClaim("permission", "Permissions.Chat.Messages.DeleteOwn"));
        options.AddPolicy("Permissions.Chat.Messages.DeleteAny", p => p.RequireClaim("permission", "Permissions.Chat.Messages.DeleteAny"));
        options.AddPolicy("Permissions.Files.Upload", p => p.RequireClaim("permission", "Permissions.Files.Upload"));
        options.AddPolicy("Permissions.Files.View", p => p.RequireClaim("permission", "Permissions.Files.View"));
        options.AddPolicy("Permissions.Files.ViewTrash", p => p.RequireClaim("permission", "Permissions.Files.ViewTrash"));
        options.AddPolicy("Permissions.Files.DeleteOwn", p => p.RequireClaim("permission", "Permissions.Files.DeleteOwn"));
        options.AddPolicy("Permissions.Files.DeleteAny", p => p.RequireClaim("permission", "Permissions.Files.DeleteAny"));
        options.AddPolicy("Permissions.Files.Restore", p => p.RequireClaim("permission", "Permissions.Files.Restore"));
    }
}
