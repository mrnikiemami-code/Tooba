using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Supply.Models;
using Tooba.Order.Application.Admin.Supply.Services;

namespace Tooba.Order.Application.Admin.Supply.Queries.GetOrderSupplyStatus;

public sealed record GetOrderSupplyStatusQuery(Guid CheckoutId)
    : IRequest<Result<OrderSupplyStatus>>;

public sealed class GetOrderSupplyStatusHandler(OrderSupplyService supply)
    : IRequestHandler<GetOrderSupplyStatusQuery, Result<OrderSupplyStatus>>
{
    public Task<Result<OrderSupplyStatus>> Handle(
        GetOrderSupplyStatusQuery request,
        CancellationToken cancellationToken) =>
        supply.GetStatusAsync(request.CheckoutId, cancellationToken);
}
