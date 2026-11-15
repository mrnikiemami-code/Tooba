using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Queries.PreviewShipmentDispatchPlan;

/// <summary>
/// TB-270-R2: read-only preview of ONE existing resend/replacement parcel's OWN dispatch plan.
/// </summary>
/// <remarks>
/// The parcel must already exist in Fulfillment; this read never creates it, never reserves stock and never commits a
/// plan. The feasible customer dates come from the seller's own captured inputs through the one scheduling authority.
/// </remarks>
public sealed record PreviewShipmentDispatchPlanQuery(
    Guid CheckoutId,
    Guid ActorUserId,
    Guid ShipmentId) : IRequest<Result<AdminShipmentDispatchPlanPreview>>;

public sealed class PreviewShipmentDispatchPlanHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<PreviewShipmentDispatchPlanQuery, Result<AdminShipmentDispatchPlanPreview>>
{
    public Task<Result<AdminShipmentDispatchPlanPreview>> Handle(
        PreviewShipmentDispatchPlanQuery request,
        CancellationToken cancellationToken)
    {
        return operations.PreviewShipmentDispatchPlanAsync(
            request.CheckoutId,
            request.ActorUserId,
            request.ShipmentId,
            cancellationToken);
    }
}
