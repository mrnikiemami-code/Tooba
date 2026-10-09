using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Composition;
using Tooba.Support.Application.Models;
using Tooba.Support.Application.Ports;
using Tooba.Support.Contracts.Errors;

namespace Tooba.Support.Application.Commands.ReplySellerTicket;

/// <summary>مورد استفادهٔ MediatR برای پاسخ فروشنده.</summary>
public sealed record ReplySellerTicketCommand(
    Guid ActorUserId,
    Guid SellerPartyId,
    Guid TicketId,
    string Body,
    string? IdempotencyKey) : IRequest<Result<TicketSnapshotDto>>;

/// <summary>پاسخ فروشنده را به تیکت اضافه می‌کند.</summary>
public sealed class ReplySellerTicketHandler(ISupportDirectory directory)
    : IRequestHandler<ReplySellerTicketCommand, Result<TicketSnapshotDto>>
{
    /// <inheritdoc />
    public Task<Result<TicketSnapshotDto>> Handle(ReplySellerTicketCommand request, CancellationToken cancellationToken) =>
        SupportOperation.ExecuteAsync(
            () => directory.ReplyForSellerAsync(
                request.ActorUserId,
                request.SellerPartyId,
                request.TicketId,
                new ReplyTicketCommand(request.Body, false, request.IdempotencyKey),
                cancellationToken),
            SupportErrorCodes.ReplyRejected);
}
