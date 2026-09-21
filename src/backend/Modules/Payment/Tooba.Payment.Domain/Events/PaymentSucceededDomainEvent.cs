using Tooba.BuildingBlocks;

namespace Tooba.Payment.Domain.Events;

/// <summary>
/// موفقیت فقط پس از Verify درگاه.
/// </summary>
public sealed class PaymentSucceededDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد را می‌سازد.
    /// </summary>
    public PaymentSucceededDomainEvent(
        Guid paymentId,
        Guid checkoutId,
        decimal amount,
        string currency,
        string providerTransactionReference,
        IReadOnlyList<Guid> sellerOrderIds)
    {
        PaymentId = paymentId;
        CheckoutId = checkoutId;
        Amount = amount;
        Currency = currency;
        ProviderTransactionReference = providerTransactionReference;
        SellerOrderIds = sellerOrderIds;
        Metadata = EventMetadataFactory.ForDomain("payment.succeeded.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// پرداخت.
    /// </summary>
    public Guid PaymentId { get; }

    /// <summary>
    /// checkout.
    /// </summary>
    public Guid CheckoutId { get; }

    /// <summary>
    /// مبلغ تصویر.
    /// </summary>
    public decimal Amount { get; }

    /// <summary>
    /// ارز.
    /// </summary>
    public string Currency { get; }

    /// <summary>
    /// مرجع تراکنش تأییدشده.
    /// </summary>
    public string ProviderTransactionReference { get; }

    /// <summary>
    /// سفارش‌های فروشندهٔ تخصیص‌یافته. تصویر Paid فقط روی همین‌ها اعمال می‌شود.
    /// </summary>
    public IReadOnlyList<Guid> SellerOrderIds { get; }
}
