using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Operations.Models;
using Tooba.Order.Application.Admin.Operations.Services;

namespace Tooba.Order.Application.Admin.Operations.Commands.ResolveDamagedGoods;

/// <summary>
/// TB-262: records the operator's reviewed decision for damaged goods physically received back at origin and, for a
/// replacement, reserves new healthy stock and creates the linked replacement parcel.
/// </summary>
public sealed record ResolveDamagedGoodsCommand(
    Guid CheckoutId,
    Guid ActorUserId,
    AdminOrderOperationRequest Request) : IRequest<Result<object>>;

public sealed class ResolveDamagedGoodsHandler(AdminOrderOperationsOrchestrator operations)
    : IRequestHandler<ResolveDamagedGoodsCommand, Result<object>>
{
    public Task<Result<object>> Handle(ResolveDamagedGoodsCommand request, CancellationToken cancellationToken)
    {
        return operations.ResolveDamagedGoodsAsync(
            request.CheckoutId,
            request.ActorUserId,
            request.Request,
            cancellationToken);
    }
}
