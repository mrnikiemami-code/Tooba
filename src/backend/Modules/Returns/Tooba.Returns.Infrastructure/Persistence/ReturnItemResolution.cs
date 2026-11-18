namespace Tooba.Returns.Infrastructure.Persistence;

/// <summary>
/// TB-279: one reviewed per-item quantity resolution of a legacy multi-item case.
/// <para>
/// The originally requested line/quantity/price stays the immutable audit claim. A resolution says, for ONE requested
/// item, how much of that claim is ACCEPTED (refundable) and how much is REJECTED/withdrawn — never more than was
/// requested, and the accepted portion can never exceed the quantity that was physically received and inspected. The
/// approved money is derived from the accepted quantities only, so a partial resolution can never silently refund the
/// whole original claim.
/// </para>
/// <para>
/// Rows are append-only by <see cref="Version"/>; the effective plan is the highest version per item. A row is written
/// only under the case row lock and the owning seller-order serialization, and never after money started.
/// </para>
/// </summary>
public sealed class ReturnItemResolution
{
    /// <summary>پروندهٔ مالک.</summary>
    public Guid ReturnRequestId { get; init; }

    /// <summary>خط مرجوعی مرجع (کالای دقیقاً همین پرونده).</summary>
    public Guid ReturnItemId { get; init; }

    /// <summary>شمارهٔ افزایشی نسخهٔ برنامهٔ حل‌وفصل.</summary>
    public int Version { get; init; }

    /// <summary>مقدار پذیرفته‌شده (مبنای بازگشت وجه).</summary>
    public decimal AcceptedQuantity { get; init; }

    /// <summary>مقدار ردشده یا انصراف‌داده‌شده (بدون بازگشت وجه و بدون رسید ساختگی).</summary>
    public decimal RejectedQuantity { get; init; }

    /// <summary>
    /// TB-279-R1 (F2): the accepted portion of the inspected USABLE condition. Null on a pre-R1 row whose accepted
    /// condition was never reviewed — such a row is read as an incomplete plan, never as a usable-first split.
    /// </summary>
    public decimal? AcceptedUsableQuantity { get; init; }

    /// <summary>TB-279-R1 (F2): the accepted portion of the inspected DAMAGED condition.</summary>
    public decimal? AcceptedDamagedQuantity { get; init; }

    /// <summary>TB-279-R1 (F3): declined but RECEIVED usable units, owed back to the owning customer.</summary>
    public decimal? RejectedReceivedUsableQuantity { get; init; }

    /// <summary>TB-279-R1 (F3): declined but RECEIVED damaged units, owed back to the owning customer.</summary>
    public decimal? RejectedReceivedDamagedQuantity { get; init; }

    /// <summary>TB-279-R1 (F2): the part of the claim that never physically arrived.</summary>
    public decimal? RejectedUnreceivedQuantity { get; init; }

    /// <summary>دلیل بازبینی‌شدهٔ این حل‌وفصل.</summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>اپراتور ثبت‌کننده.</summary>
    public Guid ActorUserId { get; init; }

    /// <summary>شناسهٔ رخداد بازبینی‌شدهٔ برنامه (یک رخداد، یک نسخه).</summary>
    public string OccurrenceKey { get; init; } = string.Empty;

    /// <summary>زمان ثبت.</summary>
    public DateTimeOffset RecordedAt { get; init; }
}
