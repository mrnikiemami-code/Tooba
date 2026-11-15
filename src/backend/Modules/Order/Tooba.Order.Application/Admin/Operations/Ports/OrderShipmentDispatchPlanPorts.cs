using Tooba.Order.Domain.Aggregates;
using Tooba.Order.Domain.Checkout;

namespace Tooba.Order.Application.Admin.Operations.Ports;

/// <summary>
/// TB-270-R2 — شاهد پذیرفته‌شدهٔ <b>همان</b> گروه ارسال به‌همراه هویت روش/خدمت/گزینه، برای ساخت درخواست
/// خواندن ورودی‌های زمان‌بندی.
/// </summary>
/// <remarks>
/// همهٔ مقادیر از موجودی Order همان گروه خوانده می‌شوند؛ هیچ‌کدام از مرورگر پذیرفته نمی‌شود و هیچ تنظیم جاری
/// Shipping جایگزین شاهد تاریخی نمی‌شود. گروه بدون شاهد (ردیف‌های تاریخی) مقدار <c>null</c> می‌دهد.
/// </remarks>
public sealed record AcceptedShipmentGroupBinding(
    Guid CheckoutId,
    Guid SellerOrderId,
    Guid SellerPartyId,
    Guid ShippingGroupId,
    string MethodCode,
    string MethodLabel,
    string ServiceCode,
    string? OptionCode,
    ShippingGroupOwnerKind OwnerKind,
    string? OwnerTenantKey,
    Guid? OwnerSellerPartyId,
    Guid SnapshotId,
    Guid DraftId,
    long SourceRevision,
    int FormatVersion,
    long SelectionVersion,
    Guid AreaGroupId,
    Guid RateRowId,
    DateOnly AcceptedReceivingDate,
    long AcceptedReceivingOpenTicks,
    long AcceptedReceivingCloseTicks,
    string AcceptedReceivingTimeZone,
    int? MinimumDeliveryDays,
    int? MaximumDeliveryDays);

/// <summary>TB-270-R2: خواندن شاهد پذیرفته‌شدهٔ یک گروه ارسال متعهد در همان checkout.</summary>
public interface IOrderAcceptedShipmentBindingReader
{
    /// <summary>شاهد گروه یا <c>null</c> وقتی گروه/شاهد وجود ندارد.</summary>
    Task<AcceptedShipmentGroupBinding?> GetAsync(
        Guid checkoutId,
        Guid shippingGroupId,
        CancellationToken cancellationToken);
}

/// <summary>نتیجهٔ تلاش نوشتن یک برنامهٔ تأییدشده.</summary>
public enum OrderShipmentDispatchPlanInsertOutcome
{
    /// <summary>ردیف تازه نوشته شد.</summary>
    Inserted = 0,

    /// <summary>همان قصد قبلاً پایدار شده است؛ همان ردیف برمی‌گردد.</summary>
    DuplicateExact = 1,

    /// <summary>برای همین مرسوله قصد متفاوتی ثبت شده است؛ هیچ نوشتنی انجام نشد.</summary>
    DuplicateConflict = 2,
}

/// <summary>
/// TB-270-R2: ذخیره/خواندن برنامه‌های ارسال مستقل مرسوله‌ها. تنها مالک این ردیف‌ها Order است.
/// </summary>
public interface IOrderShipmentDispatchPlanStore
{
    /// <summary>برنامهٔ همین مرسوله یا <c>null</c>.</summary>
    Task<OrderShipmentDispatchPlan?> FindAsync(Guid shipmentId, CancellationToken cancellationToken);

    /// <summary>برنامه‌های همین مرسوله‌ها (فقط ردیف‌های موجود).</summary>
    Task<IReadOnlyList<OrderShipmentDispatchPlan>> ListAsync(
        IReadOnlyList<Guid> shipmentIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// یک ردیف تغییرناپذیر می‌نویسد. یکتایی <c>shipment_id</c> در پایگاه‌داده داور مسابقهٔ دو تأییدیهٔ
    /// مستقل است؛ ردیف موجود هرگز بازنویسی نمی‌شود.
    /// </summary>
    Task<OrderShipmentDispatchPlanInsertOutcome> TryInsertAsync(
        OrderShipmentDispatchPlan plan,
        CancellationToken cancellationToken);
}
