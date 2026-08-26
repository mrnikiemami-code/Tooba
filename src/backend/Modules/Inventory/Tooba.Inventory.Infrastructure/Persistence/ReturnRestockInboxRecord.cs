namespace Tooba.Inventory.Infrastructure.Persistence;

/// <summary>
/// dedup idempotent restock مرجوعی در schema inventory.
/// </summary>
public sealed class ReturnRestockInboxRecord
{
    /// <summary>کلید idempotency یکتا.</summary>
    public string IdempotencyKey { get; init; } = string.Empty;

    /// <summary>رزرو مرجع.</summary>
    public Guid ReservationId { get; init; }

    /// <summary>تعداد restock.</summary>
    public int Quantity { get; init; }

    /// <summary>زمان پردازش.</summary>
    public DateTimeOffset ProcessedAt { get; init; }
}
