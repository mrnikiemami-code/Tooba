using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Errors;
using Tooba.Support.Application.Models;
using Tooba.Support.Application.Ports;

namespace Tooba.Support.Application.Commands.CloseCustomerTicket;

/// <summary>MediatR close customer ticket use case.</summary>
public sealed record CloseCustomerTicketCommand(Guid ActorUserId, Guid TicketId)
    : IRequest<Result<TicketSnapshotDto>>;

/// <summary>Closes a customer ticket.</summary>
public sealed class CloseCustomerTicketHandler(ISupportDirectory directory)
    : IRequestHandler<CloseCustomerTicketCommand, Result<TicketSnapshotDto>>
{
    public Task<Result<TicketSnapshotDto>> Handle(CloseCustomerTicketCommand request, CancellationToken cancellationToken) =>
        SupportExceptionMapper.TryAsync(
            () => directory.CloseForCustomerAsync(request.ActorUserId, request.TicketId, cancellationToken),
            SupportErrorCodes.ActionRejected);
}
