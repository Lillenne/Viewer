using System.Runtime.Serialization;

namespace Viewer.Shared;

[DataContract]
public class ImageID
{
    [DataMember(Order = 1)]
    public required Guid Guid { get; init; }
    [DataMember(Order = 2)]
    public required string Name { get; init; }
    [DataMember(Order = 3)]
    public required string Url { get; init; }
}

// public interface IImage
// {
//     Guid Guid { get; }
//     string Name { get; }
//     byte[] Bytes { get; }
//     string? Encoding { get; }
// }

public class ImageDTO
{
    public string FileName { get; private set; }
    public byte[] Bytes { get; private set; }

    public ImageDTO(string fileName, byte[] bytes)
    {
        FileName = fileName;
        Bytes = bytes;
    }
}
