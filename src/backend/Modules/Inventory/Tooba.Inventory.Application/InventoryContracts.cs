using Tooba.Inventory.Domain;

namespace Tooba.Inventory.Application;

/// <summary>
/// خلاصهٔ موجودی یک محل. موجودیت EF نیست.
/// </summary>
public sealed record LocationAvailability(
    Guid StockItemId,
    Guid LocationId,
    string LocationCode,
    int OnHand,
    int Reserved,
    int Available);

/// <summary>
/// نتیجهٔ خواندن موجودی Offer در Tenant جاری. قابل‌خرید بودن را تضمین نمی‌کند.
/// </summary>
public sealed record InventoryAvailability(
    Guid OfferId,
    Guid CatalogVariantId,
    int OnHand,
    int Reserved,
    int Available,
    IReadOnlyList<LocationAvailability> Locations);

/// <summary>
/// نتیجهٔ رزرو. سبد خرید ساخته نمی‌شود.
/// </summary>
public sealed record ReservationReceipt(
    Guid ReservationId,
    Guid StockItemId,
    Guid OfferId,
    int Quantity,
    StockReservationStatus Status,
    DateTimeOffset? ExpiresAt);

/// <summary>
/// درز خواندن موجودی بدون نشت EF.
/// </summary>
public interface IInventoryAvailabilityGateway
{
    /// <summary>
    /// موجودی Offer را در پایگاه Tenant/Marketplace جاری جمع می‌کند.
    /// </summary>
    Task<InventoryAvailability?> GetAvailabilityAsync(Guid offerId, CancellationToken cancellationToken);
}

/// <summary>
/// درز نگهبان مجوز Inventory. ماتریس انبار اینجا نیست.
/// </summary>
public interface IInventoryUseCaseGuard
{
    /// <summary>
    /// اجازهٔ نوشتن موجودی را بررسی می‌کند. پیاده‌سازی فعلی فقط درز است.
    /// </summary>
    Task EnsureCanMutateAsync(CancellationToken cancellationToken);
}

/// <summary>
/// نوشتن foundation موجودی. Cart و Order اینجا نیستند.
/// </summary>
public interface IInventoryDirectory
{
    /// <summary>
    /// محل نگهداری می‌سازد.
    /// </summary>
    Task<Guid> CreateLocationAsync(string code, string name, CancellationToken cancellationToken);

    /// <summary>
    /// موقعیت Offer در محل را باز می‌کند پس از تأیید Offer از قرارداد Lookup.
    /// </summary>
    Task<Guid> OpenPositionAsync(Guid offerId, Guid locationId, CancellationToken cancellationToken);

    /// <summary>
    /// موجودی را با دلیل اصلاح می‌کند.
    /// </summary>
    Task AdjustAsync(
        Guid stockItemId,
        StockAdjustmentKind kind,
        int quantity,
        string reason,
        string? idempotencyKey,
        CancellationToken cancellationToken);

    /// <summary>
    /// مقدار قابل‌فروش را اتمی رزرو می‌کند.
    /// </summary>
    Task<ReservationReceipt> ReserveAsync(
        Guid stockItemId,
        int quantity,
        string? externalReference,
        string? idempotencyKey,
        DateTimeOffset? expiresAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// رزرو Held را آزاد می‌کند.
    /// </summary>
    Task ReleaseAsync(Guid reservationId, CancellationToken cancellationToken);

    /// <summary>
    /// رزروهای Held منقضی‌شده را با زمان UTC سرور آزاد می‌کند؛ تایمر کلاینت نیست.
    /// </summary>
    /// <summary>
    /// رزروهای Held منقضی را batch-wise با SKIP LOCKED آزاد می‌کند.
    /// </summary>
    /// <returns>تعداد رزروهای آزادشده.</returns>
    Task<int> ReleaseExpiredHoldsAsync(DateTimeOffset utcNow, int batchSize, CancellationToken cancellationToken);

    /// <summary>
    /// رزرو Held را از OnHand کم می‌کند.
    /// </summary>
    Task ConsumeAsync(Guid reservationId, CancellationToken cancellationToken);
}
