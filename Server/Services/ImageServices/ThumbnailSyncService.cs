using System.Threading.Channels;
using MassTransit;
using Minio;
using Minio.DataModel;
using Viewer.Server.Services.ImageServices;
using Viewer.Shared.Users;

namespace Viewer.Server.Services;

// TODO prefix?
public record ImageCreated
{
    public required Guid ImageId { get; init; }
    public required string ImageName { get; init; }
    public required UserDto Owner { get; init; }
    public required Visibility Visibility { get; init; }
}

public class ThumbnailMaker : IConsumer<ImageCreated>
{
    private readonly ILogger<ThumbnailMaker> _logger;
    private readonly MinioImageClient _minio;

    public ThumbnailMaker(ILogger<ThumbnailMaker> logger, MinioImageClient minio)
    {
        _logger = logger;
        _minio = minio;
    }

    public async Task Consume(ConsumeContext<ImageCreated> context)
    {
        var name = context.Message.ImageName;
        var token = context.CancellationToken;
        try
        {
            if (MinioImageClient.IsThumbnail(name))
                return;
            foreach (var w in _minio.ThumbnailWidths)
            {
                var tname = MinioImageClient.AppendThumbnailTag(name, w);
                try
                {
                    await _minio.GetThumbnailStat(tname, token).ConfigureAwait(false);
                    // If no exception, the thumbnails already exist & do not need to be created
                }
                catch
                {
                    // thumbnails do not exist and need to be created
                    await _minio.GetImageAndMakeThumbnails(name, token).ConfigureAwait(false);
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error making thumbnails");
        }
    }
}

/// <summary>
/// Service that periodically scans the MinIO repository and creates missing thumbnails
/// </summary>
public class ThumbnailSyncService : BackgroundService
{
    private readonly int _delay;
    private readonly MinioImageClient _minio;
    private readonly ILogger<ThumbnailSyncService> _logger;
    private Channel<Item> _channel = null!;

    public ThumbnailSyncService(IConfiguration config, MinioImageClient client, ILogger<ThumbnailSyncService> logger)
    {
        var delay = config.GetValue<int>("ThumbnailSyncIntervalMilliseconds");
        _delay = delay == 0 ? TimeSpan.FromDays(1).Milliseconds : delay; // default to 1 day
        _minio = client;
        _logger = logger;
    }

    private async Task MakeThumbnails(CancellationToken token)
    {
        while (await _channel.Reader.WaitToReadAsync(token).ConfigureAwait(false))
        {
            while (_channel.Reader.TryRead(out var item))
            {
                try
                {
                    var name = item.Key;
                    if (MinioImageClient.IsThumbnail(name))
                        return;
                    foreach (var w in _minio.ThumbnailWidths)
                    {
                        var tname = MinioImageClient.AppendThumbnailTag(name, w);
                        try
                        {
                            await _minio.GetThumbnailStat(tname, token).ConfigureAwait(false);
                            // If no exception, the thumbnails already exist & do not need to be created
                        }
                        catch
                        {
                            // thumbnails do not exist and need to be created
                            await _minio.GetImageAndMakeThumbnails(name, token).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error syncing thumbnails");
                }
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = Channel.CreateUnbounded<Item>(new UnboundedChannelOptions
        {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = true
        });
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_delay, stoppingToken).ConfigureAwait(false);
            await SyncThumbnails(stoppingToken).ConfigureAwait(false);
        }
    }

    private Task SyncThumbnails(CancellationToken token = default)
    {
        var args = new ListObjectsArgs().WithBucket(_minio.ImageBucket).WithRecursive(true);
        _ = _minio.Minio.ListObjectsAsync(args, token).Subscribe(i =>
        {
            var res = _channel.Writer.TryWrite(i); // this shouldn't fail with the channel setup
            // If it fails, log it and it should eventually be caught with the next poll
            if (!res)
                _logger.LogWarning("Thumbnail sync failed to write {ObjKey} to the channel", i.Key);
        });
        return Task.CompletedTask;
    }
}
