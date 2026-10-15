using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Domain.Aggregates;

namespace Tooba.Shipping.Domain.Packaging;

/// <summary>
/// حل‌کنندهٔ قطعی و فقط-محاسباتی هزینهٔ بسته‌بندی. هیچ ورودی/خروجی، هیچ DbContext و هیچ زمان
/// جاری‌ای نمی‌خواند تا نتیجه فقط از دادهٔ داده‌شده مشتق شود و قابل بازتولید باشد.
/// </summary>
/// <remarks>
/// <para>
/// <b>ترتیب تقدم (مستند و قطعی).</b> همان ترتیب سیاست نرخ موجود، چون هر دو روی یک هویت
/// (خدمت/گزینه/دامنه) تصمیم می‌گیرند:
/// </para>
/// <list type="number">
/// <item><b>هویت گزینه:</b> سیاست گزینه‌دار پیش از سیاست سطح‌خدمت. اگر هیچ سیاست گزینه‌داری برای
/// گزینهٔ درخواست وجود نداشت، سیاست سطح‌خدمت به‌عنوان fallback صریح به کار می‌آید.</item>
/// <item><b>عمق دامنه:</b> در میان سیاست‌های هم‌هویت، عمیق‌ترین دامنه (ناحیهٔ خدمت ← شهر ← ناحیهٔ
/// اداری ← کشور) برنده است، پس هزینهٔ شهری هزینهٔ استانی را کنار می‌زند.</item>
/// </list>
/// <para>
/// <b>ابهام حل نمی‌شود.</b> اگر بیش از یک سیاست هم‌سطح و هم‌عمق برای درخواست تطبیق یابد، نتیجه
/// <see cref="ShippingPackagingReasonCodes.Ambiguous"/> است و هیچ هزینه‌ای اعلام نمی‌شود؛ «اولین
/// سطر برنده» یک تصمیم تجاری پنهان است.
/// </para>
/// <para>
/// <b>ارسال رایگان، بسته‌بندی را صفر نمی‌کند.</b> آستانهٔ ارسال رایگان یک قاعدهٔ <em>حمل</em> است.
/// بسته‌بندی مستقل است و تا وقتی یک قاعدهٔ جداگانهٔ بسته‌بندی آن را صفر نکند، حتی با حمل رایگان
/// باقی می‌ماند. این تصمیم عمدی و مستند است، نه یک اثر جانبی.
/// </para>
/// <para>
/// <b>پیش‌فرض صفر.</b> نبود سیاست یعنی هزینهٔ صفر. هیچ مسیری در این resolver به یک هزینهٔ پنهان
/// نمی‌رسد.
/// </para>
/// </remarks>
public static class ShippingPackagingFeeResolver
{
    /// <summary>مؤلفهٔ هزینهٔ بسته‌بندی یک درخواست را قطعی حل می‌کند.</summary>
    /// <param name="request">درخواست بسته‌بندی.</param>
    /// <param name="policies">سیاست‌های بسته‌بندی؛ ممکن است خالی باشد (که یعنی هزینهٔ صفر).</param>
    /// <returns>مؤلفهٔ هزینه؛ هرگز null.</returns>
    public static ShippingPackagingFeeComponent Resolve(
        ShippingPackagingFeeRequest request,
        IReadOnlyCollection<ShippingPackagingPolicy>? policies)
    {
        ArgumentNullException.ThrowIfNull(request);

        var serviceCode = request.ServiceCode?.Trim() ?? string.Empty;
        var optionCode = request.OptionCode?.Trim();
        var destination = new ShippingDestinationScope(
            request.Destination.CountryId,
            request.Destination.AdministrativeAreaId,
            request.Destination.CityId,
            request.Destination.ServiceAreaId);

        var covering = (policies ?? Array.Empty<ShippingPackagingPolicy>())
            .Where(x => x.IsActive)
            .Where(x => string.Equals(x.ServiceCode, serviceCode, StringComparison.OrdinalIgnoreCase))
            .Where(x => x.Covers(destination))
            .Where(x => x.MatchesOption(optionCode))
            .ToArray();

        if (covering.Length == 0)
        {
            // No configured policy is the documented zero default: packaging never becomes a hidden
            // extra charge just because a shipment exists.
            return ShippingPackagingFeeComponent.Zero(ShippingPackagingReasonCodes.NotConfigured);
        }

        // Option identity is resolved first, exactly like the rate engine: the request's own
        // option-specific policy when one exists, otherwise the documented service-level fallback.
        var candidates = covering.Any(x => x.IsOptionSpecific)
            ? covering.Where(x => x.IsOptionSpecific).ToArray()
            : covering;

        var deepest = candidates.Max(x => x.Scope.Level);
        var group = candidates.Where(x => x.Scope.Level == deepest).ToArray();

        if (group.Length > 1)
        {
            // Two equally specific policies for the same identity and destination. Even when they carry
            // the same fee, the pair is a data conflict that only a human may resolve.
            return ShippingPackagingFeeComponent.Zero(ShippingPackagingReasonCodes.Ambiguous);
        }

        var winner = group[0];
        return new ShippingPackagingFeeComponent(
            true,
            winner.FeeAmount,
            winner.CurrencyCode,
            winner.FreightPayerMode,
            ShippingPackagingChargePayer.Checkout,
            winner.PackagingPolicyId,
            ShippingPackagingReasonCodes.Charged);
    }
}
