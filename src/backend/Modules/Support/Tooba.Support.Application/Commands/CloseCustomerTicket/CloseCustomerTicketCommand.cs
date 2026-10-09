using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Composition;
using Tooba.Support.Application.Models;
using Tooba.Support.Application.Ports;
using Tooba.Support.Contracts.Errors;

namespace Tooba.Support.Application.Commands.CloseCustomerTicket;

/// <summary>مورد استفادهٔ MediatR برای بستن تیکت مشتری.</summary>
public sealed record CloseCustomerTicketCommand(Guid ActorUserId, Guid TicketId)
    : IRequest<Result<TicketSnapshotDto>>;

/// <summary>تیکت مشتری را می‌بندد.</summary>
public sealed class CloseCustomerTicketHandler(ISupportDirectory directory)
    : IRequestHandler<CloseCustomerTicketCommand, Result<TicketSnapshotDto>>
{
    /// <inheritdoc />
    public Task<Result<TicketSnapshotDto>> Handle(CloseCustomerTicketCommand request, CancellationToken cancellationToken) =>
        SupportOperation.ExecuteAsync(
            () => directory.CloseForCustomerAsync(request.ActorUserId, request.TicketId, cancellationToken),
            SupportErrorCodes.ActionRejected);
}
