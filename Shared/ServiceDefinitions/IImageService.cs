using System.Runtime.Serialization;

namespace Viewer.Shared;

public interface IImageService
{
    Task<GetImagesResponse> GetImages(GetImagesRequest request);
    Task<IReadOnlyList<DirectoryTreeItem>> GetDirectories(string directoryName);
}

public class DirectoryTreeItem
{
    public required string DirectoryName { get; init; }
    //public HashSet<DirectoryTreeItem>? SubDirectories { get; init; } // TODO only HashSet for mudblazor treeview
    public required bool HasSubDirectories { get; init; }
}

public class GetImagesResponse
{
    public required IReadOnlyList<ImageID> Images { get; init; }
}

public class GetImagesRequest
{
    public string? Directory { get; init; }
    public string? SearchPattern { get; init; }
    public bool Recursive { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public bool PreserveAspectRatio { get; init; }
}
