namespace FSH.BlazorShared.Permissions;

public static class ChatPermissions
{
    public static class Channels
    {
        public const string View = "Permissions.Chat.Channels.View";
        public const string Create = "Permissions.Chat.Channels.Create";
        public const string ManageAll = "Permissions.Chat.Channels.ManageAll";
    }

    public static class Messages
    {
        public const string Send = "Permissions.Chat.Messages.Send";
        public const string EditOwn = "Permissions.Chat.Messages.EditOwn";
        public const string DeleteOwn = "Permissions.Chat.Messages.DeleteOwn";
        public const string DeleteAny = "Permissions.Chat.Messages.DeleteAny";
    }
}
