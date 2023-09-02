using MassTransit;
using Viewer.Server.Models;
using Viewer.Server.Services.ImageServices;

namespace Viewer.Server.Events;

public class ThumbnailMaker : IConsumer<Upload>
{
    private readonly MinioImageClient _minio;

    public ThumbnailMaker(MinioImageClient minio)
    {
        _minio = minio;
    }

    public Task Consume(ConsumeContext<Upload> context)
    {
        var msg = context.Message;
        var token = context.CancellationToken;
        return _minio.CreateThumbnails(msg.OwnerId, msg.StoredName(), token: token);
    }
}