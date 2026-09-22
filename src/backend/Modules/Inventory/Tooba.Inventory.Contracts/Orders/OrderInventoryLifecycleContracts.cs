#pragma warning disable CS1591
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

public sealed record OrderInventorySupplyLine(
    Guid OrderLineId,
    Guid OfferId,
    Guid? CurrentReservationId,
    decimal RemainingQuantity,
    string? ItemTitle,
    string? UnitCode);

public sealed record OrderInventoryUnpaidRetryRequest(
    Guid CheckoutId,
    bool AllowReacquire,
    string Reason,
    IReadOnlyList<OrderInventorySupplyLine> Lines);

public sealed record OrderInventorySupplyResult(
    string Status,
    string Outcome,
    IReadOnlyDictionary<Guid, Guid> NewBindingsByOrderLineId);

public sealed record OrderInventorySupplyStatusSnapshot(Guid CheckoutId, string Status);

/// <summary>رزرو قابل مشاهده برای بازیابی سفارش (بدون Domain Inventory).</summary>
public sealed record OrderInventoryReservationView(
    Guid ReservationId,
    Guid StockItemId,
    decimal Quantity,
    string Status,
    DateTimeOffset? ExpiresAt);

/// <summary>جزئیات کمبود یک خط تأمین برای Order.</summary>
public sealed record OrderInventorySupplyLineDetail(
    Guid OrderLineId,
    string? ItemTitle,
    string? UnitCode,
    decimal Required,
    decimal Available,
    decimal Shortage,
    string LineStatus,
    Guid? BoundReservationId);

/// <summary>وضعیت تأمین با خطوط کمبود.</summary>
public sealed record OrderInventorySupplyStatusDetail(
    Guid CheckoutId,
    string Status,
    IReadOnlyList<OrderInventorySupplyLineDetail> Lines);

/// <summary>درخواست Ensure کامل با mode پایدار (نام enum مالک Inventory).</summary>
public sealed record OrderInventoryEnsureDetailRequest(
    Guid CheckoutId,
    string Mode,
    bool AllowReacquire,
    string Reason,
    string? CorrelationId,
    DateTimeOffset? ReviewExpiresAt,
    IReadOnlyList<OrderInventorySupplyLine> Lines);

/// <summary>نتیجهٔ Ensure با جزئیات خطوط و bindingها.</summary>
public sealed record OrderInventoryEnsureDetailResult(
    string Outcome,
    string Status,
    IReadOnlyList<OrderInventorySupplyLineDetail> Lines,
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

    Task<OrderInventorySupplyResult> EnsureUnpaidRetryHoldAsync(
        OrderInventoryUnpaidRetryRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, OrderInventorySupplyStatusSnapshot>> GetSupplyStatusesAsync(
        IReadOnlyDictionary<Guid, IReadOnlyList<OrderInventorySupplyLine>> linesByCheckoutId,
        CancellationToken cancellationToken);

    /// <summary>رزرو را برای ارزیابی بازیابی می‌خواند؛ Released هم برمی‌گردد.</summary>
    Task<OrderInventoryReservationView?> FindReservationAsync(
        Guid reservationId,
        CancellationToken cancellationToken);

    /// <summary>رزرو جدید authoritative (بدون زنده کردن Released).</summary>
    Task<OrderInventoryReservationView> ReserveAsync(
        Guid stockItemId,
        decimal quantity,
        string? externalReference,
        string? idempotencyKey,
        DateTimeOffset? expiresAt,
        CancellationToken cancellationToken);

    /// <summary>رزرو Held پرداخت‌شده را از TTL خارج می‌کند.</summary>
    Task CommitReservationForPaidOrderAsync(Guid reservationId, CancellationToken cancellationToken);

    /// <summary>وضعیت تأمین با خطوط کمبود (CheckOnly).</summary>
    Task<OrderInventorySupplyStatusDetail> GetSupplyStatusDetailAsync(
        Guid checkoutId,
        IReadOnlyList<OrderInventorySupplyLine> lines,
        CancellationToken cancellationToken);

    /// <summary>Ensure کامل با mode/expiry برای آهنگ‌سازی Order.</summary>
    Task<OrderInventoryEnsureDetailResult> EnsureSupplyDetailAsync(
        OrderInventoryEnsureDetailRequest request,
        CancellationToken cancellationToken);
}
