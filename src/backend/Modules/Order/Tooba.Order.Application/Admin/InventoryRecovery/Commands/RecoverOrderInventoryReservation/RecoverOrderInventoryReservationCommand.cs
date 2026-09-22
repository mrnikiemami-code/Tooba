using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.InventoryRecovery.Models;
using Tooba.Order.Application.Admin.InventoryRecovery.Services;

namespace Tooba.Order.Application.Admin.InventoryRecovery.Commands.RecoverOrderInventoryReservation;

public sealed record RecoverOrderInventoryReservationCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    string? Reason) : IRequest<Result<OrderInventoryRecoveryResult>>;

public sealed class RecoverOrderInventoryReservationHandler(OrderInventoryRecoveryService recovery)
    : IRequestHandler<RecoverOrderInventoryReservationCommand, Result<OrderInventoryRecoveryResult>>
{
    public Task<Result<OrderInventoryRecoveryResult>> Handle(
        RecoverOrderInventoryReservationCommand request,
        CancellationToken cancellationToken) =>
        recovery.RecoverAsync(request.CheckoutId, request.ActorUserId, request.Reason, cancellationToken);
}
