namespace Viewer.Client.Pages;

public static class Routes
{
    public const string Login = "/login";
    public const string Register = "/register";
    public const string Home = "/";

    public static string GetLoginWithReturnUri(string returnUri) => $"{Login}/{returnUri}";
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
        public const string Base = "api/ImageAccess/";
        public const string Dirs = $"{Base}dirs";
    }
}
