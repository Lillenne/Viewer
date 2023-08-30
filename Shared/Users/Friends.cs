using System.Runtime.Serialization;

namespace Viewer.Shared.Users;

[DataContract, Serializable]
public class GetFriendsResponse
{
    public required IReadOnlyList<Identity> Friends { get; set; }
}