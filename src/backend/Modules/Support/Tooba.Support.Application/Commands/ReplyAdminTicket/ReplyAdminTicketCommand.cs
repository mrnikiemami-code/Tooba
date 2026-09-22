using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Errors;
using Tooba.Support.Application.Models;
using Tooba.Support.Application.Ports;

namespace Tooba.Support.Application.Commands.ReplyAdminTicket;

/// <summary>MediatR reply admin ticket use case.</summary>
public sealed record ReplyAdminTicketCommand(
    Guid ActorUserId,
    Guid TicketId,
    string Body,
    bool IsInternalNote,
    string? IdempotencyKey) : IRequest<Result<TicketSnapshotDto>>;

/// <summary>Adds an admin reply (public may notify).</summary>
public sealed class ReplyAdminTicketHandler(ISupportDirectory directory)
    : IRequestHandler<ReplyAdminTicketCommand, Result<TicketSnapshotDto>>
{
    public Task<Result<TicketSnapshotDto>> Handle(ReplyAdminTicketCommand request, CancellationToken cancellationToken) =>
        SupportExceptionMapper.TryAsync(
            () => directory.ReplyForAdminAsync(
                request.ActorUserId,
                request.TicketId,
                new ReplyTicketCommand(request.Body, request.IsInternalNote, request.IdempotencyKey),
                cancellationToken),
            SupportErrorCodes.ReplyRejected);
}
