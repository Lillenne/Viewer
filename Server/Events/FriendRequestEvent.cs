using MassTransit;
using MimeKit;
using Viewer.Server.Services;
using Viewer.Server.Services.Email;
using Viewer.Shared;

namespace Viewer.Server.Events;

public class FriendRequestEvent
{
    public required Guid RequesterId { get; init; }
    public required Guid FriendId { get; init; }
}

public class FriendRequestHandler : IConsumer<FriendRequestEvent>
{
    private readonly IUserRepository _users;
    private readonly IUserRelationsRepository _relations;
    private readonly EmailClient _client;
    private readonly ApiRoutes _routes;

    public FriendRequestHandler(IUserRepository users, IUserRelationsRepository relations, EmailClient client, ApiRoutes routes)
    {
        _users = users;
        _relations = relations;
        _client = client;
        _routes = routes;
    }
    public async Task Consume(ConsumeContext<FriendRequestEvent> context)
    {
        var evnt = context.Message;
        var requestor = await _users.GetUserInfo(evnt.RequesterId).ConfigureAwait(false);
        var friend = await _users.GetUserInfo(evnt.FriendId).ConfigureAwait(false);
        await _relations.AddFriend(evnt.RequesterId, evnt.FriendId).ConfigureAwait(false);
        var msg = new MimeMessage();
        msg.To.Add(new MailboxAddress(friend.FirstName ?? friend.UserName, friend.Email));
        msg.Subject = "New friend request";
        var (approve, deny) = GetRoutes(evnt.RequesterId, evnt.FriendId);
        msg.Body = new TextPart("html")
        {
            Text = $"You've received a new friend request from {requestor.FirstName ?? requestor.UserName}! " +
                   $"<a href=\"{approve}\">Click here to confirm.</a>" + 
                   $" <a href=\"{deny}\">or here to deny.</a>"
        };
        // TODO not open or autoclose tab at link? 
        await _client.Send(msg, context.CancellationToken).ConfigureAwait(false);
    }

    private (string Approve, string Deny) GetRoutes(Guid r, Guid f)
    {
        return (_routes.Fq(ApiRoutes.Relations.ConfirmFriend(r, f, true)),
                _routes.Fq(ApiRoutes.Relations.ConfirmFriend(r, f, false)));
    }
}

public class FriendRequestHandlerDefinition : ConsumerDefinition<FriendRequestHandler>
{
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<FriendRequestHandler> consumerConfigurator)
    {
        endpointConfigurator.UseMessageRetry(r => r.Incremental(3, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1)));
        endpointConfigurator.UseInMemoryOutbox();
    }
}

public record FriendRequestConfirmedEvent
{
    public required Guid RequesterId { get; init; }
    public required Guid FriendId { get; init; }
    public required bool Approve { get; init; }
}

public class FriendRequestConfirmedHandler : IConsumer<FriendRequestConfirmedEvent>
{
    private readonly IUserRelationsRepository _users;

    public FriendRequestConfirmedHandler(IUserRelationsRepository repository)
    {
        _users = repository;
    }
    
    public Task Consume(ConsumeContext<FriendRequestConfirmedEvent> context)
    {
        var request = context.Message;
        return _users.ConfirmFriend(request.RequesterId, request.FriendId, request.Approve);
    }
}

public class FriendRequestConfirmedHandlerDefinition : ConsumerDefinition<FriendRequestConfirmedHandler>
{
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<FriendRequestConfirmedHandler> consumerConfigurator)
    {
        endpointConfigurator.UseMessageRetry(r => r.Incremental(3, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1)));
        endpointConfigurator.UseInMemoryOutbox();
    }
}
