namespace Tooba.Order.Contracts.Settlement;

/// <summary>
/// هویت گروه ارسال سفارش پذیرفته‌شده برای Settlement. فقط‌خواندنی: مرجع نقل‌قول پذیرفته‌شده و هزینهٔ
/// ارسال مشتری را حمل می‌کند و هیچ هزینهٔ carrier، سهم فروشنده/فروشگاه یا نرخ مالیاتی نمی‌سازد.
/// <see cref="AcceptedQuoteVersion"/> از وضعیت پایدار گروه سفارش می‌آید و هرگز جعل نمی‌شود.
/// </summary>
public sealed record OrderShippingGroupSettlementSnapshot(
    Guid ShippingGroupId,
    Guid CheckoutId,
    Guid SellerOrderId,
    Guid SellerPartyId,
    string MethodCode,
    string? ServiceCode,
    string? OptionCode,
    string AcceptedQuoteVersion,
    decimal ShippingCharge,
    string Currency);

/// <summary>
/// درز خواندن گروه‌های ارسال پذیرفته‌شده برای Settlement از Contracts؛ بدون cross-DbContext.
/// </summary>
public interface IOrderShippingGroupSettlementReader
{
    /// <summary>
    /// snapshot گروه‌های پایدار سفارش‌های فروشندهٔ داده‌شده را برمی‌گرداند (خالی اگر گروه‌بندی نباشد).
    /// </summary>
    Task<IReadOnlyList<OrderShippingGroupSettlementSnapshot>> GetForSellerOrdersAsync(
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken);
}
