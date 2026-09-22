using Tooba.Order.Domain;

namespace Tooba.Order.Application.Admin.Detail.Ports;

/// <summary>خواندن Checkout و ثبت AdminViewAck برای جزئیات سفارش مدیر.</summary>
public interface IAdminOrderDetailCheckoutStore
{
    /// <summary>Checkout با SellerOrders و Lines، یا null اگر نباشد.</summary>
    Task<CheckoutGroup?> GetCheckoutAsync(Guid checkoutId, CancellationToken cancellationToken);

    /// <summary>ثبت مشاهدهٔ مدیر با زمان تزریق‌شده (IClock).</summary>
    Task RecordAdminViewAckAsync(
        Guid checkoutId,
        Guid viewerUserId,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
