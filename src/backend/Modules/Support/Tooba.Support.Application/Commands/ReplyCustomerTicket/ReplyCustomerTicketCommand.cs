using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Errors;
using Tooba.Support.Application.Models;
using Tooba.Support.Application.Ports;

namespace Tooba.Support.Application.Commands.ReplyCustomerTicket;

/// <summary>MediatR reply customer ticket use case.</summary>
public sealed record ReplyCustomerTicketCommand(
    Guid ActorUserId,
    Guid TicketId,
    string Body,
    string? IdempotencyKey) : IRequest<Result<TicketSnapshotDto>>;

/// <summary>Adds a customer reply.</summary>
public sealed class ReplyCustomerTicketHandler(ISupportDirectory directory)
    : IRequestHandler<ReplyCustomerTicketCommand, Result<TicketSnapshotDto>>
{
    public Task<Result<TicketSnapshotDto>> Handle(ReplyCustomerTicketCommand request, CancellationToken cancellationToken) =>
        SupportExceptionMapper.TryAsync(
            () => directory.ReplyForCustomerAsync(
                request.ActorUserId,
                request.TicketId,
                new ReplyTicketCommand(request.Body, false, request.IdempotencyKey),
                cancellationToken),
            SupportErrorCodes.ReplyRejected);
}
