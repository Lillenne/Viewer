using System.Runtime.Serialization;

namespace Viewer.Shared;

[DataContract]
public class ImageId
{
    [DataMember(Order = 1)]
    public required Guid Guid { get; init; }
    [DataMember(Order = 2)]
    public required string Name { get; init; }
    [DataMember(Order = 3)]
    public required string Url { get; init; }
}