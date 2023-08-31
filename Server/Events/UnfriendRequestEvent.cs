using MassTransit;
using Viewer.Server.Models;
using Viewer.Server.Services;

namespace Viewer.Server.Events;
public record UnfriendRequestEvent
{
    public required Guid RequesterId { get; init; }
    public required Guid FriendId { get; init; }
}

public class UnfriendRequestHandler : IConsumer<UnfriendRequestEvent>
{
    private readonly IUserRepository _users;

    public UnfriendRequestHandler(IUserRepository repository)
    {
        _users = repository;
    }
    
    public async Task Consume(ConsumeContext<UnfriendRequestEvent> context)
    {
        var request = context.Message;
        // TODO way to add friends key without pulling them all into memory?
        var requester = await _users.GetUser(request.RequesterId).ConfigureAwait(false);
        var friend = await _users.GetUser(request.FriendId).ConfigureAwait(false);
        friend.Friends ??= new List<User>();
        friend.Friends.Remove(requester);
        await _users.UpdateUser(friend);
        // TODO handle case where it fails here
        requester.Friends ??= new List<User>();
        requester.Friends.Remove(friend);
        await _users.UpdateUser(requester);
    }
}

public class UnfriendRequestHandlerDefinition : ConsumerDefinition<UnfriendRequestHandler>
{
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<UnfriendRequestHandler> consumerConfigurator)
    {
        endpointConfigurator.UseMessageRetry(r => r.Incremental(3, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1)));
        endpointConfigurator.UseInMemoryOutbox();
    }
}
