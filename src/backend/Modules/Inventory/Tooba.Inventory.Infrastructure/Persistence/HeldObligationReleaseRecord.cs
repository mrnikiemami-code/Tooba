namespace Tooba.Inventory.Infrastructure.Persistence;

/// <summary>
/// TB-265-R2: the durable identity of ONE requested release of an unconsumed order hold.
/// <para>
/// The actual release effect and the REQUEST that caused it are different facts: an Effect row stores how much was
/// really released (which may be less than requested when only part of the hold was still Held), so it can never be
/// used to answer a different request. This row keeps the immutable requested identity — purpose, idempotency key,
/// reservation and requested quantity — next to the actual effect, so an exact replay answers its own recorded amount
/// and a changed reservation/quantity is a typed conflict with zero writes.
/// </para>
/// </summary>
public sealed class HeldObligationReleaseRecord
{
    /// <summary>کلید idempotency یکتای درخواست.</summary>
    public string IdempotencyKey { get; init; } = string.Empty;

    /// <summary>هدف مالکانهٔ آزادسازی (هرگز با restock مرجوعی مخلوط نمی‌شود).</summary>
    public string Purpose { get; init; } = string.Empty;

    /// <summary>رزرو درخواست‌شده.</summary>
    public Guid ReservationId { get; init; }

    /// <summary>مقدار درخواست‌شدهٔ همین درخواست (نه مقدار واقعی آزادشده).</summary>
    public decimal RequestedQuantity { get; init; }

    /// <summary>مقدار واقعی آزادشده.</summary>
    public decimal ReleasedQuantity { get; init; }

    /// <summary>نتیجهٔ durable: Released | PartiallyHeld | Consumed | AlreadyReleased.</summary>
    public string Outcome { get; init; } = string.Empty;

    /// <summary>زمان پردازش.</summary>
    public DateTimeOffset ProcessedAt { get; init; }
}
