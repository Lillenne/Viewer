using MassTransit;
using Viewer.Server.Services.ImageServices;

namespace Viewer.Server.Services;

public record Uploaded
{
    
    /// <summary>
    /// The ID for this upload
    /// </summary>
    public required Guid UploadId { get; init; }
    
    /// <summary>
    /// The ID of the owner of the image
    /// </summary>
    public required Guid OwnerId { get; init; }
    
    /// <summary>
    /// The original upload file name
    /// </summary>
    public required string OriginalFileName { get; set; }
    
    /// <summary>
    /// The relative directory of the upload
    /// </summary>
    public string? DirectoryPrefix { get; set; }
}

public class ThumbnailMaker : IConsumer<Uploaded>
{
    private readonly ILogger<ThumbnailMaker> _logger;
    private readonly MinioImageClient _minio;

    public ThumbnailMaker(ILogger<ThumbnailMaker> logger, MinioImageClient minio)
    {
        _logger = logger;
        _minio = minio;
    }

    public async Task Consume(ConsumeContext<Uploaded> context)
    {
        var msg = context.Message;
        var token = context.CancellationToken;
        try
        {
            await _minio.CreateThumbnails(msg.OwnerId, msg.UploadId.ToString(), msg.DirectoryPrefix, token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error making thumbnails");
        }
    }
}