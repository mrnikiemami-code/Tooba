using Tooba.BuildingBlocks;
using Tooba.Returns.Domain.Events;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Domain.Aggregates;


/// <summary>
/// خط مرجوعی با snapshot قیمت سفارش.
/// </summary>
public sealed class ReturnItem
{
    private ReturnItem()
    {
    }

    /// <summary>شناسه خط مرجوعی.</summary>
    public Guid ReturnItemId { get; init; }

    /// <summary>درخواست مالک.</summary>
    public Guid ReturnRequestId { get; init; }

    /// <summary>خط سفارش مرجع.</summary>
    public Guid OrderLineId { get; init; }

    /// <summary>تعداد درخواستی.</summary>
    public decimal Quantity { get; init; }

    /// <summary>snapshot قیمت واحد.</summary>
    public decimal UnitPriceSnapshot { get; init; }

    /// <summary>snapshot ارز.</summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>رزرو موجودی مرجع؛ FK Inventory نیست.</summary>
    public Guid? ReservationId { get; init; }

    /// <summary>TB-265: فیزیک دریافت این خط؛ ردیف‌های قدیمی همیشه Usable خوانده می‌شوند.</summary>
    public ReturnItemCondition Condition { get; private set; } = ReturnItemCondition.Usable;

    /// <summary>TB-265: سرنوشت پایدار مقدار دریافت‌شده؛ واحد آسیب‌دیده هرگز saleable نمی‌شود.</summary>
    public ReturnItemDisposition Disposition { get; private set; } = ReturnItemDisposition.None;

    /// <summary>TB-265: مرسولهٔ مبدأ که این مقدار از آن بازگشته.</summary>
    public Guid? SourceShipmentId { get; init; }

    /// <summary>TB-265: رسید فیزیکی مبدأ (رسید دوم ساخته نمی‌شود).</summary>
    public Guid? SourceReceiptId { get; init; }

    internal static ReturnItem Create(
        Guid returnItemId,
        Guid returnRequestId,
        Guid orderLineId,
        decimal quantity,
        decimal unitPriceSnapshot,
        string currency,
        Guid? reservationId,
        ReturnItemCondition condition = ReturnItemCondition.Usable,
        Guid? sourceShipmentId = null,
        Guid? sourceReceiptId = null) =>
        new()
        {
            ReturnItemId = returnItemId,
            ReturnRequestId = returnRequestId,
            OrderLineId = orderLineId,
            Quantity = quantity,
            UnitPriceSnapshot = unitPriceSnapshot,
            Currency = currency.Trim(),
            ReservationId = reservationId,
            Condition = Enum.IsDefined(condition) ? condition : ReturnItemCondition.Usable,
            Disposition = condition == ReturnItemCondition.Damaged
                ? ReturnItemDisposition.DamagedNonSaleable
                : ReturnItemDisposition.None,
            SourceShipmentId = sourceShipmentId,
            SourceReceiptId = sourceReceiptId,
        };

    /// <summary>
    /// TB-265: سرنوشت فیزیکی را یک‌بار ثبت می‌کند. واحد آسیب‌دیده هرگز saleable نمی‌شود حتی اگر فراخوان اشتباه باشد.
    /// </summary>
    /// <summary>TB-265: records the physical disposition once (damaged never becomes saleable).</summary>
    public void MarkDisposition(ReturnItemDisposition disposition)
    {
        if (Condition == ReturnItemCondition.Damaged && disposition == ReturnItemDisposition.SaleableRestock)
        {
            throw new ContractOperationException("return.damaged_saleable_restock_refused");
        }

        if (Disposition != ReturnItemDisposition.None)
        {
            return;
        }

        Disposition = disposition;
    }
}
