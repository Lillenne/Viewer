using Viewer.Shared.Users;

namespace Viewer.Shared;

public static class Extensions
{
    public static Identity Identity(this UserDto user) => new() { Id = user.Id, Name = user.FirstName ?? user.UserName };
}