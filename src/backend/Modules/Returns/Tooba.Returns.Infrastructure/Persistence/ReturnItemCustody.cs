namespace Tooba.Returns.Infrastructure.Persistence;

/// <summary>
/// TB-279: one audited ACTUAL handback of rejected received goods to the owning customer.
/// <para>
/// A rejected received portion stays customer-owned custody work; it is never restocked because another portion was
/// refunded. This row records that a specific quantity of a specific condition was really handed back to the owning
/// customer (in person or with verified delivered proof) — a tracking number alone is not delivery, and no carrier
/// booking or Fulfillment shipment status is invented. The fact moves no money and no stock; it only discharges the
/// custody obligation it names, and it is idempotent by <see cref="OccurrenceKey"/>.
/// </para>
/// </summary>
public sealed class ReturnItemCustody
{
    /// <summary>شناسهٔ ردیف.</summary>
    public Guid ReturnCustodyId { get; init; }

    /// <summary>پروندهٔ مالک.</summary>
    public Guid ReturnRequestId { get; init; }

    /// <summary>خط مرجوعی تحویل‌داده‌شده.</summary>
    public Guid ReturnItemId { get; init; }

    /// <summary>مقدار واقعاً تحویل‌داده‌شده به مشتری مالک.</summary>
    public decimal Quantity { get; init; }

    /// <summary>وضعیت فیزیکی این مقدار: Usable | Damaged.</summary>
    public string Condition { get; init; } = string.Empty;

    /// <summary>مشتری مالک که کالا به او تحویل داده شد.</summary>
    public Guid RecipientUserId { get; init; }

    /// <summary>مرجع مدرک تحویل (حضوری یا تحویل تأییدشده)؛ هرگز شمارهٔ رهگیری تنها نیست.</summary>
    public string ProofReference { get; init; } = string.Empty;

    /// <summary>اپراتور ثبت‌کننده.</summary>
    public Guid ActorUserId { get; init; }

    /// <summary>شناسهٔ رخداد بازبینی‌شده.</summary>
    public string OccurrenceKey { get; init; } = string.Empty;

    /// <summary>زمان ثبت.</summary>
    public DateTimeOffset RecordedAt { get; init; }
}
