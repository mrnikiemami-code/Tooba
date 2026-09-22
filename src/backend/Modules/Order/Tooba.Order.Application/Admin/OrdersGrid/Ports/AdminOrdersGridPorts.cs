using Tooba.BuildingBlocks.Grid;
using Tooba.Order.Application.Admin.OrdersGrid.Models;

namespace Tooba.Order.Application.Admin.OrdersGrid.Ports;

/// <summary>خوانندهٔ DB-native گرید سفارش‌های مدیر (Order.Infrastructure).</summary>
public interface IAdminOrdersGridReader
{
    /// <summary>صفحهٔ گرید را برای درخواست از پیش normalize‌شده برمی‌گرداند.</summary>
    Task<GridPageResponse<AdminOrderListItem>> QueryAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken);
}

/// <summary>
/// وضعیت تأمین Checkout برای ستون/فیلتر گرید.
/// درز باریک Order به ترکیب‌گر تأمین که هنوز در Host است (R4).
/// </summary>
public interface IAdminOrderSupplyStatusReader
{
    /// <summary>وضعیت تأمین هر Checkout با نام پایدار enum.</summary>
    Task<IReadOnlyDictionary<Guid, string>> GetStatusesAsync(
        IReadOnlyList<Guid> checkoutIds,
        CancellationToken cancellationToken);
}
