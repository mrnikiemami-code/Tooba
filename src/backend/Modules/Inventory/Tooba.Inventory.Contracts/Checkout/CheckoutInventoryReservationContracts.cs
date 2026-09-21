namespace Tooba.Inventory.Contracts.Checkout;

/// <summary>خط سبد برای رزرو checkout؛ موجودیت Inventory نیست.</summary>
public sealed record CheckoutInventoryLineRequest(
    Guid CartLineId,
    Guid OfferId,
    decimal Quantity,
    Guid? ExistingReservationId);

/// <summary>درخواست رزرو خطوط checkout با هویت فرآیند.</summary>
public sealed record CheckoutInventoryReservationRequest(
    Guid CartId,
    Guid? ProcessId,
    string CorrelationId,
    DateTimeOffset Now,
    DateTimeOffset? ExpiresAt,
    IReadOnlyList<CheckoutInventoryLineRequest> Lines);

/// <summary>نتیجهٔ رزرو یک خط سبد.</summary>
public sealed record CheckoutInventoryLineReservation(
    Guid CartLineId,
    Guid ReservationId);

/// <summary>نتیجهٔ رزرو مجموعهٔ خطوط checkout.</summary>
public sealed record CheckoutInventoryReservationResult(
    IReadOnlyList<CheckoutInventoryLineReservation> Lines)
{
    /// <summary>نگاشت LineId → ReservationId.</summary>
    public IReadOnlyDictionary<Guid, Guid> ByCartLineId =>
        Lines.ToDictionary(x => x.CartLineId, x => x.ReservationId);
}

/// <summary>
/// درز پایدار رزرو موجودی برای هماهنگی checkout.
/// انتخاب محل و معنای رزرو در Inventory می‌ماند؛ آمادهٔ آداپتر آیندهٔ HTTP/gRPC/message.
/// </summary>
public interface ICheckoutInventoryReservationPort
{
    /// <summary>خطوط سبد را برای commit سفارش رزرو می‌کند؛ در شکست، رزروهای همین درخواست آزاد می‌شوند.</summary>
    Task<CheckoutInventoryReservationResult> ReserveForCheckoutAsync(
        CheckoutInventoryReservationRequest request,
        CancellationToken cancellationToken);

    /// <summary>رزروهای Held را آزاد می‌کند (بهترین تلاش).</summary>
    Task ReleaseAsync(IEnumerable<Guid> reservationIds, CancellationToken cancellationToken);
}
