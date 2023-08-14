using System.Diagnostics.CodeAnalysis;

namespace Viewer.Shared;

public class ChangePasswordRequest
{
    public required string UserId { get; init; }
    public required string OldPassword { get; init; }
    public required string NewPassword { get; init; }
}

public class GetImagesResponse
{
    public required IReadOnlyList<NamedUri> Images { get; init; }
    public GetImagesResponse(){}

    [SetsRequiredMembers]
    public GetImagesResponse(IEnumerable<NamedUri> images)
    {
        Images = images.ToList().AsReadOnly();
    }
}

public class GetImagesRequest
{
    public required Guid SourceId { get; init; }
    public string? Directory { get; init; }
    //public string? SearchPattern { get; init; }
    public int Width { get; init; }
}

public class GetImageRequest
{
    public int Width { get; init; }
    public int Height { get; init; }
    public required Guid Id { get; init; }
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