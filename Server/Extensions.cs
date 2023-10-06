using MassTransit;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
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
    public static IEnumerable<string> Names(this IEnumerable<Group> groups)
    {
        return groups.Select(g => g.GroupName);
    }
    
    public static IEnumerable<Identity> ViewableIdentities(this UserRelations user)
    {
        yield return new Identity(user.User.Id, user.User.UserName);
        foreach (var id in user.Groups)
            yield return new Identity(id.Id, id.Name);
        foreach (var id in user.Friends)
            yield return new Identity(id.Id, id.UserName);
    }
}