namespace FSH.BlazorShared.Permissions;

public static class IdentityPermissions
{
    public static class Users
    {
        public const string View = "Permissions.Users.View";
        public const string Create = "Permissions.Users.Create";
        public const string Update = "Permissions.Users.Update";
        public const string Delete = "Permissions.Users.Delete";
        public const string Export = "Permissions.Users.Export";
        public const string Impersonate = "Permissions.Users.Impersonate";
    }

    public static class Roles
    {
        public const string View = "Permissions.Roles.View";
        public const string Create = "Permissions.Roles.Create";
        public const string Update = "Permissions.Roles.Update";
        public const string Delete = "Permissions.Roles.Delete";
    }

    public static class Impersonation
    {
        public const string View = "Permissions.Impersonation.View";
        public const string Revoke = "Permissions.Impersonation.Revoke";
    }
}
