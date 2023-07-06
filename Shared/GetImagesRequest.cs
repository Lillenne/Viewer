namespace Viewer.Shared.Requests;

public class GetImagesResponse
{
    public required IReadOnlyList<ImageId> Images { get; init; }
}

public class GetImagesRequest
{
    public string? Directory { get; init; }
    public string? SearchPattern { get; init; }
    public bool Recursive { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
}

public class GetImageRequest
{
    public int Width { get; init; }
    public int Height { get; init; }
    public string Name { get; init; }
}
