using Tooba.Order.Application.Admin.Dashboard.Models;

namespace Tooba.Order.Application.Admin.Dashboard.Ports;

/// <summary>خواندن شمارنده‌های Order برای داشبورد مدیر.</summary>
public interface IAdminOrderDashboardMetricsStore
{
    Task<AdminOrderDashboardMetrics> GetAsync(CancellationToken cancellationToken);
}
