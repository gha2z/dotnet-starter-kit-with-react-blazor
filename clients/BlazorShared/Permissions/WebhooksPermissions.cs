namespace FSH.BlazorShared.Permissions;

public static class WebhooksPermissions
{
    public static class Subscriptions
    {
        // Mirrors the React admin constant: the server registry value is
        // "Permissions.Webhooks.View" even though the class is scoped to Subscriptions.
        public const string View = "Permissions.Webhooks.View";
        public const string Create = "Permissions.Webhooks.Create";
        public const string Delete = "Permissions.Webhooks.Delete";
        public const string Test = "Permissions.Webhooks.Test";
    }
}
