namespace Tooba.Returns.Infrastructure.Persistence;

/// <summary>
/// TB-279: the durable per-delivery-slice allocation of a claimed return quantity.
/// <para>
/// A return window is a property of the DELIVERY it belongs to. Before this row existed the evaluator subtracted
/// every historical return of a line from the units still inside the window, so an old return could consume a newly
/// delivered batch (or the reverse). Each new claim therefore binds its exact quantity to the owning delivered slices
/// it actually comes from, and eligibility subtracts claims only from THEIR slices. The allocation is written under
/// the existing per-seller-order serialization, so the same source can never fund two sibling cases.
/// </para>
/// <para>
/// A released allocation (rejected/cancelled case, or a rejected resolution portion) stops occupying its slice but is
/// kept as history. Ambiguous legacy rows are never backfilled or reassigned: the read reports the exact blocker.
/// </para>
/// </summary>
public sealed class ReturnDeliveryAllocation
{
    /// <summary>شناسهٔ تخصیص.</summary>
    public Guid ReturnAllocationId { get; init; }

    /// <summary>پروندهٔ مالک.</summary>
    public Guid ReturnRequestId { get; init; }

    /// <summary>خط مرجوعی مالک.</summary>
    public Guid ReturnItemId { get; init; }

    /// <summary>خط سفارش مالک برش.</summary>
    public Guid OrderLineId { get; init; }

    /// <summary>شناسهٔ پایدار برش تحویل مالک (مرسولهٔ تحویل‌شده).</summary>
    public string SliceKey { get; init; } = string.Empty;

    /// <summary>مقدار تخصیص‌یافته از این برش.</summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// TB-279-R1 (F3): shrinks this claim to the portion still physically held. A handback (or the never-received
    /// remainder of a rejection) releases capacity without inventing a new slice or rewriting the delivery instant.
    /// </summary>
    public void ReduceQuantity(decimal quantity)
    {
        if (quantity <= 0m || quantity >= Quantity)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        Quantity -= quantity;
    }

    /// <summary>زمان تحویل همان برش (برای محاسبهٔ پنجره؛ هرگز از امروز مرورگر).</summary>
    public DateTimeOffset DeliveredAt { get; init; }

    /// <summary>زمان آزادسازی تخصیص (رد/لغو)؛ null یعنی هنوز ظرفیت را اشغال می‌کند.</summary>
    public DateTimeOffset? ReleasedAt { get; set; }

    /// <summary>زمان ثبت تخصیص.</summary>
    public DateTimeOffset RecordedAt { get; init; }
}
