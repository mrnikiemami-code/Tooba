using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Composition;
using Tooba.Support.Application.Models;
using Tooba.Support.Application.Ports;
using Tooba.Support.Contracts.Errors;

namespace Tooba.Support.Application.Commands.ReplyCustomerTicket;

/// <summary>مورد استفادهٔ MediatR برای پاسخ مشتری.</summary>
public sealed record ReplyCustomerTicketCommand(
    Guid ActorUserId,
    Guid TicketId,
    string Body,
    string? IdempotencyKey) : IRequest<Result<TicketSnapshotDto>>;

/// <summary>پاسخ مشتری را به تیکت اضافه می‌کند.</summary>
public sealed class ReplyCustomerTicketHandler(ISupportDirectory directory)
    : IRequestHandler<ReplyCustomerTicketCommand, Result<TicketSnapshotDto>>
{
    /// <inheritdoc />
    public Task<Result<TicketSnapshotDto>> Handle(ReplyCustomerTicketCommand request, CancellationToken cancellationToken) =>
        SupportOperation.ExecuteAsync(
            () => directory.ReplyForCustomerAsync(
                request.ActorUserId,
                request.TicketId,
                new ReplyTicketCommand(request.Body, false, request.IdempotencyKey),
                cancellationToken),
            SupportErrorCodes.ReplyRejected);
}
