using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Composition;
using Tooba.Support.Application.Tickets.Models;
using Tooba.Support.Application.Tickets.Ports;
using Tooba.Support.Contracts.Errors;

namespace Tooba.Support.Application.Tickets.Commands;

/// <summary>مورد استفادهٔ MediatR برای بازگشایی تیکت فروشنده.</summary>
public sealed record ReopenSellerTicketCommand(Guid SellerPartyId, Guid TicketId)
    : IRequest<Result<TicketSnapshotDto>>;

/// <summary>تیکت فروشنده را دوباره باز می‌کند.</summary>
public sealed class ReopenSellerTicketHandler(ISupportDirectory directory)
    : IRequestHandler<ReopenSellerTicketCommand, Result<TicketSnapshotDto>>
{
    /// <inheritdoc />
    public Task<Result<TicketSnapshotDto>> Handle(ReopenSellerTicketCommand request, CancellationToken cancellationToken) =>
        SupportOperation.ExecuteAsync(
            () => directory.ReopenForSellerAsync(request.SellerPartyId, request.TicketId, cancellationToken),
            SupportErrorCodes.ActionRejected);
}
