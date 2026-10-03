using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Seller.Models;

namespace Tooba.Order.Application.Seller.Queries;

/// <summary>
/// نمای داشبورد فروشنده: شمارش open/paid سفارش Order-owned بعلاوهٔ غنی‌سازی نام نمایشی از Party.Contracts.
/// مسیر GET /v1/seller/dashboard به Order.Endpoints منتقل شده و از همین request استفاده می‌کند.
/// </summary>
public sealed record GetSellerOrderDashboardSummaryQuery(Guid SellerPartyId, Guid ActorUserId)
    : IRequest<Result<SellerDashboardView>>;

/// <summary>Handler خلاصهٔ داشبورد سفارش فروشنده.</summary>
public sealed class GetSellerOrderDashboardSummaryHandler(SellerOrderComposer composer)
    : IRequestHandler<GetSellerOrderDashboardSummaryQuery, Result<SellerDashboardView>>
{
    public Task<Result<SellerDashboardView>> Handle(
        GetSellerOrderDashboardSummaryQuery request,
        CancellationToken cancellationToken) =>
        composer.GetDashboardViewAsync(request.SellerPartyId, request.ActorUserId, cancellationToken);
}

