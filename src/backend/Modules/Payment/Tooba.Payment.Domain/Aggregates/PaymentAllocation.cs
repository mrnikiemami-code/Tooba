using Tooba.Payment.Domain.ValueObjects;

namespace Tooba.Payment.Domain.Aggregates;

/// <summary>
/// تخصیص مبلغ یک پرداخت مشتری روی سفارش فروشنده یا هزینهٔ ارسال Store. تسویه/payout نیست.
/// </summary>
public sealed class PaymentAllocation
{
    /// <summary>
    /// شناسهٔ پایدار هدف StoreShipping (نه SellerOrder).
    /// </summary>
    public static readonly Guid StoreShippingTargetId = Guid.Parse("00000000-0000-7000-9000-00000000a11c");

    /// <summary>
    /// سازندهٔ EF.
    /// </summary>
    private PaymentAllocation()
    {
    }

    /// <summary>
    /// شناسهٔ تخصیص.
    /// </summary>
    public Guid AllocationId { get; init; }

    /// <summary>
    /// پرداخت مالک.
    /// </summary>
    public Guid PaymentId { get; init; }

    /// <summary>
    /// نوع هدف تخصیص.
    /// </summary>
    public PaymentAllocationTargetKind TargetKind { get; init; }

    /// <summary>
    /// سفارش فروشنده وقتی TargetKind=SellerOrder؛ برای StoreShipping همان StoreShippingTargetId.
    /// </summary>
    public Guid SellerOrderId { get; init; }

    /// <summary>
    /// مبلغ تخصیص‌یافته.
    /// </summary>
    public decimal AllocatedAmount { get; init; }

    /// <summary>
    /// ارز تخصیص؛ باید با پرداخت یکی باشد.
    /// </summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>
    /// آیا این تخصیص متعلق به SellerOrder است؟
    /// </summary>
    public bool IsSellerOrder => TargetKind == PaymentAllocationTargetKind.SellerOrder;

    /// <summary>
    /// تخصیص فروشنده را می‌سازد.
    /// </summary>
    public static PaymentAllocation Create(Guid allocationId, Guid paymentId, Guid sellerOrderId, decimal amount, string currency)
        => Create(allocationId, paymentId, PaymentAllocationTargetKind.SellerOrder, sellerOrderId, amount, currency);

    /// <summary>
    /// تخصیص هزینهٔ ارسال Store را می‌سازد.
    /// </summary>
    public static PaymentAllocation CreateStoreShipping(Guid allocationId, Guid paymentId, decimal amount, string currency)
        => Create(allocationId, paymentId, PaymentAllocationTargetKind.StoreShipping, StoreShippingTargetId, amount, currency);

    /// <summary>
    /// تخصیص را می‌سازد.
    /// </summary>
    public static PaymentAllocation Create(
        Guid allocationId,
        Guid paymentId,
        PaymentAllocationTargetKind targetKind,
        Guid targetId,
        decimal amount,
        string currency)
    {
        if (allocationId == Guid.Empty || paymentId == Guid.Empty)
        {
            throw new InvalidOperationException("payment.allocation.ids_required");
        }

        if (amount <= 0)
        {
            throw new InvalidOperationException("payment.allocation.amount_positive");
        }

        if (targetKind == PaymentAllocationTargetKind.SellerOrder && targetId == Guid.Empty)
        {
            throw new InvalidOperationException("payment.allocation.seller_order_required");
        }

        if (targetKind == PaymentAllocationTargetKind.StoreShipping && targetId != StoreShippingTargetId)
        {
            throw new InvalidOperationException("payment.allocation.store_shipping_target");
        }

        return new PaymentAllocation
        {
            AllocationId = allocationId,
            PaymentId = paymentId,
            TargetKind = targetKind,
            SellerOrderId = targetId,
            AllocatedAmount = amount,
            Currency = currency,
        };
    }
}
