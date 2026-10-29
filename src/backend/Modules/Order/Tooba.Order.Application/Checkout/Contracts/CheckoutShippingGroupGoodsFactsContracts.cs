namespace Tooba.Order.Application.Checkout.Contracts;

/// <summary>
/// عضویت سرورساختهٔ یک گروه ارسال: کلید پایدار گروه و <b>همان</b> شناسه‌های خط سبدی که این گروه
/// ادعا می‌کند. این ادعا اعتبارسنجی می‌شود و هیچ مالکیت/قیمتی از مرورگر پذیرفته نمی‌شود.
/// </summary>
/// <param name="GroupKey">کلید پایدار گروه در همان درخواست (تهی/تکراری مجاز نیست).</param>
/// <param name="CartLineIds">شناسهٔ خطوط سبد همین گروه؛ باید افراز کامل سبد فعال باشد.</param>
public sealed record CheckoutShippingGroupMembership(
    string GroupKey,
    IReadOnlyList<Guid> CartLineIds);

/// <summary>
/// درخواست فکت‌های مالی گروه ارسال برای Shipping: همان فرمان/دسترسی/نسخهٔ سبد معمول به‌همراه
/// عضویت صریح گروه‌ها.
/// </summary>
/// <remarks>
/// <para>
/// <b>Not a price request.</b> مبلغ‌ها از پیش‌نمایش authoritative همان checkout می‌آیند، نه از این
/// درخواست؛ هیچ مبلغ/تخفیف/مالیاتی از فراخوان پذیرفته نمی‌شود. <c>ExpectedCartVersion</c> تنها
/// واقعیتی است که این درز از فرمان می‌خواند و باید با نسخهٔ واقعی سبد یکی باشد.
/// </para>
/// </remarks>
/// <param name="Command">فرمان checkout با دسترسی/نسخه/ارز خودش.</param>
/// <param name="Groups">عضویت گروه‌ها؛ باید افراز کامل خطوط سبد فعال باشد.</param>
public sealed record CheckoutShippingGroupGoodsFactsRequest(
    SubmitCheckoutCommand Command,
    IReadOnlyList<CheckoutShippingGroupMembership> Groups);

/// <summary>
/// سهم یک خط سبد در مبلغ گروه؛ همهٔ مبالغ از واقعیت‌های ذخیره‌شده/محاسبه‌شدهٔ همان پیش‌نمایش‌اند.
/// </summary>
/// <param name="CartLineId">هویت خط سبد (مبدأ عضویت).</param>
/// <param name="OrderLineId">هویت خط سفارش همان تبدیل؛ تطبیق دقیق، نه حدس.</param>
/// <param name="OfferId">آفر همان خط.</param>
/// <param name="Quantity">مقدار همان خط (اعشاری؛ تبدیل خودکار به شمارش اقلام ممنوع).</param>
/// <param name="UnitPrice">قیمت واحد snapshot.</param>
/// <param name="PreDiscountTaxExclusiveAmount">مبلغ قبل از تخفیف و بدون مالیات.</param>
/// <param name="DiscountAmount">تخفیف واقعی همان خط.</param>
/// <param name="PostDiscountTaxExclusiveAmount">مبلغ پس از تخفیف و بدون مالیات.</param>
/// <param name="TaxAmount">مالیات واقعی همان خط (پس از تخفیف).</param>
/// <param name="TaxInclusiveAmount">مبلغ نهایی همان خط با مالیات؛ همان مقدار authoritative گروه.</param>
/// <param name="Currency">ارز همان خط.</param>
public sealed record CheckoutShippingGroupGoodsContribution(
    Guid CartLineId,
    Guid OrderLineId,
    Guid OfferId,
    decimal Quantity,
    decimal UnitPrice,
    decimal PreDiscountTaxExclusiveAmount,
    decimal DiscountAmount,
    decimal PostDiscountTaxExclusiveAmount,
    decimal TaxAmount,
    decimal TaxInclusiveAmount,
    string Currency);

