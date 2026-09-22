using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Errors;
using Tooba.Support.Application.Models;
using Tooba.Support.Application.Ports;

namespace Tooba.Support.Application.Commands.CloseSellerTicket;

/// <summary>MediatR close seller ticket use case.</summary>
public sealed record CloseSellerTicketCommand(Guid SellerPartyId, Guid TicketId)
    : IRequest<Result<TicketSnapshotDto>>;

/// <summary>Closes a seller ticket.</summary>
public sealed class CloseSellerTicketHandler(ISupportDirectory directory)
    : IRequestHandler<CloseSellerTicketCommand, Result<TicketSnapshotDto>>
{
    public Task<Result<TicketSnapshotDto>> Handle(CloseSellerTicketCommand request, CancellationToken cancellationToken) =>
        SupportExceptionMapper.TryAsync(
            () => directory.CloseForSellerAsync(request.SellerPartyId, request.TicketId, cancellationToken),
            SupportErrorCodes.ActionRejected);
}
