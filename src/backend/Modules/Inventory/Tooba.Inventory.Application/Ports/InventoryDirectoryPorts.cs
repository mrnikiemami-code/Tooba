using Tooba.Inventory.Application.Orders;
using Tooba.Inventory.Domain.ValueObjects;
using Tooba.Inventory.Domain.Aggregates;
using Tooba.Inventory.Domain.Events;

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
        decimal quantity,
        string reason,
        string? idempotencyKey,
        CancellationToken cancellationToken);

    /// <summary>
    /// مقدار قابل‌فروش را اتمی رزرو می‌کند.
    /// </summary>
    Task<ReservationReceipt> ReserveAsync(
        Guid stockItemId,
        decimal quantity,
        string? externalReference,
        string? idempotencyKey,
        DateTimeOffset? expiresAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// رزرو Held را آزاد می‌کند.
    /// </summary>
    Task ReleaseAsync(Guid reservationId, CancellationToken cancellationToken);

    /// <summary>
    /// رزرو را با شناسه می‌خواند؛ وضعیت Released هم برمی‌گردد.
    /// </summary>
    Task<ReservationReceipt?> FindReservationAsync(Guid reservationId, CancellationToken cancellationToken);

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

    /// <summary>رزرو Held سفارش پرداخت‌شده را از TTL سبد خارج می‌کند (ExpiresAt=null). Idempotent.</summary>
    Task<ReservationReceipt> CommitReservationForPaidOrderAsync(Guid reservationId, CancellationToken cancellationToken);

    /// <summary>
    /// رزرو Held را به مهلت بررسی پرداخت دستی ارتقا می‌دهد (جایگزین TTL سبد). Idempotent.
    /// </summary>
    Task<ReservationReceipt> PromoteReservationForManualPaymentReviewAsync(
        Guid reservationId,
        DateTimeOffset reviewExpiresAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// قابلیت مرکزی تأمین سفارش: reuse hold معتبر یا بازگیری authoritative (بدون زنده کردن Released).
    /// </summary>
    Task<EnsureOrderSupplyResult> EnsureOrderSupplyAsync(
        EnsureOrderSupplyRequest request,
        CancellationToken cancellationToken);

    /// <summary>تصویر تأمین بدون mutation (CheckOnly).</summary>
    Task<OrderSupplyStatus> GetOrderSupplyStatusAsync(
        Guid checkoutId,
        IReadOnlyList<OrderSupplyLineInput> lines,
        CancellationToken cancellationToken);
}
