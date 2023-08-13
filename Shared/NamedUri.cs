using System.Runtime.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace Viewer.Shared;

[DataContract]
public class NamedUri
{
    [DataMember(Order = 1)]
    public required string Name { get; init; }
    
    [DataMember(Order = 2)]
    public required Guid Id { get; init; }

    [DataMember(Order = 3)]
    public required string Uri { get; init; }
    
    public NamedUri(){}
    
    [SetsRequiredMembers]
    public NamedUri(string name, Guid id, string uri)
    {
        Name = name;
        Id = id;
        Uri = uri;
    }

    public override bool Equals(object? obj)
    {
        return obj is NamedUri id && id.Id == Id;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id);
    }
}
