using Tooba.Cart.Contracts;
using Tooba.Order.Domain;

namespace Tooba.Order.Application.Storefront.Ports;

/// <summary>Order-owned shipping draft persistence.</summary>
public interface IStorefrontShippingDraftStore
{
    Task<CartShippingDraft?> GetByCartIdAsync(Guid cartId, CancellationToken cancellationToken);
    Task SaveAsync(CartShippingDraft draft, bool isNew, CancellationToken cancellationToken);

    /// <summary>
    /// TB-232-R1: خواندن <b>تازه و جدا</b> از ردیف ذخیره‌شده برای نوشتن انتخاب capture‌شده.
    /// </summary>
    /// <remarks>
    /// نتیجه از <b>حقیقت ذخیره‌شده</b> ساخته می‌شود (نه از identity map که ممکن است کهنه باشد) و
    /// detached است، پس نویسنده می‌تواند بدون لمس EF آن را آماده کند. اگر در همان context کار
    /// افزودن/تغییر/حذفِ نامرتبطِ در انتظار وجود داشته باشد، نوشتن <b>پیش از هر تغییر/تراکنش</b> با کد
    /// <see cref="StorefrontCapturedSelectionReasons.PendingWork"/> رد می‌شود و آن کار دست‌نخورده می‌ماند.
    /// </remarks>
    Task<CartShippingDraft?> GetForCapturedSelectionAsync(Guid cartId, CancellationToken cancellationToken);

    /// <summary>
    /// TB-232: جایگزینی <b>اتمیک</b> انتخاب capture‌شده با CAS نسخهٔ انتخاب.
    /// </summary>
    /// <remarks>
    /// حذف و درج گروه‌ها و به‌روزرسانی ریشه در <b>یک</b> تراکنش انجام می‌شود تا حذف جزئی هرگز durable
    /// نشود. نسخهٔ انتخاب باید دقیقاً با نسخهٔ <b>ذخیره‌شده</b> برابر باشد (توکن هم‌زمانی واقعی EF)؛ در
    /// غیر این صورت یا در صورت برخورد هم‌زمانی/یکتایی، استثنا با کد
    /// <see cref="StorefrontShippingSelectionConflict"/> پرتاب می‌شود و <b>هیچ</b> بخشی از انتخاب جدید
    /// باقی نمی‌ماند.
    /// <para>
    /// TB-232-R1: <paramref name="draft"/> فقط یک <b>قصد جدا</b> است؛ مقادیر آن روی ریشهٔ تازه‌خوانده‌شدهٔ
    /// tracked اعمال می‌شود. مقادیر اصلیِ پیش از تلاش snapshot می‌شوند، همهٔ مراحل (شامل شروع تراکنش)
    /// داخل همان try هستند و بازگردانی فقط همین تلاش را برمی‌گرداند تا SaveChanges بعدی چیزی از انتخاب
    /// ردشده را جاودانه نکند.
    /// </para>
    /// </remarks>
    /// <param name="draft">ریشهٔ آماده‌شده (detached) با فیلدهای اسکالر همین درخواست.</param>
    /// <param name="groups">گروه‌های ساخته‌شده توسط برنامه‌ریز سرور.</param>
    /// <param name="isNew">آیا این پیش‌نویس در این درخواست ساخته می‌شود.</param>
    /// <param name="expectedSelectionRevision">نسخهٔ موردانتظار (۰ برای نبود/ردیف قدیمی).</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<CartShippingDraft> ReplaceCapturedSelectionAsync(
        CartShippingDraft draft,
        IReadOnlyList<CartShippingDraftGroup> groups,
        bool isNew,
        long expectedSelectionRevision,
        CancellationToken cancellationToken);
}

/// <summary>کد پایدار برخورد/ناسازگاری انتخاب capture‌شده در ذخیره‌سازی (TB-232).</summary>
public static class StorefrontShippingSelectionConflict
{
    /// <summary>نسخهٔ انتخاب ذخیره‌شده با نسخهٔ موردانتظار نمی‌خواند (یا هم‌زمانی).</summary>
    public const string Stale = "storefront.captured_selection.stale";
}

/// <summary>Order-owned pending-payment checkout reads/writes.</summary>
public interface IStorefrontPendingCheckoutStore
{
    Task<IReadOnlyList<CheckoutGroup>> ListOwnedByUserAsync(Guid userId, int take, CancellationToken cancellationToken);
    Task<IReadOnlyList<CheckoutGroup>> ListByCheckoutIdsAsync(IReadOnlyList<Guid> checkoutIds, CancellationToken cancellationToken);
    Task<CheckoutGroup?> GetWithSellerOrdersAsync(Guid checkoutId, CancellationToken cancellationToken);
    Task HidePendingCardAsync(Guid checkoutId, Guid? ownerUserId, Guid? guestCartId, DateTimeOffset now, CancellationToken cancellationToken);
    Task<HashSet<Guid>> LoadHiddenCheckoutIdsAsync(
        IReadOnlyList<Guid> checkoutIds,
        Guid? ownerUserId,
        IReadOnlyList<Guid> guestCartIds,
        CancellationToken cancellationToken);
}
