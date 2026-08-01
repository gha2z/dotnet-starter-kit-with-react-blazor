namespace FSH.BlazorShared.Permissions;

public static class MultitenancyPermissions
{
    public static class Tenants
    {
        public const string Resource = nameof(Tenants);
        public const string View = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string UpgradeSubscription = $"Permissions.{Resource}.UpgradeSubscription";
        public const string ViewTheme = $"Permissions.{Resource}.ViewTheme";
        public const string UpdateTheme = $"Permissions.{Resource}.UpdateTheme";
    }
}
