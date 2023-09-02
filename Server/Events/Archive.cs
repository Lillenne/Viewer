using MassTransit;
using Viewer.Server.Services.ImageServices;

namespace Viewer.Server.Events;

public record ArchiveCreated(Guid OwnerId, Guid ArchiveId);

public record DeleteArchive(Guid OwnerId, Guid ArchiveId);

public class ArchiveCreatedConsumer : IConsumer<ArchiveCreated>
{
    public Task Consume(ConsumeContext<ArchiveCreated> context)
    {
        var del = new DeleteArchive(context.Message.OwnerId, context.Message.ArchiveId);
        var when = DateTime.Now.Add(TimeSpan.FromSeconds(10));
        context.SchedulePublish(when, del);
        return Task.CompletedTask;
    }
}

public class ArchiveDeletedConsumer : IConsumer<DeleteArchive>
{
    private readonly MinioImageClient _service;

    public ArchiveDeletedConsumer(MinioImageClient service)
    {
        _service = service;
    }
    public Task Consume(ConsumeContext<DeleteArchive> context)
    {
        return _service.DeleteArchive(context.Message.OwnerId, context.Message.ArchiveId);
    }
}
