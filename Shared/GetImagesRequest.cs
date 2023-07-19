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
    public int StartIndex { get; init; } = 0;
    public int TakeNumber { get; init; } = -1;
}

public class GetImageRequest
{
    public int Width { get; init; }
    public int Height { get; init; }
    public required string Name { get; init; }
}
