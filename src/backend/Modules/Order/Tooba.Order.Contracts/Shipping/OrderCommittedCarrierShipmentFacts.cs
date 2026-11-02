namespace Tooba.Order.Contracts.Shipping;

/// <summary>
/// Read-only committed Order facts for a carrier shipment. Shipping never joins Order storage.
/// </summary>
/// <param name="CheckoutId">سفارش متعهد.</param>
/// <param name="ShippingGroupId">گروه ارسال متعهد.</param>
/// <param name="RecipientName">نام گیرندهٔ ذخیره‌شده.</param>
/// <param name="RecipientMobile">موبایل گیرنده.</param>
/// <param name="RecipientAddress">نشانی پستی.</param>
/// <param name="PostalCode">کدپستی یا تهی.</param>
/// <param name="Currency">ارز گروه.</param>
/// <param name="PayableAtCheckoutAmount">مبلغ پرداختی در تسویه.</param>
/// <param name="CarrierCollectAmount">مبلغ جمع‌آوری جدا از تسویه.</param>
/// <param name="CarrierAccountId">حساب حامل پذیرفته‌شده یا تهی.</param>
/// <param name="ProviderServiceCode">کد خدمت provider یا تهی.</param>
public sealed record OrderCommittedCarrierShipmentFacts(
    Guid CheckoutId,
    Guid ShippingGroupId,
    string RecipientName,
    string RecipientMobile,
    string RecipientAddress,
    string? PostalCode,
    string Currency,
    decimal PayableAtCheckoutAmount,
    decimal CarrierCollectAmount,
    Guid? CarrierAccountId,
    string? ProviderServiceCode);

/// <summary>Contracts-only lookup of a committed shipping group. Empty when the order is not committed.</summary>
public interface IOrderCommittedCarrierShipmentFactsReader
{
    /// <summary>فکت‌های گروه متعهد را می‌خواند؛ نبود گروه یعنی واجد شرایط نیست.</summary>
    Task<OrderCommittedCarrierShipmentFacts?> GetAsync(
        Guid checkoutId,
        Guid shippingGroupId,
        CancellationToken cancellationToken);
}
