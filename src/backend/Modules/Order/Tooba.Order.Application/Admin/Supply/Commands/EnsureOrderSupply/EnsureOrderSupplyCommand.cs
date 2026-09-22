using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Supply.Models;
using Tooba.Order.Application.Admin.Supply.Services;

namespace Tooba.Order.Application.Admin.Supply.Commands.EnsureOrderSupply;

public sealed record EnsureOrderSupplyCommand(
    Guid CheckoutId,
    OrderSupplyMode Mode,
    bool AllowReacquire,
    string Reason) : IRequest<Result<EnsureOrderSupplyResult>>;

public sealed class EnsureOrderSupplyHandler(OrderSupplyService supply)
    : IRequestHandler<EnsureOrderSupplyCommand, Result<EnsureOrderSupplyResult>>
{
    public Task<Result<EnsureOrderSupplyResult>> Handle(
        EnsureOrderSupplyCommand request,
        CancellationToken cancellationToken) =>
        supply.EnsureAsync(
            request.CheckoutId,
            request.Mode,
            request.AllowReacquire,
            request.Reason,
            cancellationToken);
}
