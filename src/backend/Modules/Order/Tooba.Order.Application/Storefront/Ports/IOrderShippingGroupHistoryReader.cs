namespace Tooba.Order.Application.Storefront.Ports;

/// <summary>
/// TB240-R4: درز <b>فقط-خواندنی</b> گروه‌های ارسالِ یک سفارشِ ثبت‌شده، همراه شاهد فشردهٔ پذیرفته‌شده که در
/// خود Order ذخیره شده است.
/// </summary>
/// <remarks>
/// <para>
/// این درز هیچ‌گاه Shipping/تعرفه/گروه منطقه‌ای را نمی‌خواند و هیچ قیمتی را بازمحاسبه نمی‌کند؛ فقط ردیف‌های
/// پایدار `order.order_shipping_groups` همان checkout را برمی‌گرداند. مالکیت/مجوز در لایهٔ بالاتر (همان خواندن
/// مجاز سفارش) اثبات شده است؛ اینجا فقط داده خوانده می‌شود.
/// </para>
/// </remarks>
public interface IOrderShippingGroupHistoryReader
{
    /// <summary>گروه‌های پایدار یک checkout؛ آرایهٔ خالی وقتی گروهی ثبت نشده است.</summary>
    Task<IReadOnlyList<OrderShippingGroupHistoryRecord>> GetForCheckoutAsync(
        Guid checkoutId,
        CancellationToken cancellationToken);
}

/// <summary>یک گروه ارسال پایدار با شاهد فشردهٔ اختیاری (همهٔ مقادیر عیناً از Order storage).</summary>
public sealed record OrderShippingGroupHistoryRecord(
    Guid ShippingGroupId,
    Guid SellerOrderId,
    Guid SellerPartyId,
    string MethodCode,
    string? ServiceCode,
    string? OptionCode,
    string MethodLabel,
    decimal ShippingChargeSnapshot,
    string Currency,
    DateOnly MinimumDeliveryDate,
    DateOnly? RequestedDeliveryDate,
    string? RequestedDeliveryTimeWindow,
    int LeadDays,
    string State,
    IReadOnlyList<Guid> OrderLineIds,
    OrderShippingGroupHistoryEvidence? Evidence);

/// <summary>شاهد فشردهٔ پذیرفته‌شدهٔ همان گروه، بدون هیچ تفسیر/بازمحاسبه.</summary>
public sealed record OrderShippingGroupHistoryEvidence(
    string OwnerKind,
    string? OwnerTenantKey,
    Guid? OwnerSellerPartyId,
    Guid SnapshotId,
    Guid DraftId,
    long SourceRevision,
    int FormatVersion,
    long SelectionVersion,
    Guid AreaGroupId,
    Guid RateRowId,
    Guid? ShippingTypeId,
    decimal FreightAmount,
    decimal PackagingAmount,
    decimal PayableAtCheckoutAmount,
    decimal CarrierCollectAmount,
    DateOnly ReceivingDate,
    long ReceivingOpenTicks,
    long ReceivingCloseTicks,
    string ReceivingTimeZone,
    int? MinimumDeliveryDays,
    int? MaximumDeliveryDays);
