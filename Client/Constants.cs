namespace Viewer.Client;

public static class Routes
{
    public const string Login = "/login";
    public const string Register = "/register";
    public const string Home = "/";
    public const string Upload = "/upload";
    public const string Friends = "/friends";

    public static string GetLoginWithReturnUri(string returnUri)
    {
        return $"{Login}/{returnUri}";
    }
}
public static class ApiRoutes
{
    public static class Auth
    {
        public const string Base = "api/Auth/";
        public const string Login = $"{Base}login";
        public const string Register = $"{Base}register";
        public const string ChangePassword = $"{Base}change-pwd";
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
