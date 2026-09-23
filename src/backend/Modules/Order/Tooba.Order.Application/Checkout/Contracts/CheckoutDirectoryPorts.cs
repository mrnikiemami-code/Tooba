using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;

namespace Tooba.Order.Application.Checkout.Contracts;

/// <summary>
/// هویت مجاز برای خواندن سفارش. شمارهٔ سفارش به‌تنهایی Bearer نیست.
/// </summary>
public sealed record OrderAccess(Guid? BuyerPartyId, Guid? PlacedByUserId);

/// <summary>
/// مهلت رزرو اولیه پس از commit سفارش / شروع پرداخت. مدت جادویی در CheckoutDirectory نیست.
/// </summary>
public interface ICheckoutReservationHoldPolicy
{
    /// <summary>ExpiresAt رزرو اولیهٔ سفارش را از Settings برمی‌گرداند.</summary>
    DateTimeOffset ResolveInitialExpiresAt(DateTimeOffset utcNow);
}

/// <summary>
/// درز نگهبان مجوز Order. ماتریس نهایی فروشنده اینجا نیست.
/// </summary>
public interface IOrderUseCaseGuard
{
    /// <summary>
    /// اجازهٔ نوشتن checkout را بررسی می‌کند. پیاده‌سازی فعلی فقط درز است.
    /// </summary>
    Task EnsureCanMutateAsync(CancellationToken cancellationToken);
}

/// <summary>
/// ارکستراسیون checkout روی قراردادهای Cart/Offer/Pricing/Inventory. DbContext آن‌ها لمس نمی‌شود.
/// </summary>
public interface ICheckoutDirectory
{
    /// <summary>
    /// سبد را به گروه checkout و سفارش‌های فروشنده تبدیل می‌کند. تکرار با همان کلید سفارش تکراری نمی‌سازد.
    /// </summary>
    Task<CheckoutSnapshot> SubmitAsync(SubmitCheckoutCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// همان ارزیابی تجاری Submit را بدون ماندگاری CheckoutGroup برمی‌گرداند تا ویترین مبلغ را از React حساب نکند.
    /// </summary>
    Task<CheckoutSnapshot> PreviewAsync(SubmitCheckoutCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// checkout را پس از احراز هویت خریدار یا کاربر عامل برمی‌گرداند.
    /// </summary>
    Task<CheckoutSnapshot?> GetCheckoutAsync(Guid checkoutId, OrderAccess access, CancellationToken cancellationToken);

    /// <summary>
    /// سفارش فروشنده را با شمارهٔ مرجع پس از احراز هویت برمی‌گرداند. شماره به‌تنهایی کافی نیست.
    /// </summary>
    Task<SellerOrderSnapshot?> GetSellerOrderByNumberAsync(string orderNumber, OrderAccess access, CancellationToken cancellationToken);

    /// <summary>
    /// سفارش فروشنده را در صورت ایمن بودن لغو می‌کند و رزرو را از قرارداد Inventory آزاد می‌کند.
    /// </summary>
    Task CancelSellerOrderAsync(Guid sellerOrderId, OrderAccess access, CancellationToken cancellationToken);

    /// <summary>
    /// سفارش‌های لغوشدهٔ یک checkout را به وضعیت قبلی برمی‌گرداند و رزرو موجودی را دوباره می‌گیرد.
    /// </summary>
    Task RestoreCancelledCheckoutAsync(Guid checkoutId, OrderAccess access, CancellationToken cancellationToken);

    /// <summary>
    /// یادداشت‌های عملیاتی داخلی checkout را از جدید به قدیم برمی‌گرداند (محدود؛ بدون حذف‌شده‌ها).
    /// </summary>
    Task<IReadOnlyList<CheckoutOperationalNoteSnapshot>> ListNotesAsync(
        Guid checkoutId,
        Guid viewerUserId,
        int take,
        CancellationToken cancellationToken);

    /// <summary>
    /// یادداشت عملیاتی داخلی را ثبت می‌کند.
    /// </summary>
    Task<CheckoutOperationalNoteSnapshot> AddNoteAsync(
        Guid checkoutId,
        Guid actorUserId,
        string body,
        CancellationToken cancellationToken);

    /// <summary>
    /// یادداشت را طبق قاعدهٔ نویسنده/قفل مشاهده soft-delete می‌کند.
    /// </summary>
    Task<CheckoutNoteDeleteOutcome> DeleteNoteAsync(
        Guid checkoutId,
        Guid noteId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// مشاهدهٔ Admin از جزئیات سفارش را برای قفل حذف یادداشت ثبت می‌کند.
    /// </summary>
    Task RecordAdminViewAsync(
        Guid checkoutId,
        Guid viewerUserId,
        CancellationToken cancellationToken);
}

/// <summary>snapshot خواندنی یادداشت عملیاتی داخلی checkout.</summary>
public sealed record CheckoutOperationalNoteSnapshot(
    Guid NoteId,
    Guid CheckoutId,
    string Body,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    bool CanDelete);

public enum CheckoutNoteDeleteOutcome
{
    Deleted,
    Forbidden,
    NotFound
}
