using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Composition;
using Tooba.Support.Application.Tickets.Models;
using Tooba.Support.Application.Tickets.Ports;
using Tooba.Support.Contracts.Errors;

namespace Tooba.Support.Application.Tickets.Commands;

/// <summary>مورد استفادهٔ MediatR برای بازگشایی تیکت مشتری.</summary>
public sealed record ReopenCustomerTicketCommand(Guid ActorUserId, Guid TicketId)
    : IRequest<Result<TicketSnapshotDto>>;

/// <summary>تیکت مشتری را دوباره باز می‌کند.</summary>
public sealed class ReopenCustomerTicketHandler(ISupportDirectory directory)
    : IRequestHandler<ReopenCustomerTicketCommand, Result<TicketSnapshotDto>>
{
    /// <inheritdoc />
    public Task<Result<TicketSnapshotDto>> Handle(ReopenCustomerTicketCommand request, CancellationToken cancellationToken) =>
        SupportOperation.ExecuteAsync(
            () => directory.ReopenForCustomerAsync(request.ActorUserId, request.TicketId, cancellationToken),
            SupportErrorCodes.ActionRejected);
}
