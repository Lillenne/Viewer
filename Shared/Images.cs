using System.Runtime.Serialization;

namespace Viewer.Shared;

[DataContract]
public class ImageId
{
    [DataMember(Order = 1)]
    public required string Name { get; init; }

    [DataMember(Order = 2)]
    public required string Url { get; init; }

    public override bool Equals(object? obj)
    {
        return obj is ImageId id
            && Name.Equals(id.Name, StringComparison.OrdinalIgnoreCase)
            && Url.Equals(id.Url, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        if (Name is not null && Url is not null)
        {
            var l = Math.Min(Name.Length, 3);
            var a = l == 0 ? string.Empty : Name[..l];
            l = Math.Min(Url.Length, 3);
            var b = l == 0 ? string.Empty : Url[..l];
            return HashCode.Combine(a, b);
        }
        else
        {
            return base.GetHashCode();
        }
    }
}
