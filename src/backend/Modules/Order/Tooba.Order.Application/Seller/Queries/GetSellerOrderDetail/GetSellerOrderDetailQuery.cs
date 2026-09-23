using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Seller.Models;

namespace Tooba.Order.Application.Seller.Queries.GetSellerOrderDetail;

/// <summary>جزئیات سفارش فروشنده برای Actor مجاز.</summary>
public sealed record GetSellerOrderDetailQuery(Guid SellerPartyId, Guid ActorUserId, Guid SellerOrderId)
    : IRequest<Result<SellerOrderDetailPage>>;

/// <summary>Handler جزئیات سفارش فروشنده.</summary>
public sealed class GetSellerOrderDetailHandler(SellerOrderComposer composer)
    : IRequestHandler<GetSellerOrderDetailQuery, Result<SellerOrderDetailPage>>
{
    public Task<Result<SellerOrderDetailPage>> Handle(
        GetSellerOrderDetailQuery request,
        CancellationToken cancellationToken) =>
        composer.GetDetailAsync(
            request.SellerPartyId,
            request.ActorUserId,
            request.SellerOrderId,
            cancellationToken);
}
