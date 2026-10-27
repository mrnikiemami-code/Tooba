using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Cart.Contracts;
using Tooba.Inventory.Contracts.Orders;
using Tooba.Inventory.Contracts.Seller;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.Storefront.Shipping.CapturedPreview;
using Tooba.Order.Domain;

namespace Tooba.Order.Infrastructure.Checkout.Persistence;

/// <summary>
/// پذیرش گروه‌های ارسال capture‌شده در <b>همان</b> مسیر واقعی submit (TB-233).
/// </summary>
/// <remarks>
/// <para>
/// این قطعه هیچ مسیر موازی نمی‌سازد: فقط از داخل <see cref="CheckoutProcessManager.SubmitAsync"/> و
/// <b>پیش از</b> نخستین رزرو/نوشتن سفارش صدا زده می‌شود.
/// </para>
/// <para>
/// <b>Fail-closed.</b> تا وقتی بازاعتبارسنجی کامل (مقصد/اشاره‌گر/نقل‌قول/تقویم/مبلغ نهایی) پیاده نشده است،
/// نبودِ کلید <c>CheckoutEnabled</c> و نیز روشن‌بودن آن هر دو مسیر capture را با کد پایدار
/// <see cref="StorefrontCapturedPreviewReasons.PreviewNotReady"/> می‌بندند: هیچ شاهد capture‌شده‌ای بدون
/// بازاعتبارسنجی به سفارش تبدیل نمی‌شود و هیچ پلن فراخوانی هم اعتبارسنجی مستقل شمرده نمی‌شود.
/// </para>
/// <para>
/// مسیر قدیمی (پیش‌نویس/فرمان بدون هیچ شاهد capture) عیناً همان پلن ورودی را می‌گیرد و هیچ خواندن/تغییر
/// اضافه‌ای نمی‌بیند.
/// </para>
/// </remarks>
public sealed partial class CheckoutDirectory
{
    /// <inheritdoc />
    async Task<IReadOnlyList<OrderShippingGroupPlan>> ICheckoutSubmitHost.AcceptCapturedShippingAsync(
        SubmitCheckoutCommand command,
        CartSnapshot cart,
        IReadOnlyList<SellerOrder> sellerOrders,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(cart);

        var plans = command.ShippingGroups ?? [];
        var callerSuppliedEvidence = plans.Any(plan => plan.AcceptedEvidence is not null);
        var checkoutEnabled = _capturedPreview?.Value.CheckoutEnabled ?? false;

        // مسیر قدیمی: نه شاهد فراخوان و نه کلید ثبت نهایی — دقیقاً همان پلن ورودی، بدون هیچ خواندن اضافه.
        if (!checkoutEnabled && !callerSuppliedEvidence)
        {
            return plans;
        }

        // شاهدِ ساختگی/فراخوان بدون کلید ثبت نهایی هرگز پذیرفته نمی‌شود و حتی به خواندن نمی‌رسد.
        if (!checkoutEnabled)
        {
            throw new ContractOperationException(StorefrontCapturedPreviewReasons.PreviewNotReady);
        }

        // شاهدِ ذخیره‌شده باید تازه و کامل خوانده شود؛ پلن فراخوان فقط ورودی مقایسه است، نه اقتدار.
        var stored = await ReadStoredCapturedDraftAsync(command.CartId, cancellationToken);
        if (stored is null && !callerSuppliedEvidence)
        {
            // کلید ثبت نهایی روشن ولی هیچ پیش‌نویس capture‌شده‌ای نیست: مسیر قدیمی دست‌نخورده.
            return plans;
        }

        // هر گذرِ capture‌شده (شاهد ذخیره‌شده یا شاهد فراخوان) تا تکمیل بازاعتبارسنجی بسته است.
        _ = sellerOrders;
        _ = now;
        throw new ContractOperationException(StorefrontCapturedPreviewReasons.PreviewNotReady);
    }

    /// <summary>
    /// خواندن <b>تازه و بدون ردیابی</b> ریشهٔ پیش‌نویس با شاهد و اعضای گروه‌ها؛ هرگز از گراف tracked
    /// شامل‌های قدیمی استفاده نمی‌شود.
    /// </summary>
    private async Task<CartShippingDraft?> ReadStoredCapturedDraftAsync(
        Guid cartId,
        CancellationToken cancellationToken)
    {
        if (cartId == Guid.Empty)
        {
            return null;
        }

        return await _db.ShippingDrafts
            .AsNoTracking()
            .Include(x => x.ShippingGroups)
            .ThenInclude(group => group.Lines)
            .Where(x => x.CartId == cartId)
            .Where(x => x.ShippingGroups.Any(group => group.AcceptedEvidence != null))
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// تفسیر دقیق متن نسخهٔ انتخاب: فقط رقم‌های AscIی نامنفی و بدون فاصله/علامت. نبود/تهی/بدشکل یعنی
    /// «قصد نامعتبر» و هرگز به معنای «۰» یا «هر نسخه‌ای» نیست. این تابع هیچ حالتی/نویسهٔ دیگری را نمی‌پذیرد.
    /// </summary>
    /// <param name="text">متن خام نسخهٔ انتخاب از درخواست.</param>
    /// <param name="revision">نسخهٔ تفسیرشده وقتی متن معتبر است.</param>
    public static bool TryParseSelectionRevision(string? text, out long revision)
    {
        revision = 0;
        if (string.IsNullOrEmpty(text) || text.Length != text.Trim().Length)
        {
            return false;
        }

        foreach (var character in text)
        {
            if (character is < '0' or > '9')
            {
                return false;
            }
        }

        return long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out revision)
            && revision >= 0;
    }
}
