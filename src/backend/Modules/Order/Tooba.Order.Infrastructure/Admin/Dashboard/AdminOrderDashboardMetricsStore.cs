using Microsoft.EntityFrameworkCore;
using Tooba.Order.Application.Admin.Dashboard.Models;
using Tooba.Order.Application.Admin.Dashboard.Ports;
using Tooba.Order.Domain;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Infrastructure.Admin.Dashboard;

/// <summary>شمارنده‌های SellerOrder/Checkout برای داشبورد مدیر.</summary>
internal sealed class AdminOrderDashboardMetricsStore(OrderDbContext orders) : IAdminOrderDashboardMetricsStore
{
    public async Task<AdminOrderDashboardMetrics> GetAsync(CancellationToken cancellationToken)
    {
        var statuses = await orders.SellerOrders.AsNoTracking()
            .Select(x => x.Status)
            .ToListAsync(cancellationToken);
        var customers = await orders.Checkouts.AsNoTracking()
            .Select(x => x.PlacedByUserId)
            .Distinct()
            .CountAsync(cancellationToken);
        var paid = statuses.Count(x => x == SellerOrderStatus.Paid);
        var pending = statuses.Count(x => x is SellerOrderStatus.PendingPayment or SellerOrderStatus.Submitted);
        var open = statuses.Count(x => x is not SellerOrderStatus.Paid and not SellerOrderStatus.Cancelled);
        return new AdminOrderDashboardMetrics(open, paid, pending, customers);
    }
}
