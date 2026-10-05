using Tooba.Inventory.Domain.ValueObjects;

namespace Tooba.Inventory.Application.Ports;

/// <summary>
/// نتیجهٔ رزرو. سبد خرید ساخته نمی‌شود.
/// </summary>
public sealed record ReservationReceipt(
    Guid ReservationId,
    Guid StockItemId,
    Guid OfferId,
    decimal Quantity,
    StockReservationStatus Status,
    DateTimeOffset? ExpiresAt);
