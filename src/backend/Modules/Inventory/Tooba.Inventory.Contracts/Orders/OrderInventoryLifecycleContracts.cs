namespace Tooba.Inventory.Contracts.Orders;

/// <summary>خط سفارش برای تأمین Paid durable.</summary>
public sealed record OrderInventoryPaidSupplyLine(
    Guid OrderLineId,
    Guid OfferId,
    Guid? CurrentReservationId,
    decimal RemainingQuantity,
    string? ItemTitle,
    string? UnitCode);

/// <summary>درخواست تأمین پایدار پس از پرداخت تأییدشده.</summary>
public sealed record OrderInventoryPaidSupplyRequest(
    Guid CheckoutId,
    string Reason,
    string? CorrelationId,
    IReadOnlyList<OrderInventoryPaidSupplyLine> Lines);

/// <summary>نتیجهٔ تأمین Paid؛ فقط bindingهای جدید برای به‌روزرسانی خطوط سفارش.</summary>
public sealed record OrderInventoryPaidSupplyResult(
    IReadOnlyDictionary<Guid, Guid> NewBindingsByOrderLineId);

/// <summary>
/// درز پایدار Cancel / Restore / PaymentBridge برای هماهنگی سفارش با موجودی.
/// معنای رزرو و انتخاب StockItem در Inventory می‌ماند؛ آمادهٔ آداپتر آیندهٔ HTTP/gRPC.
/// </summary>
public interface IOrderInventoryLifecyclePort
{
    /// <summary>رزرو Held را آزاد می‌کند؛ خطا به فراخوان می‌رسد (مسیر Cancel).</summary>
    Task ReleaseHeldReservationAsync(Guid reservationId, CancellationToken cancellationToken);

    /// <summary>آزادسازی بهترین‌تلاش (rollback مسیر Restore).</summary>
    Task TryReleaseReservationAsync(Guid reservationId, CancellationToken cancellationToken);

    /// <summary>
    /// از رزرو قبلی hold پایدار (ExpiresAt=null) بازمی‌گیرد؛ StockItemId داخل Inventory می‌ماند.
    /// </summary>
    Task<Guid> ReacquireDurableHoldFromPreviousAsync(
        Guid previousReservationId,
        string externalReference,
        string idempotencyKey,
        CancellationToken cancellationToken);

    /// <summary>
    /// اگر Held باشد ارتقای مهلت بررسی دستی؛ وگرنه بازگیری authoritative تحت TTL بررسی.
    /// شناسهٔ رزرو مؤثر را برمی‌گرداند (ممکن است جدید باشد).
    /// </summary>
    Task<Guid> PromoteOrReacquireForManualPaymentReviewAsync(
        Guid reservationId,
        DateTimeOffset reviewExpiresAt,
        string externalReference,
        string idempotencyKey,
        CancellationToken cancellationToken);

    /// <summary>اگر Held باشد آزاد می‌کند؛ در غیر این صورت no-op (رد دستی پرداخت).</summary>
    Task ReleaseIfHeldAsync(Guid reservationId, CancellationToken cancellationToken);

    /// <summary>تضمین تأمین Paid durable با امکان بازگیری؛ bindingهای تازه را برمی‌گرداند.</summary>
    Task<OrderInventoryPaidSupplyResult> EnsurePaidDurableSupplyAsync(
        OrderInventoryPaidSupplyRequest request,
        CancellationToken cancellationToken);
}
