using Tooba.BuildingBlocks;
using Tooba.Inventory.Domain.ValueObjects;

namespace Tooba.Inventory.Domain.Aggregates;

/// <summary>
/// رزرو مقدار روی یک موقعیت. Cart/Order اینجا نوع دامنه نیستند.
/// </summary>
public sealed class StockReservation
{
    /// <summary>
    /// شناسهٔ پایدار رزرو برای درز آیندهٔ سبد/سفارش.
    /// </summary>
    public Guid ReservationId { get; init; }

    /// <summary>
    /// موقعیت موجودی.
    /// </summary>
    public Guid StockItemId { get; init; }

    /// <summary>
    /// مقدار قفل‌شده.
    /// </summary>
    public decimal Quantity { get; init; }

    /// <summary>
    /// وضعیت چرخهٔ رزرو.
    /// </summary>
    public StockReservationStatus Status { get; private set; }

    /// <summary>
    /// مرجع خارجی اختیاری مثل کلید سبد آینده؛ نوع Cart نیست.
    /// </summary>
    public string? ExternalReference { get; init; }

    /// <summary>
    /// کلید تکرارناپذیری اختیاری.
    /// </summary>
    public string? IdempotencyKey { get; private set; }

    /// <summary>
    /// زمان ایجاد UTC.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// زمان آخرین تغییر UTC.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// مهلت UTC آزادسازی خودکار؛ تهی یعنی بدون انقضای زمانی در این رزرو.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    /// <summary>
    /// رزرو Held می‌سازد.
    /// </summary>
    public static StockReservation Hold(
        Guid reservationId,
        Guid stockItemId,
        decimal quantity,
        string? externalReference,
        string? idempotencyKey,
        DateTimeOffset now,
        DateTimeOffset? expiresAt)
    {
        if (reservationId == Guid.Empty || stockItemId == Guid.Empty)
        {
            throw new ContractOperationException("inventory.reservation.id_required");
        }

        if (quantity <= 0)
        {
            throw new ContractOperationException("inventory.reservation.quantity_invalid");
        }

        if (expiresAt is { } expiry && expiry <= now)
        {
            throw new ContractOperationException("inventory.reservation.expiry_invalid");
        }

        return new StockReservation
        {
            ReservationId = reservationId,
            StockItemId = stockItemId,
            Quantity = quantity,
            Status = StockReservationStatus.Held,
            ExternalReference = string.IsNullOrWhiteSpace(externalReference) ? null : externalReference.Trim(),
            IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            ExpiresAt = expiresAt,
        };
    }

    /// <summary>
    /// رزرو Held سفارش پرداخت‌شده را از TTL سبد خارج می‌کند (ExpiresAt=null). Idempotent.
    /// Released/Consumed را زنده نمی‌کند.
    /// </summary>
    public void CommitForPaidOrder(DateTimeOffset now)
    {
        if (Status is StockReservationStatus.Released or StockReservationStatus.Consumed)
        {
            throw new ContractOperationException("inventory.reservation.not_active");
        }

        if (Status != StockReservationStatus.Held)
        {
            throw new ContractOperationException("inventory.reservation.not_active");
        }

        if (ExpiresAt is null)
        {
            UpdatedAt = now;
            return;
        }

        ExpiresAt = null;
        UpdatedAt = now;
    }

    /// <summary>
    /// رزرو Held را از TTL سبد به مهلت بررسی پرداخت دستی ارتقا می‌دهد. Released/Consumed را زنده نمی‌کند.
    /// Idempotent: اگر مهلت فعلی ≥ مهلت بررسی باشد فقط UpdatedAt را تازه می‌کند.
    /// </summary>
    public void PromoteForManualPaymentReview(DateTimeOffset reviewExpiresAt, DateTimeOffset now)
    {
        if (Status is StockReservationStatus.Released or StockReservationStatus.Consumed)
        {
            throw new ContractOperationException("inventory.reservation.not_active");
        }

        if (Status != StockReservationStatus.Held)
        {
            throw new ContractOperationException("inventory.reservation.not_active");
        }

        if (reviewExpiresAt <= now)
        {
            throw new ContractOperationException("inventory.reservation.review_expiry_invalid");
        }

        if (ExpiresAt is { } existing && existing >= reviewExpiresAt)
        {
            UpdatedAt = now;
            return;
        }

        ExpiresAt = reviewExpiresAt;
        UpdatedAt = now;
    }

    /// <summary>
    /// وضعیت را عوض می‌کند.
    /// </summary>
    public void MoveTo(StockReservationStatus status, DateTimeOffset now)
    {
        if (Status != StockReservationStatus.Held)
        {
            throw new ContractOperationException("inventory.reservation.not_active");
        }

        Status = status;
        UpdatedAt = now;
        IdempotencyKey = null;
    }
}
