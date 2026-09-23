using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Admin.Dashboard.Models;
using Tooba.Order.Application.Admin.Dashboard.Ports;

namespace Tooba.Order.Application.Admin.Dashboard.Queries.GetAdminOrderDashboardMetrics;

/// <summary>شمارنده‌های Order برای داشبورد مدیر.</summary>
public sealed record GetAdminOrderDashboardMetricsQuery : IRequest<Result<AdminOrderDashboardMetrics>>;

/// <summary>شمارنده‌ها را از store Order می‌خواند.</summary>
public sealed class GetAdminOrderDashboardMetricsHandler(IAdminOrderDashboardMetricsStore store)
    : IRequestHandler<GetAdminOrderDashboardMetricsQuery, Result<AdminOrderDashboardMetrics>>
{
    public async Task<Result<AdminOrderDashboardMetrics>> Handle(
        GetAdminOrderDashboardMetricsQuery request,
        CancellationToken cancellationToken) =>
        Result.Success(await store.GetAsync(cancellationToken));
}
