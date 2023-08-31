using MassTransit;
using MimeKit;
using Viewer.Server.Models;
using Viewer.Server.Services;
using Viewer.Server.Services.Email;

namespace Viewer.Server.Events;

public class FriendRequestEvent
{
    public required Guid RequesterId { get; init; }
    public required Guid FriendId { get; init; }
}

public class FriendRequestHandler : IConsumer<FriendRequestEvent>
{
    private readonly IUserRepository _users;
    private readonly EmailClient _client;

    public FriendRequestHandler(IUserRepository users, EmailClient client)
    {
        _users = users;
        _client = client;
    }
    public async Task Consume(ConsumeContext<FriendRequestEvent> context)
    {
        var evnt = context.Message;
        var requestor = await _users.GetUser(evnt.RequesterId).ConfigureAwait(false);
        var friend = await _users.GetUser(evnt.FriendId).ConfigureAwait(false);
        var msg = new MimeMessage();
        msg.To.Add(new MailboxAddress(friend.FirstName ?? friend.UserName, friend.Email));
        msg.Subject = "New friend request";
        msg.Body = new TextPart("html")
        {
            Text = $"You've received a new friend request from {requestor.FirstName ?? requestor.UserName}! " +
                   $"<a href=\"www.pixalyzer.com/api/Relations/confirm-friend?requester={requestor.Id}&friend={friend.Id}\" >Click here to confirm.</a>"
        };
        await _client.Send(msg, context.CancellationToken).ConfigureAwait(false);
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