/// <summary>
/// فکت‌های مالی یک گروه ارسال: جمع مبلغ نهایی کالاهای <b>همان گروه</b> پس از تخفیف و پس از مالیات.
/// </summary>
/// <remarks>
/// <para>
/// <b>No re-derivation.</b> <see cref="FinalGoodsAmountAfterDiscountAndTax"/> جمع
/// <see cref="CheckoutShippingGroupGoodsContribution.TaxInclusiveAmount"/> همان اعضاست؛ نه مالیات
/// دوباره اضافه می‌شود، نه تخفیف دوباره کم، و نه سهمی از جمع کل سبد به نسبت تقسیم می‌شود.
/// </para>
/// <para>
/// <b>Physical facts stay optional.</b> <see cref="GroupWeightGrams"/> و
/// <see cref="PhysicalItemCount"/> فقط وقتی پر می‌شوند که منبع authoritative اندازه‌گیری موجود باشد؛
/// در غیر این صورت <c>null</c> می‌مانند. <see cref="QuantityTotal"/> فقط متادیتای مقدار است و هرگز
/// به‌طور خودکار «تعداد اقلام» Shipping نمی‌شود (مقدار اعشاری گرد/سقف نمی‌شود).
/// </para>
/// </remarks>
/// <param name="GroupKey">کلید گروه همان درخواست.</param>
/// <param name="SellerPartyId">فروشندهٔ واقعی همان گروه.</param>
/// <param name="Currency">ارز همان گروه؛ نبود/ناسازگاری رد می‌شود.</param>
/// <param name="FinalGoodsAmountAfterDiscountAndTax">جمع مبلغ نهایی اعضای همین گروه.</param>
/// <param name="QuantityTotal">جمع مقدار خطوط همین گروه (متادیتا، اعشاری).</param>
/// <param name="GroupWeightGrams">وزن اندازه‌گیری‌شدهٔ گروه یا تهی وقتی منبع authoritative نیست.</param>
/// <param name="PhysicalItemCount">تعداد فیزیکی اقلام یا تهی وقتی منبع authoritative نیست.</param>
/// <param name="Contributions">سهم دقیق هر خط سبد در همین گروه.</param>
public sealed record CheckoutShippingGroupGoodsFacts(
    string GroupKey,
    Guid SellerPartyId,
    string Currency,
    decimal FinalGoodsAmountAfterDiscountAndTax,
    decimal QuantityTotal,
    long? GroupWeightGrams,
    decimal? PhysicalItemCount,
    IReadOnlyList<CheckoutShippingGroupGoodsContribution> Contributions)
{
    /// <summary>شناسهٔ خطوط سبد همین گروه، به همان ترتیب سهم‌ها.</summary>
    public IReadOnlyList<Guid> CartLineIds =>
        Contributions.Select(contribution => contribution.CartLineId).ToArray();
}

/// <summary>
/// نتیجهٔ فکت‌های گروهی یک سبد: هویت سبد/نسخه/ارز و گروه‌های اعتبارسنجی‌شده.
/// </summary>
/// <param name="CartId">سبد authoritative همان درخواست.</param>
/// <param name="CartVersion">نسخهٔ واقعی سبد که با نسخهٔ درخواستی سنجیده شد.</param>
/// <param name="Currency">ارز یگانهٔ سبد.</param>
/// <param name="Groups">گروه‌ها به همان ترتیب درخواست.</param>
public sealed record CheckoutShippingGroupGoodsFactsResult(
    Guid CartId,
    int CartVersion,
    string Currency,
    IReadOnlyList<CheckoutShippingGroupGoodsFacts> Groups);

/// <summary>
/// درز فقط-خواندنی Order برای فکت‌های مالی گروه ارسال، تا Shipping مبلغ «همین گروه پس از تخفیف و
/// مالیات» را از منبع authoritative بخواند و خودش آن را بازمحاسبه نکند.
/// </summary>
/// <remarks>
/// <para>
/// <b>Read-only.</b> هیچ Checkout/Order ردیفی نوشته نمی‌شود، هیچ رزروی گرفته نمی‌شود، سبد تبدیل
/// نمی‌شود و پرداخت آغاز نمی‌شود؛ همین پیش‌نمایش بدون ماندگاری کافی است.
/// </para>
/// <para>
/// <b>Not the fulfillment owner.</b> این درز نه نسخهٔ منتشرشدهٔ ارسال را انتخاب می‌کند و نه مالک
/// fulfillment را تعیین می‌کند؛ فقط واقعیت مالی گروه را می‌دهد. فکت‌های فیزیکی تا وجود منبع
/// authoritative صادقانه ناشناخته می‌مانند.
/// </para>
/// </remarks>
public interface ICheckoutShippingGroupGoodsFactsReader
{
    /// <summary>
    /// افراز کامل سبد را اعتبارسنجی می‌کند و برای هر گروه، جمع مبلغ نهایی کالاهای همان گروه را از
    /// پیش‌نمایش authoritative برمی‌گرداند.
    /// </summary>
    /// <param name="request">درخواست با فرمان checkout و عضویت صریح گروه‌ها.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<CheckoutShippingGroupGoodsFactsResult> ReadAsync(
        CheckoutShippingGroupGoodsFactsRequest request,
        CancellationToken cancellationToken);
}
