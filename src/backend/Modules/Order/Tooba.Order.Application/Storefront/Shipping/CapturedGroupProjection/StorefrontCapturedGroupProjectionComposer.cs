using Tooba.BuildingBlocks;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Shipping.Contracts.Rates;

namespace Tooba.Order.Application.Storefront.Shipping.CapturedGroupProjection;

/// <summary>
/// تنها نقطهٔ ترکیب سه منبع پذیرفته‌شده برای یک گروه ارسال: فکت واقعی مبلغ کالای همان گروه پس از
/// تخفیف و مالیات (منبع Order)، قیمت یک بازبینی capture‌شدهٔ دقیق (Contracts مالک Shipping) و تقویم
/// دریافت مشتری از هفتهٔ ذخیره‌شده همان روش.
/// </summary>
/// <remarks>
/// <para>
/// <b>Compose, don't recalculate.</b> نه مالیات/تخفیف دوباره حساب می‌شود، نه مبلغ از ورودی فراخوان،
/// نه تقویم از پنجرهٔ ثابت یا ساعت مرورگر. مبلغ گروه یک‌بار از منبع Order و قیمت هر نامزد یک‌بار از
/// مالک Shipping خوانده می‌شود.
/// </para>
/// <para>
/// <b>One core.</b> تمام اعتبارسنجی/ترکیب/تقویم در <see cref="StorefrontCapturedGroupCompositionCore"/>
/// است؛ این کلاس فقط فکت‌ها را می‌خواند و به همان هسته می‌دهد. مسیر فکت‌های آمادهٔ submit هم همان
/// هسته را مستقیم صدا می‌زند، بدون چرخهٔ سازنده با <c>CheckoutDirectory</c>.
/// </para>
/// <para>
/// <b>No activation, no owner decision.</b> این سرویس هیچ بازبینی منتشرشده/فعال را انتخاب نمی‌کند و
/// هیچ مالک fulfillment را از فروشنده استنتاج نمی‌کند.
/// </para>
/// </remarks>
public sealed class StorefrontCapturedGroupProjectionComposer(
    ICheckoutShippingGroupGoodsFactsReader goodsFactsReader,
    IShippingCapturedRevisionQuoteLookup capturedQuoteLookup,
    IClock clock)
{
    private readonly StorefrontCapturedGroupCompositionCore _core =
        new(capturedQuoteLookup, clock);

    /// <summary>
    /// برنامهٔ گروه‌ها و نامزدها را اعتبارسنجی می‌کند، فکت‌های مالی را یک‌بار برای کل سبد می‌خواند و
    /// برای هر نامزد یک قیمت دقیق و یک تقویم دریافت می‌سازد.
    /// </summary>
    /// <param name="request">درخواست با فرمان checkout و برنامهٔ گروه/نامزد.</param>
    /// <param name="cancellationToken">توکن لغو؛ بدون تغییر به درزهای بیرونی می‌رسد.</param>
    public async Task<StorefrontCapturedGroupProjectionResult> ComposeAsync(
        StorefrontCapturedGroupProjectionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var command = request.Command ?? throw new ContractOperationException(
            StorefrontCapturedGroupProjectionReasons.PlanInvalid);
        if (request.Groups is null || request.Groups.Count == 0)
        {
            throw new ContractOperationException(StorefrontCapturedGroupProjectionReasons.PlanInvalid);
        }

        // برنامه پیش از هر خواندن سبد/فکت رد می‌شود؛ هیچ برنامهٔ ناسازگاری خواندن ایجاد نمی‌کند.
        _core.EnsurePlanIsValid(request.Groups);

        // یک‌بار برای کل سبد، با همان دسترسی/نسخهٔ مورداعتماد؛ هرگز پیش‌نمایش جدا برای هر گروه.
        var facts = await goodsFactsReader.ReadAsync(
            new CheckoutShippingGroupGoodsFactsRequest(
                command,
                request.Groups
                    .Select(group => new CheckoutShippingGroupMembership(group.GroupKey, group.CartLineIds))
                    .ToArray()),
            cancellationToken);

        return await _core.ComposeFromFactsAsync(command, request.Groups, facts, cancellationToken);
    }

    /// <summary>
    /// ترکیب از <b>فکت‌های آمادهٔ</b> همین Order (TB-233-R1): همان هستهٔ بدون خوانندهٔ فکت، همان یک
    /// خواندن ساعت سرور، همان یک نقل‌قول ۲۱۶ و همان تقویم ۲۱۳ برای هر نامزد.
    /// </summary>
    /// <remarks>
    /// این مسیر عمداً <b>هیچ</b> فراخوانی به <see cref="ICheckoutShippingGroupGoodsFactsReader"/>،
    /// Preview/Submit یا Pricing/Promotion/Tax ندارد: فکت‌ها از خطوط <c>SellerOrder</c> همان submit
    /// ساخته شده‌اند و فقط اعتبارسنجی/ترکیب می‌شوند. ورودی فقط داخلی و متعلق به Order است.
    /// </remarks>
    /// <param name="request">درخواست با فرمان checkout و برنامهٔ گروه/نامزد.</param>
    /// <param name="facts">فکت‌های ساخته‌شده از خطوط سفارش همان submit.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    public Task<StorefrontCapturedGroupProjectionResult> ComposeFromFactsAsync(
        StorefrontCapturedGroupProjectionRequest request,
        CheckoutShippingGroupGoodsFactsResult facts,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _core.ComposeFromFactsAsync(
            request.Command ?? throw new ContractOperationException(
                StorefrontCapturedGroupProjectionReasons.PlanInvalid),
            request.Groups,
            facts,
            cancellationToken);
    }
}
