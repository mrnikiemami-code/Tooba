using Tooba.Cart.Contracts;
using Tooba.Order.Domain;

namespace Tooba.Order.Application.Storefront.Ports;

/// <summary>Order-owned shipping draft persistence.</summary>
public interface IStorefrontShippingDraftStore
{
    Task<CartShippingDraft?> GetByCartIdAsync(Guid cartId, CancellationToken cancellationToken);
    Task SaveAsync(CartShippingDraft draft, bool isNew, CancellationToken cancellationToken);

    /// <summary>
    /// TB-232: جایگزینی <b>اتمیک</b> انتخاب capture‌شده با CAS نسخهٔ انتخاب.
    /// </summary>
    /// <remarks>
    /// حذف و درج گروه‌ها و به‌روزرسانی ریشه در <b>یک</b> تراکنش انجام می‌شود تا حذف جزئی هرگز durable
    /// نشود. نسخهٔ انتخاب باید دقیقاً با نسخهٔ ذخیره‌شده برابر باشد (توکن هم‌زمانی واقعی EF)؛ در غیر
    /// این صورت یا در صورت برخورد هم‌زمانی/یکتایی، استثنا با کد <see cref="StorefrontShippingSelectionConflict"/>
    /// پرتاب می‌شود و <b>هیچ</b> بخشی از انتخاب جدید باقی نمی‌ماند.
    /// </remarks>
    /// <param name="draft">ریشهٔ آماده‌شده با فیلدهای اسکالر همین درخواست.</param>
    /// <param name="groups">گروه‌های ساخته‌شده توسط برنامه‌ریز سرور.</param>
    /// <param name="isNew">آیا این پیش‌نویس در این درخواست ساخته می‌شود.</param>
    /// <param name="expectedSelectionRevision">نسخهٔ موردانتظار (۰ برای نبود/ردیف قدیمی).</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task ReplaceCapturedSelectionAsync(
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
