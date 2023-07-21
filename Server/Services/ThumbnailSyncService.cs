using Minio;
using Viewer.Shared.Services;

namespace Viewer.Server.Services;

public class ThumbnailSyncService : BackgroundService
{
    private readonly int _delay;
    private readonly MinioImageClient _minio;
    private readonly ILogger<ThumbnailSyncService> _logger;
    private readonly SemaphoreSlim _s = new(16);

    public ThumbnailSyncService(IConfiguration config, MinioImageClient client, ILogger<ThumbnailSyncService> logger)
    {
        var delay = config.GetValue<int>("ThumbnailSyncIntervalMilliseconds");
        _delay = delay == 0 ? 60 * 10 * 1000 : delay; // default to 10 min
        _minio = client;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_delay, stoppingToken).ConfigureAwait(false);
            await SyncThumbnails(stoppingToken).ConfigureAwait(false);
        }
    }

    private Task SyncThumbnails(CancellationToken token = default)
    {
        var args = new ListObjectsArgs().WithBucket(_minio.ImageBucket).WithRecursive(true);
        _ = _minio.Minio.ListObjectsAsync(args, token).Subscribe(async item =>
        {
            try
            {
                var name = item.Key;
                if (MinioImageClient.IsThumbnail(name) || !MinioImageClient.IsSupportedImage(name))
                {
                    return;
                }
                foreach (var w in _minio.ThumbnailWidths)
                {
                    var tname = MinioImageClient.GetThumbnailName(name, w);
                    try
                    {
                        var existsArgs = new StatObjectArgs()
                            .WithBucket(_minio.ThumbnailBucket)
                            .WithObject(tname);
                        var exists = await _minio.Minio.StatObjectAsync(existsArgs, token).ConfigureAwait(false);
                        // If no exception, the thumbnails already exist & do not need to be created
                    }
                    catch
                    {
                        // thumbnails do not exist and need to be created
                        await _s.WaitAsync(token); // Throttle observable without ignoring requests
                        try
                        {
                            using var ms = new MemoryStream();
                            var args = new GetObjectArgs()
                                .WithBucket(_minio.ImageBucket)
                                .WithObject(name)
                                .WithCallbackStream(s => s.CopyTo(ms));
                            _ = await _minio.Minio.GetObjectAsync(args).ConfigureAwait(false);
                            ms.Flush();
                            var arr = ms.ToArray();
                            var ul = new ImageUpload() { Name = name, Image = arr };
                            await _minio.MakeThumbnails(ul, token);
                            return;
                        }
                        finally
                        {
                            _ = _s.Release();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error syncing thumbnails");
            }
        });
        return Task.CompletedTask;
    }
}
