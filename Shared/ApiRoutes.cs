using System.Text;
using System.Web;

namespace Viewer.Shared;

public class ApiRoutes
{
    private readonly string _baseUri;

    public ApiRoutes(string baseUri)
    {
        _baseUri = baseUri.EndsWith("/") ? baseUri : baseUri + '/';
    }

    public string Fq(string route)
    {
        var sb = new StringBuilder();
        sb.Append(_baseUri);
        var rt = route.StartsWith('/') ? route.AsSpan(1) : route;
        sb.Append(rt);
        return sb.ToString();
    }
    
    public string ConfirmFriendRedirect(int code)
    {
        return $"{_baseUri}{Relations.ConfirmFriendBase}?a={code}";
    }
    public static class AuthRoutes
    {
        public static string Base => "api/Auth/";
        public static string Login => $"{Base}login";
        public static string Register => $"{Base}register";
        public static string ChangePassword => $"{Base}change-pwd";
        public static string WhoAmI => $"{Base}whoami";
        public static string Refresh => $"{Base}refresh";
        public static string RequestPrivilege(string privilege) => $"{Base}privileges/{privilege}";
    }

    public static class ImageAccess
    {
        public static string Base => "api/ImageAccess";
        public static string Image => $"{Base}/image";
        public static string Dirs => $"{Base}/dirs";
        public static string Upload => $"{Base}/upload";
        public static string? Download => $"{Base}/download";
    }

    public static class Relations
    {
        public static string Base => "api/Relations";
        public static string AddFriend(Guid id) => $"{Base}/add-friend/{id}";
        public static string Unfriend(Guid id) => $"{Base}/unfriend/{id.ToString()}";
        public static string GetFriends => $"{Base}/friends";
        public static string SuggestFriends => $"{Base}/find-friends";
        public static string ConfirmFriendBase => $"/confirm-friend";

        public static string ConfirmFriend(Guid req, Guid fr, bool approve)
        {
            var code = approve ? "1" : "0";
            var r = HttpUtility.HtmlEncode(req.ToString());
            var f = HttpUtility.HtmlEncode(fr.ToString());
            return $"{Base}{ConfirmFriendBase}?req={r}&fr={f}&a={code}";
        }

        public static string ConfirmFriend(string baseUri, Guid req, Guid fr, bool approve)
        {
            return baseUri.EndsWith("/")
                ? baseUri + ConfirmFriend(req, fr, approve)
                : $"{baseUri}/{ConfirmFriend(req, fr, approve)}";
        }
    }
}
