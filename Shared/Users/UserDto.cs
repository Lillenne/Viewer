using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Viewer.Shared.Users;

public class UserDto
{
    public required Guid Id { get; init; }
    public required string UserName { get; init; }
    public string? FirstName { get; init; }
    public IList<string> Roles { get; init; } = new List<string>();
}

[DataContract, Serializable]
public record Identity
{
    [DataMember(Order = 1)] public required Guid Id { get; init; }
    [DataMember(Order = 2)] public required string Name { get; init; }
    
    public Identity(){}

    [SetsRequiredMembers]
    public Identity(Guid id, string name) => (Id, Name) = (id, name);
}