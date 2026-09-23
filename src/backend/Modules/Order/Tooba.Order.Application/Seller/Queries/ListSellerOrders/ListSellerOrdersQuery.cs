using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Seller.Models;

namespace Tooba.Order.Application.Seller.Queries.ListSellerOrders;

/// <summary>فهرست سفارش‌های فروشنده برای Actor مجاز.</summary>
public sealed record ListSellerOrdersQuery(Guid SellerPartyId, Guid ActorUserId)
    : IRequest<Result<IReadOnlyList<SellerOrderListItem>>>;

/// <summary>Handler فهرست سفارش فروشنده.</summary>
public sealed class ListSellerOrdersHandler(SellerOrderComposer composer)
    : IRequestHandler<ListSellerOrdersQuery, Result<IReadOnlyList<SellerOrderListItem>>>
{
    public Task<Result<IReadOnlyList<SellerOrderListItem>>> Handle(
        ListSellerOrdersQuery request,
        CancellationToken cancellationToken) =>
        composer.ListAsync(request.SellerPartyId, request.ActorUserId, cancellationToken);
}
