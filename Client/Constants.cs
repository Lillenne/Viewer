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