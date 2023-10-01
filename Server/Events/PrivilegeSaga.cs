using MassTransit;
using Microsoft.Extensions.Options;
using MimeKit;
using Viewer.Server.Configuration;
using Viewer.Server.Models;
using Viewer.Server.Services;
using Viewer.Server.Services.Email;

// ReSharper disable ClassNeverInstantiated.Global

namespace Viewer.Server.Events;

public class PrivilegeRequestState : SagaStateMachineInstance, CorrelatedBy<Guid>
{
    public Guid CorrelationId { get; set; }
    public int CurrentState { get; set; }
    public required Guid UserId { get; set; }
    public string? FaultReason { get; set; }
    public bool Succeeded { get; set; }
    public required string Privilege { get; set; }
    public DateTime RequestSent { get; set; }
    public bool Approved { get; set; }
}

public class PrivilegeRequestStateMachine : MassTransitStateMachine<PrivilegeRequestState>
{
    public Event<PrivilegeRequested>? RequestSubmitted { get; private set; }
    public State? Submitted { get; private set; }
    public Event<PrivilegeRequestSent>? RequestSent { get; private set; }
    public State? AwaitingResponse { get; private set; }
    public Event<PrivilegeResponseReceived>? ResponseReceived { get; private set; }
    public State? AwaitingAction { get; private set; }
    public Event<PrivilegeRequestActionTaken>? ActionTaken { get; private set; }
    public State? Resolved { get; private set; }
    
    public PrivilegeRequestStateMachine()
    {
        InstanceState(x => x.CurrentState, Submitted, AwaitingResponse, AwaitingAction, Resolved);
        Initially(
            When(RequestSubmitted)
                .Then(ctx =>
                {
                    ctx.Saga.UserId = ctx.Message.UserId;
                    ctx.Saga.Privilege = ctx.Message.Privilege;
                })
                .TransitionTo(Submitted));
        During(Initial,
            When(RequestSent)
                .Then(x => x.Saga.RequestSent = DateTime.UtcNow)
                .TransitionTo(AwaitingResponse));
        During(Submitted,
            When(RequestSent)
                //.Schedule() // TODO schedule timeout
                .TransitionTo(AwaitingResponse));
        During(Submitted, CallForAction(When(ResponseReceived)));
        During(AwaitingResponse, CallForAction(When(ResponseReceived)
            // .Unschedule()// TODO unschedule timeout
        )); 
        // TODO fault scenario of action taken
        
        During(AwaitingAction, Ignore(RequestSubmitted));
        During(AwaitingAction, Ignore(RequestSent));
        During(AwaitingAction, Ignore(ResponseReceived));
        During(AwaitingResponse, Ignore(RequestSubmitted));
        During(AwaitingResponse, Ignore(RequestSent));
        //During(AwaitingAction, When(ActionTaken).TransitionTo(Resolved));
        DuringAny(When(ActionTaken)
            .Then(ctx => ctx.Saga.Succeeded = ctx.Message.Outcome)
            .If(ctx => ctx.Saga.Succeeded,
                ctx => ctx.Publish(x 
                    => new PrivilegeRequestSucceeded(x.CorrelationId!.Value, x.Saga.UserId, x.Saga.Privilege))));
    }

    private EventActivityBinder<PrivilegeRequestState, PrivilegeResponseReceived> CallForAction(EventActivityBinder<PrivilegeRequestState, PrivilegeResponseReceived> binder) =>
        binder
            .Then(ctx => ctx.Saga.Approved = ctx.Message.Approve)
            .IfElse(ctx => ctx.Saga.Approved,
                approved => approved
                    .Then(ctx => ctx.Saga.RequestSent = DateTime.UtcNow)
                    .Publish(ctx => new PrivilegeRequest(ctx.Saga.CorrelationId, ctx.Saga.UserId, ctx.Saga.Privilege))
                    .TransitionTo(AwaitingAction),
                denied => denied
                    .Publish(ctx => new PrivilegeRequestDenied(ctx.Saga.CorrelationId, ctx.Saga.UserId, ctx.Saga.Privilege))
                    .TransitionTo(Final));
}


public record PrivilegeRequested(Guid CorrelationId, Guid UserId, string Privilege) : CorrelatedBy<Guid>;
public record PrivilegeRequestSent(Guid CorrelationId, Guid UserId, string Privilege) : CorrelatedBy<Guid>;
public record PrivilegeResponseReceived(Guid CorrelationId, Guid UserId, string Privilege, bool Approve) : CorrelatedBy<Guid>; // TODO fault vs approve?
public record PrivilegeRequest(Guid CorrelationId, Guid UserId, string Privilege) : CorrelatedBy<Guid>;
public record PrivilegeRequestDenied(Guid CorrelationId, Guid UserId, string Privilege) : CorrelatedBy<Guid>;
public record PrivilegeRequestActionTaken(Guid CorrelationId, bool Outcome) : CorrelatedBy<Guid>; // TODO fault
public record PrivilegeRequestSucceeded(Guid CorrelationId, Guid UserId, string Privilege) : CorrelatedBy<Guid>;

public class RolesUpdatedHandler : IConsumer<PrivilegeRequestSucceeded>
{
    private readonly IUserRepository _repo;

    public RolesUpdatedHandler(IUserRepository repo)
    {
        _repo = repo;
    }

    public async Task Consume(ConsumeContext<PrivilegeRequestSucceeded> context)
    {
        var usr = await _repo.GetUser(context.Message.UserId).ConfigureAwait(false);
        usr.Roles.Add(new Role() { RoleName = context.Message.Privilege});
        await _repo.UpdateUser(usr).ConfigureAwait(false);
    }
}

public class PrivilegeRequestedHandler : IConsumer<PrivilegeRequested>
{
    private readonly EmailClient _client;
    private readonly IUserRepository _repo;
    private readonly AdminOptions _opts;

    public PrivilegeRequestedHandler(EmailClient client, IOptions<AdminOptions> options, IUserRepository repo)
    {
        _client = client;
        _repo = repo;
        _opts = options.Value;
    }
    public async Task Consume(ConsumeContext<PrivilegeRequested> context)
    {
        var ctx = context.Message;
        var usr = await _repo.GetUser(ctx.UserId).ConfigureAwait(false);
        var msg = new MimeMessage();
        msg.To.Add(new MailboxAddress("Admin", _opts.AdminEmail));
        msg.Subject = "Privilege request";
        msg.Body = new TextPart("html")
        {
            Text = $"User {usr.UserName} ({usr.FirstName} {usr.LastName}) has requested role {ctx.Privilege}"
        };
        await _client.Send(msg).ConfigureAwait(false);
    }
}
