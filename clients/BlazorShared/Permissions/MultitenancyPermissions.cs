namespace FSH.BlazorShared.Permissions;

public static class MultitenancyPermissions
{
    public static class Tenants
    {
        public const string View = "Permissions.Tenants.View";
        public const string Create = "Permissions.Tenants.Create";
        public const string Edit = "Permissions.Tenants.Edit";
        public const string Delete = "Permissions.Tenants.Delete";
        public const string Upgrade = "Permissions.Tenants.Upgrade";
    }
}
