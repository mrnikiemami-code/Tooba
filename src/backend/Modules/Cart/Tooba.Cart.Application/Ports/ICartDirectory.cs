using Tooba.Cart.Application.Ports;
using Tooba.Cart.Contracts;
using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Cart.Application.Ports;

/// <summary>
/// نتیجهٔ ساخت سبد مهمان؛ راز خام فقط یک‌بار برمی‌گردد.
/// </summary>
public sealed record GuestCartCreated(CartSnapshot Cart, string GuestSecret);

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
    /// خط Offer اضافه یا ادغام می‌کند پس از اعتبارسنجی موجودی؛ رزرو سخت نمی‌سازد.
    /// </summary>
    Task<CartSnapshot> AddOrIncreaseLineAsync(
        Guid cartId,
        CartAccess access,
        int expectedVersion,
        Guid offerId,
        decimal quantity,
        CancellationToken cancellationToken,
        Guid? merchandisingCampaignId = null);

    /// <summary>
    /// تعداد خط را عوض می‌کند. صفر یعنی حذف؛ رزرو تاریخی سبد در صورت وجود آزاد می‌شود.
    /// </summary>
    Task<CartSnapshot> ChangeLineQuantityAsync(
        Guid cartId,
        CartAccess access,
        int expectedVersion,
        Guid lineId,
        decimal quantity,
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

    /// <summary>
    /// سبد مهمان اثبات‌شده را پس از ورود با سبد احرازشده ادغام می‌کند.
    /// </summary>
    Task<CartMergeResult> MergeAnonymousAfterLoginAsync(
        Guid userId,
        Guid? anonymousCartId,
        string? guestSecret,
        CancellationToken cancellationToken);

    /// <summary>
    /// سبد Active احرازشدهٔ مشتری را برمی‌گرداند؛ سبد خالی سایه‌ای مهمان نمی‌سازد.
    /// </summary>
    Task<CartSnapshot?> FindActiveAuthenticatedAsync(Guid userId, CancellationToken cancellationToken);
}

/// <summary>
/// نتیجهٔ ادغام ورود. خط ناموجود حذف خاموش نمی‌شود.
/// </summary>
public sealed record CartMergeResult(
    CartSnapshot Cart,
    bool AdoptedAnonymousCart,
    IReadOnlyList<CartLineSnapshot> Lines);
