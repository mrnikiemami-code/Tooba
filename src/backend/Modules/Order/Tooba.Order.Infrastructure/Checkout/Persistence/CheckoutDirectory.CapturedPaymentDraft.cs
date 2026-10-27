using Tooba.BuildingBlocks;
using Tooba.Cart.Contracts;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Domain;

namespace Tooba.Order.Infrastructure.Checkout.Persistence;

/// <summary>
/// خواندنِ <b>فقط-خواندنی</b> «پیش‌نویس پرداخت» برای یک انتخاب capture‌شده (TB-234).
/// </summary>
/// <remarks>
/// <para>
/// این قطعه هیچ مسیر موازی برای قیمت/تقویم نمی‌سازد: <b>یک</b> نقل‌قول کل سبد از همان هستهٔ پیش‌نمایش
/// (<see cref="CheckoutDirectory.ComposePreviewSnapshot"/>) گرفته می‌شود و همان نتیجهٔ
/// <see cref="ICheckoutSubmitHost.AcceptCapturedShippingAsync"/> که مسیر ثبت نهایی استفاده می‌کند
/// اعتبارسنجی را انجام می‌دهد. پس هیچ اعتبارسنجی دوم/مهندسی دوباره‌ای وجود ندارد.
/// </para>
/// <para>
/// <b>No writes.</b> نه سفارش، نه رزرو، نه تبدیل سبد، نه <c>SaveChanges</c> و نه مصرف سهمیهٔ abuse.
/// مقدار بازگشتی فقط نمای همین لحظه است؛ دسترس‌پذیری تاریخ به‌معنای CanCheckout یا فعال‌سازی نیست.
/// </para>
/// </remarks>
public sealed partial class CheckoutDirectory
{
    /// <inheritdoc />
    async Task<CapturedPaymentDraftSnapshot> ICheckoutDirectory.ProjectCapturedPaymentDraftAsync(
        SubmitCheckoutCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // همان مرز دسترسی/وضعیت/نسخهٔ مسیر پیش‌نمایش: هیچ نقل‌قولی پیش از آن انجام نمی‌شود.
        var cart = await LoadPreviewCartAsync(command, cancellationToken);
        if (cart.Version != command.ExpectedCartVersion)
        {
            throw new ContractOperationException("checkout.version.conflict");
        }

        var now = _clock.UtcNow;
        var checkoutId = UuidV7.New();
        var sellerOrders = await QuoteSellerOrdersAsync(
            cart,
            command,
            checkoutId,
            now,
            new Dictionary<Guid, Guid>(),
            requireReservation: false,
            cancellationToken);

        // همان پذیرش مسیر ثبت نهایی — فقط-خواندنی: شاهد ذخیره‌شده با سبد/نشانی/اشاره‌گر/نقل‌قول/تاریخ
        // همین لحظه سنجیده می‌شود و نتیجهٔ آن نمای پرداخت است. نسخهٔ صریح و قفل نقطه‌ای (مخصوص ثبت) اجرا
        // نمی‌شود تا این مسیر هیچ قفل/نوشتنی نداشته باشد.
        var groups = await AcceptCapturedShippingCoreAsync(
            command,
            cart,
            sellerOrders,
            now,
            requireExplicitSelectionRevision: false,
            cancellationToken);

        var quote = ComposePreviewSnapshot(cart, command, checkoutId, now, sellerOrders);
        return new CapturedPaymentDraftSnapshot(quote, groups);
    }
}
