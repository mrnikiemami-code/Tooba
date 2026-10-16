using Tooba.Offer.Contracts.Dtos;

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

/// <summary>
/// کانال فروش پایدار یک checkout. حقیقت پایدار از وضعیت ثبت‌شدهٔ سفارش می‌آید و هرگز از برچسب رابط
/// کاربری، نام فروشنده، SKU، حدس Edition یا ادعای کلاینت ساخته نمی‌شود.
/// </summary>
/// <param name="CheckoutId">شناسهٔ checkout مالک.</param>
/// <param name="Channel">کانال پایدار فروش در لحظهٔ ثبت.</param>
public sealed record OrderCheckoutSalesChannelSnapshot(
    Guid CheckoutId,
    SalesChannel Channel);

/// <summary>
/// درز خواندن کانال فروش پایدار یک checkout برای Settlement از Contracts؛ فقط‌خواندنی و بدون
/// cross-DbContext.
/// </summary>
public interface IOrderCheckoutSalesChannelSettlementReader
{
    /// <summary>
    /// کانال پایدار checkout را برمی‌گرداند؛ <c>null</c> اگر checkout ناشناخته باشد (fail-closed در مصرف‌کننده).
    /// </summary>
    Task<OrderCheckoutSalesChannelSnapshot?> GetChannelAsync(
        Guid checkoutId,
        CancellationToken cancellationToken);
}
