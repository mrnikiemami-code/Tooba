using Tooba.BuildingBlocks.Grid;
using Tooba.Order.Application.Admin.Customers.Models;

namespace Tooba.Order.Application.Admin.Customers.Ports;

/// <summary>خوانندهٔ DB-native مشتریان Admin از Checkoutهای Order.</summary>
public interface IAdminCustomersGridReader
{
    /// <summary>فهرست کامل مشتریان مشتق‌شده از Checkout (بدون صفحه‌بندی).</summary>
    Task<IReadOnlyList<AdminCustomerListItem>> ListAsync(CancellationToken cancellationToken);

    /// <summary>صفحهٔ گرید برای درخواست از پیش normalize‌شده.</summary>
    Task<GridPageResponse<AdminCustomerListItem>> QueryAsync(
        GridQueryRequest request,
        CancellationToken cancellationToken);
}
