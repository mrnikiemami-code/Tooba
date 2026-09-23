using Tooba.Order.Domain;

namespace Tooba.Order.Application.Admin.LegacyList.Ports;

/// <summary>خواندن آخرین CheckoutGroupها برای GET سازگاری /v1/admin/orders.</summary>
public interface IAdminOrderListStore
{
    /// <summary>آخرین <paramref name="take"/> Checkout با SellerOrders و Lines.</summary>
    Task<IReadOnlyList<CheckoutGroup>> ListLatestGroupsAsync(int take, CancellationToken cancellationToken);
}
