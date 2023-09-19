using Viewer.Server.Models;
using Viewer.Shared.Users;

namespace Viewer.Server;

public static class Policies
{
    public const string UploadPolicy = "upload-privilege";
    public const string DownloadPolicy = "download-privilege";
    public const string AuthenticatedPolicy = "authenticated";
}

public static class Extensions
{
    public static IEnumerable<Identity> GroupIdentities(this IEnumerable<Group> groups)
    {
        return groups.Select(g => new Identity
        {
            Id = g.Id,
            Name = g.Name
        });
    }
    
    public static IEnumerable<Identity> ViewableIdentities(this User user)
    {
        yield return new Identity(user.Id, user.UserName);
        foreach (var id in user.Groups)
            yield return new Identity(id.Id, id.Name);
        foreach (var id in user.Friends)
            yield return new Identity(id.Id, id.UserName);
    }
}