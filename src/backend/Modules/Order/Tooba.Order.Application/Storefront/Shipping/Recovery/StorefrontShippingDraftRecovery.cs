using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Domain.Checkout;

namespace Tooba.Order.Application.Storefront.Shipping.Recovery;

/// <summary>
/// TB-SHIPPING-ONE-LEVEL-TABS-134 — بازیابی فقط-خواندنی انتخاب ارسالِ ذخیره‌شده روی مرحلهٔ ارسال.
///
/// چرا لازم است: پیش‌نویس پایدار چندفروشنده‌ای، shorthand قدیمی <see cref="CartShippingDraft.ShippingMethodCode"/>
/// را <b>خالی</b> نگه می‌دارد و انتخاب واقعی فقط در گروه‌های پایدار (<c>cart_shipping_groups</c>) است.
/// پس اعتبارسنجی گذشته که تنها با همان shorthand کار می‌کرد، در رفرش سرد، پیش‌نویس معتبر را دور می‌ریخت.
///
/// این کلاس فقط <b>تصمیم</b> می‌گیرد: هیچ ذخیره‌سازی، هیچ قیمت‌گذاری و هیچ نوشتنی انجام نمی‌دهد. کد روش،
/// برچسب، مبلغ، ارز و روزهای آماده‌سازی همچنان فقط از فهرست واجدین شرایط authoritative خوانده می‌شود.
/// </summary>
public static class StorefrontShippingDraftRecovery
{
    /// <summary>پیام بازآزمایی موجود و مورد استفادهٔ مشتری؛ متن آن تغییر نمی‌کند.</summary>
    public const string RevalidationMessage = "انتخاب ارسال دیگر معتبر نیست؛ لطفاً دوباره انتخاب کنید.";

    /// <summary>
    /// کدهای روشِ انتخاب‌شدهٔ پیش‌نویس: گروه‌های پایدار اگر وجود دارند، وگرنه shorthand تک‌روشی قدیمی.
    /// کدهای خالی حذف و تکراری‌ها یکی می‌شوند تا سبد چندفروشنده‌ای درست همان مجموعهٔ ذخیره‌شده را بازآزمایی کند.
    /// </summary>
    public static string[] SelectedMethodCodes(CartShippingDraft draft)
    {
        var codes = draft.ShippingGroups.Count > 0
            ? draft.ShippingGroups.Select(x => x.MethodCode)
            : [draft.ShippingMethodCode];

        return codes
            .Select(code => (code ?? string.Empty).Trim())
            .Where(code => code.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// آیا انتخاب ذخیره‌شده هنوز قابل بازیابی است؟ (فقط‌خواندنی، fail-closed)
    /// شرط‌ها: هر روش انتخاب‌شده امروز هم واجد شرایط باشد؛ مقصد در صورت ارسال با استان پیش‌نویس سازگار باشد؛
    /// و گروه‌های ذخیره‌شده هنوز به فروشنده‌های همان سبد تعلق داشته باشند (سبد تغییر نکرده باشد).
    /// </summary>
    public static bool IsStillValid(
        CartShippingDraft draft,
        IReadOnlyList<StorefrontShippingMethodView> methods,
        string? provinceName,
        IReadOnlyCollection<Guid> cartSellerPartyIds,
        ShippingMethodsOptions shippingOptions)
    {
        var codes = SelectedMethodCodes(draft);
        if (codes.Length == 0)
        {
            return false;
        }

        foreach (var code in codes)
        {
            var eligible = methods.FirstOrDefault(m =>
                string.Equals(m.MethodCode, code, StringComparison.OrdinalIgnoreCase));
            if (eligible is null)
            {
                return false;
            }

            // استان ارسالی فقط وقتی سخت‌گیرانه است که با استان ثبت‌شده فرق کند؛ در آن حالت همان درز
            // پوشش canonical تصمیم می‌گیرد که آیا انتخاب ذخیره‌شده برای مقصد جدید هم معتبر است یا نه.
            if (!string.IsNullOrWhiteSpace(provinceName)
                && !string.Equals(provinceName, draft.ProvinceName, StringComparison.OrdinalIgnoreCase)
                && !StorefrontShippingCalculator.IsDestinationAllowed(
                    StorefrontShippingCalculator.ResolveRate(code, shippingOptions),
                    draft.ProvinceName))
            {
                return false;
            }
        }

        if (draft.ShippingGroups.Count > 0
            && !draft.ShippingGroups.All(g => cartSellerPartyIds.Contains(g.SellerPartyId)))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// دیرترین روز آماده‌سازی روش‌های گروه‌های ذخیره‌شده بر پایهٔ فهرست <b>امروزِ</b> واجدین شرایط؛
    /// اگر هیچ گروهی نباشد یا روشی امروز واجد شرایط نباشد <c>null</c> برمی‌گردد (تاریخ ساختگی ساخته نمی‌شود).
    /// این مقدار همان «حداقل روز تحویل» محافظه‌کارانهٔ سبد چندگروهی است که سرور با قاعدهٔ canonical می‌سازد.
    /// </summary>
    public static int? ResolveProjectedLeadDays(
        CartShippingDraft draft,
        IReadOnlyList<StorefrontShippingMethodView> methods)
    {
        if (draft.ShippingGroups.Count == 0)
        {
            return null;
        }

        var leadDays = new List<int>(draft.ShippingGroups.Count);
        foreach (var code in SelectedMethodCodes(draft))
        {
            var eligible = methods.FirstOrDefault(m =>
                string.Equals(m.MethodCode, code, StringComparison.OrdinalIgnoreCase));
            if (eligible is null)
            {
                return null;
            }

            leadDays.Add(eligible.LeadDays);
        }

        return leadDays.Count == 0 ? null : leadDays.Max();
    }
}
