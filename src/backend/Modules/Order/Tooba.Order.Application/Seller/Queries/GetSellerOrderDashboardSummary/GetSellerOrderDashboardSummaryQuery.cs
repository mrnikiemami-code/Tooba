using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Seller.Models;

namespace Tooba.Order.Application.Seller.Queries.GetSellerOrderDashboardSummary;

/// <summary>شمارش open/paid سفارش فروشنده برای داشبورد.</summary>
public sealed record GetSellerOrderDashboardSummaryQuery(Guid SellerPartyId, Guid ActorUserId)
    : IRequest<Result<SellerOrderDashboardSummary>>;

/// <summary>Handler خلاصهٔ داشبورد سفارش فروشنده.</summary>
public sealed class GetSellerOrderDashboardSummaryHandler(SellerOrderComposer composer)
    : IRequestHandler<GetSellerOrderDashboardSummaryQuery, Result<SellerOrderDashboardSummary>>
{
    public Task<Result<SellerOrderDashboardSummary>> Handle(
        GetSellerOrderDashboardSummaryQuery request,
        CancellationToken cancellationToken) =>
        composer.GetDashboardSummaryAsync(request.SellerPartyId, request.ActorUserId, cancellationToken);
}
