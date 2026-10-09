using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Composition;
using Tooba.Support.Application.Models;
using Tooba.Support.Application.Ports;
using Tooba.Support.Contracts.Errors;

namespace Tooba.Support.Application.Commands.CloseSellerTicket;

/// <summary>مورد استفادهٔ MediatR برای بستن تیکت فروشنده.</summary>
public sealed record CloseSellerTicketCommand(Guid SellerPartyId, Guid TicketId)
    : IRequest<Result<TicketSnapshotDto>>;

/// <summary>تیکت فروشنده را می‌بندد.</summary>
public sealed class CloseSellerTicketHandler(ISupportDirectory directory)
    : IRequestHandler<CloseSellerTicketCommand, Result<TicketSnapshotDto>>
{
    /// <inheritdoc />
    public Task<Result<TicketSnapshotDto>> Handle(CloseSellerTicketCommand request, CancellationToken cancellationToken) =>
        SupportOperation.ExecuteAsync(
            () => directory.CloseForSellerAsync(request.SellerPartyId, request.TicketId, cancellationToken),
            SupportErrorCodes.ActionRejected);
}
