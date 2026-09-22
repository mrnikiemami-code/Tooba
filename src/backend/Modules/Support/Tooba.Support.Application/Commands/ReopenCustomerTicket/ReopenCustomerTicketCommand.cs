using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Errors;
using Tooba.Support.Application.Models;
using Tooba.Support.Application.Ports;

namespace Tooba.Support.Application.Commands.ReopenCustomerTicket;

/// <summary>MediatR reopen customer ticket use case.</summary>
public sealed record ReopenCustomerTicketCommand(Guid ActorUserId, Guid TicketId)
    : IRequest<Result<TicketSnapshotDto>>;

/// <summary>Reopens a customer ticket.</summary>
public sealed class ReopenCustomerTicketHandler(ISupportDirectory directory)
    : IRequestHandler<ReopenCustomerTicketCommand, Result<TicketSnapshotDto>>
{
    public Task<Result<TicketSnapshotDto>> Handle(ReopenCustomerTicketCommand request, CancellationToken cancellationToken) =>
        SupportExceptionMapper.TryAsync(
            () => directory.ReopenForCustomerAsync(request.ActorUserId, request.TicketId, cancellationToken),
            SupportErrorCodes.ActionRejected);
}
