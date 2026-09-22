using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Errors;
using Tooba.Support.Application.Models;
using Tooba.Support.Application.Ports;

namespace Tooba.Support.Application.Commands.ReopenSellerTicket;

/// <summary>MediatR reopen seller ticket use case.</summary>
public sealed record ReopenSellerTicketCommand(Guid SellerPartyId, Guid TicketId)
    : IRequest<Result<TicketSnapshotDto>>;

/// <summary>Reopens a seller ticket.</summary>
public sealed class ReopenSellerTicketHandler(ISupportDirectory directory)
    : IRequestHandler<ReopenSellerTicketCommand, Result<TicketSnapshotDto>>
{
    public Task<Result<TicketSnapshotDto>> Handle(ReopenSellerTicketCommand request, CancellationToken cancellationToken) =>
        SupportExceptionMapper.TryAsync(
            () => directory.ReopenForSellerAsync(request.SellerPartyId, request.TicketId, cancellationToken),
            SupportErrorCodes.ActionRejected);
}
