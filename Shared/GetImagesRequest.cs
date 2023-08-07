namespace Viewer.Shared.Requests;
using System.Diagnostics.CodeAnalysis;

public class GetImagesResponse
{
    public required IReadOnlyList<ImageId> Images { get; init; }
    public GetImagesResponse(){}

    [SetsRequiredMembers]
    public GetImagesResponse(IEnumerable<ImageId> images)
    {
        Images = images.ToList().AsReadOnly();
    }
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

public class DownloadImagesRequest
{
    public required IEnumerable<GetImageRequest> Images { get; init; }

    public DownloadImagesRequest(){}
    
    [SetsRequiredMembers]
    public DownloadImagesRequest(IEnumerable<GetImageRequest> images)
    {
        Images = images;
    }
}