namespace Viewer.Shared;

public static class ApiRoutes
{
    public static class Auth
    {
        public const string Base = "api/Auth/";
        public const string Login = $"{Base}login";
        public const string Register = $"{Base}register";
        public const string ChangePassword = $"{Base}change-pwd";
        public const string WhoAmI = $"{Base}whoami";
        public const string Refresh = $"{Base}refresh";
        public const string Privileges = $"{Base}privileges";
    }

    public static class ImageAccess
    {
        public const string Base = "api/ImageAccess";
        public const string Image = $"{Base}/image";
        public const string Dirs = $"{Base}/dirs";
        public const string Upload = $"{Base}/upload";
        public const string? Download = $"{Base}/download";
    }

    public static class Relations
    {
        public const string Base = "api/Relations";
        public const string AddFriend = $"{Base}/addfriend";
        public const string Unfriend = $"{Base}/unfriend";
        public const string GetFriends = $"{Base}/friends";
        public const string SuggestFriends = $"{Base}/findfriends";
    }
}
