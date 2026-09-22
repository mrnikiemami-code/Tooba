using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Errors;
using Tooba.Support.Application.Models;
using Tooba.Support.Application.Ports;

namespace Tooba.Support.Application.Commands.ReplySellerTicket;

/// <summary>MediatR reply seller ticket use case.</summary>
public sealed record ReplySellerTicketCommand(
    Guid ActorUserId,
    Guid SellerPartyId,
    Guid TicketId,
    string Body,
    string? IdempotencyKey) : IRequest<Result<TicketSnapshotDto>>;

/// <summary>Adds a seller reply.</summary>
public sealed class ReplySellerTicketHandler(ISupportDirectory directory)
    : IRequestHandler<ReplySellerTicketCommand, Result<TicketSnapshotDto>>
{
    public Task<Result<TicketSnapshotDto>> Handle(ReplySellerTicketCommand request, CancellationToken cancellationToken) =>
        SupportExceptionMapper.TryAsync(
            () => directory.ReplyForSellerAsync(
                request.ActorUserId,
                request.SellerPartyId,
                request.TicketId,
                new ReplyTicketCommand(request.Body, false, request.IdempotencyKey),
                cancellationToken),
            SupportErrorCodes.ReplyRejected);
}
