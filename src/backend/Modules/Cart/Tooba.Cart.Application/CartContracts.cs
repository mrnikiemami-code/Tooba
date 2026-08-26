using Tooba.Cart.Domain;
using Tooba.Offer.Domain;

namespace Tooba.Cart.Application;

/// <summary>
/// دسترسی به سبد. CartId به‌تنهایی Bearer نیست.
/// </summary>
public sealed record CartAccess(Guid? UserId, string? GuestSecret);

/// <summary>
/// خط سبد برای خواندن و درز Checkout آینده. موجودیت EF نیست.
/// </summary>
public sealed record CartLineSnapshot(
    Guid LineId,
    Guid OfferId,
    Guid CatalogVariantId,
    Guid SellerPartyId,
    int Quantity,
    Guid? ReservationId,
    decimal? QuotedAmount,
    string? QuotedCurrency,
    bool QuotedTaxExclusive,
    Guid? PriceId,
    DateTimeOffset QuotedAt);

/// <summary>
/// نمای سبد بدون نشت EF. حقیقت تسویه یا سفارش نیست.
/// </summary>
public sealed record CartSnapshot(
    Guid CartId,
    CartStatus Status,
    CartAccessKind AccessKind,
    Guid? OwnerUserId,
    string Market,
    string Currency,
    SalesChannel Channel,
    DateTimeOffset? ExpiresAt,
    CartConversionIntent ConversionIntent,
    int Version,
    IReadOnlyList<CartLineSnapshot> Lines);

/// <summary>
/// نتیجهٔ ساخت سبد مهمان؛ راز خام فقط یک‌بار برمی‌گردد.
/// </summary>
public sealed record GuestCartCreated(CartSnapshot Cart, string GuestSecret);

/// <summary>
/// درز خواندن سبد برای Checkout آینده بدون نشت EF.
/// </summary>
public interface ICartQueryGateway
{
    /// <summary>
    /// سبد را پس از احراز دسترسی برمی‌گرداند. CartId تنها کافی نیست.
    /// </summary>
    Task<CartSnapshot?> GetCartAsync(Guid cartId, CartAccess access, CancellationToken cancellationToken);
}

/// <summary>
/// درز نگهبان مجوز Cart. ماتریس نهایی هویت اینجا نیست.
/// </summary>
public interface ICartUseCaseGuard
{
    /// <summary>
    /// اجازهٔ نوشتن سبد را بررسی می‌کند. پیاده‌سازی فعلی فقط درز است.
    /// </summary>
    Task EnsureCanMutateAsync(CancellationToken cancellationToken);
}

/// <summary>
/// نوشتن foundation سبد. Checkout، Order و Payment اینجا نیستند.
/// </summary>
public interface ICartDirectory
{
    /// <summary>
    /// سبد واردشده می‌سازد.
    /// </summary>
    Task<CartSnapshot> CreateAuthenticatedAsync(
        Guid userId,
        string market,
        string currency,
        SalesChannel channel,
        CancellationToken cancellationToken);

    /// <summary>
    /// سبد مهمان می‌سازد و راز یک‌بارمصرف را برمی‌گرداند.
    /// </summary>
    Task<GuestCartCreated> CreateGuestAsync(
        string market,
        string currency,
        SalesChannel channel,
        CancellationToken cancellationToken);

    /// <summary>
    /// خط Offer اضافه یا با همان Offer ادغام می‌کند پس از رزرو موجودی.
    /// </summary>
    Task<CartSnapshot> AddOrIncreaseLineAsync(
        Guid cartId,
        CartAccess access,
        int expectedVersion,
        Guid offerId,
        int quantity,
        CancellationToken cancellationToken);

    /// <summary>
    /// تعداد خط را عوض می‌کند. صفر یعنی حذف پس از آزادسازی رزرو.
    /// </summary>
    Task<CartSnapshot> ChangeLineQuantityAsync(
        Guid cartId,
        CartAccess access,
        int expectedVersion,
        Guid lineId,
        int quantity,
        CancellationToken cancellationToken);

    /// <summary>
    /// خط را حذف می‌کند و رزرو را آزاد می‌کند.
    /// </summary>
    Task<CartSnapshot> RemoveLineAsync(
        Guid cartId,
        CartAccess access,
        int expectedVersion,
        Guid lineId,
        CancellationToken cancellationToken);

    /// <summary>
    /// سبد را رها می‌کند و رزروها را آزاد می‌کند.
    /// </summary>
    Task AbandonAsync(Guid cartId, CartAccess access, int expectedVersion, CancellationToken cancellationToken);

    /// <summary>
    /// سبدهای سررسیدشده را به‌صورت batch با SKIP LOCKED منقضی و رزرو منقضی Inventory را آزاد می‌کند.
    /// </summary>
    /// <returns>تعداد سبدهای منقضی‌شده.</returns>
    Task<int> ExpireDueCartsAsync(DateTimeOffset utcNow, int batchSize, CancellationToken cancellationToken);

    /// <summary>
    /// درز تبدیل را بدون ساختن Order ثبت می‌کند.
    /// </summary>
    Task<CartSnapshot> ConvertAsync(
        Guid cartId,
        CartAccess access,
        int expectedVersion,
        CartConversionIntent intent,
        CancellationToken cancellationToken);
}
