namespace Tooba.Order.Application.Storefront.Models;

/// <summary>
/// تصویر ارسال فروشگاهی. SavedAddressId اختیاری است و روی سفارش ذخیره نمی‌شود؛
/// فقط برای تصویربرداری فیلدها از دفترچهٔ متعلق به Actor استفاده می‌گردد.
/// </summary>
public sealed record StorefrontCheckoutShippingInput(
    string RecipientName,
    string ContactMobile,
    string ProvinceName,
    string CityName,
    string PostalAddress,
    string PostalCode,
    Guid? SavedAddressId = null,
    string FirstName = "",
    string LastName = "");

/// <summary>
/// ورودی ارسال checkout از سبد زنده.
/// </summary>
public sealed record StorefrontSubmitCheckoutRequest(
    Guid CartId,
    int ExpectedCartVersion,
    string IdempotencyKey,
    StorefrontCheckoutShippingInput Shipping,
    string? CouponCode = null);

/// <summary>
/// خط بازبینی checkout. مبلغ از Order/Tax است نه جمع React.
/// </summary>
public sealed record StorefrontCheckoutLineView(
    Guid OfferId,
    Guid SellerPartyId,
    string Title,
    string SellerDisplayName,
    decimal Quantity,
    decimal LineExclusiveOfTax,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal LinePayable,
    string Currency);

/// <summary>
/// سفارش فروشنده داخل checkout. پرداخت‌شده نیست.
/// </summary>
public sealed record StorefrontSellerOrderView(
    Guid SellerOrderId,
    string OrderNumber,
    Guid SellerPartyId,
    string SellerDisplayName,
    string Status,
    decimal SubtotalExclusiveOfTax,
    decimal TaxAmount,
    decimal DiscountAmount,
    decimal PayableAmount,
    string Currency,
    IReadOnlyList<StorefrontCheckoutLineView> Lines);

/// <summary>
/// صفحهٔ checkout/تأیید. جمع نهایی از backend است.
/// </summary>
public sealed record StorefrontCheckoutPage(
    Guid? CheckoutId,
    Guid CartId,
    int CartVersion,
    string Market,
    string Currency,
    string Channel,
    string PaymentState,
    string ShippingMethodCode,
    string ShippingMethodLabel,
    string RecipientName,
    string ContactMobile,
    string ProvinceName,
    string CityName,
    string PostalAddress,
    string PostalCode,
    string FirstName,
    string LastName,
    decimal SubtotalExclusiveOfTax,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal ShippingAmount,
    decimal PayableAmount,
    IReadOnlyList<StorefrontSellerOrderView> SellerOrders,
    bool CanInitiatePayment = true,
    IReadOnlyList<StorefrontShippingGroupView>? ShippingGroups = null,
    /// <summary>
    /// نسخهٔ <b>واقعی</b> ریشهٔ انتخاب به‌شکل متن اعشاری دقیق برای CAS بعدی (TB-235)؛ فقط در نمای
    /// پیش‌نویس پرداخت یک انتخاب capture‌شده پر می‌شود و در بقیهٔ مسیرها تهی می‌ماند.
    /// </summary>
    string? SelectionRevision = null);

/// <summary>روش ارسال قابل انتخاب فروشگاهی از کاتالوگ Store-enabled.</summary>
/// <param name="OptionCode">
/// گزینهٔ روش ارسال Fulfillment در صورت وجود؛ تهی یعنی خدمت گزینه‌ای ندارد. این هویت گزینه
/// است، نه بخشی از کد خدمت، و برای قیمت‌گذاری گزینه‌ای مصرف می‌شود.
/// </param>
public sealed record StorefrontShippingMethodView(
    string MethodCode,
    string Label,
    string ServiceCode,
    string IconKey,
    decimal PriceAmount,
    int LeadDays,
    bool IsFree,
    string EstimationLabel,
    string? OptionCode = null);

/// <summary>
/// روشی که مسیر قدیمی واجد شرایطش می‌دانست اما نرخ authoritative مالک برایش وجود ندارد.
/// </summary>
/// <remarks>
/// این رکورد فقط <b>دلیل پایدار</b> را اعلام می‌کند: هیچ مبلغ، صفر یا «رایگان»ی ساخته نمی‌شود و
/// روش در فهرست قابل انتخاب هم حاضر نیست. تهی‌بودن فهرست در تصویر یعنی دروازهٔ نرخ مالک اجرا نشده
/// (رفتار قدیمی عیناً حفظ شده است).
/// </remarks>
/// <param name="MethodCode">کد روش متعارف.</param>
/// <param name="ReasonCode">کد دلیل پایدار رد نرخ (نبود ردیف مالک، پوشش Restricted، اندازه‌گیری لازم، ...).</param>
public sealed record StorefrontShippingUnavailableMethodView(string MethodCode, string ReasonCode);

/// <summary>گزینهٔ تاریخ تحویل.</summary>
public sealed record StorefrontDeliveryDateOption(
    string Value,
    string Label,
    string SubLabel,
    bool IsEarliest);

/// <summary>گزینهٔ پنجرهٔ ساعتی.</summary>
public sealed record StorefrontDeliveryTimeOption(string Value, string Label);

/// <summary>استان و شهرهای وابسته از کاتالوگ Host.</summary>
public sealed record StorefrontProvinceOption(string Code, string Label, IReadOnlyList<string> Cities);

/// <summary>پیش‌نویس ارسال ذخیره‌شده روی سبد.</summary>
public sealed record StorefrontShippingDraftView(
    Guid CartId,
    int CartVersion,
    string RecipientName,
    string ContactMobile,
    string ProvinceName,
    string CityName,
    string PostalAddress,
    string PostalCode,
    Guid? SavedAddressId,
    string ShippingMethodCode,
    string ShippingMethodLabel,
    decimal ShippingAmount,
    string MinimumDeliveryDate,
    string? SelectedDeliveryDate,
    string? SelectedDeliveryTimeWindow,
    string? CustomerNote,
    string FirstName = "",
    string LastName = "",
    IReadOnlyList<StorefrontShippingGroupView>? ShippingGroups = null,
    /// <summary>
    /// نسخهٔ انتخاب ذخیره‌شده به‌صورت <b>متن اعشاری دقیق</b> (TB-232)؛ همان مقداری که کلاینت باید در
    /// CAS بعدی بفرستد. صفر یعنی هیچ انتخاب capture‌شده‌ای ثبت نشده است.
    /// </summary>
    string? SelectionRevision = null);

/// <summary>تصویر یکپارچهٔ مرحلهٔ ارسال.</summary>
public sealed record StorefrontShippingProjection(
    Guid CartId,
    int CartVersion,
    string Currency,
    decimal ItemCount,
    decimal SubtotalExclusiveOfTax,
    int SellerCount,
    int MaxSellerPreparationDays,
    IReadOnlyList<StorefrontShippingMethodView> Methods,
    string? SelectedMethodCode,
    decimal SelectedShippingAmount,
    string? MinimumDeliveryDate,
    IReadOnlyList<StorefrontDeliveryDateOption> DeliveryDates,
    IReadOnlyList<StorefrontDeliveryTimeOption> DeliveryTimeWindows,
    StorefrontShippingDraftView? Draft,
    string? RevalidationMessage,
    IReadOnlyList<StorefrontProvinceOption> Provinces,
    /// <summary>
    /// TB-SHIPPING-INDEPENDENT-GROUP-BACKEND-149: افقِ <b>هر گروه</b> جداگانه. هر عضو همین گروه پایدار
    /// (<c>ShippingGroupId</c>) را با حداقل/تاریخ‌های مجاز و پنجره‌های خودش برمی‌گرداند؛ هیچ اشتراک یا
    /// دیرترین-حداقلِ سراسری در این بخش اعمال نمی‌شود. اسکالرهای بالا فقط نمای تک‌گروهی/سازگاری‌اند.
    /// </summary>
    IReadOnlyList<StorefrontShippingGroupHorizonView>? ShippingGroupHorizons = null,
    /// <summary>
    /// TB-SHIPPING-STOREFRONT-STRICT-RATE-CUTOVER-163: روش‌هایی که تنها به‌دلیل نبود نرخ authoritative
    /// مالک از فهرست حذف شده‌اند، با دلیل پایدار. <c>null</c> یعنی دروازهٔ نرخ مالک خاموش است و رفتار
    /// قدیمی عیناً حفظ شده؛ هیچ مبلغ یا صفر ساختگی برای این روش‌ها اعلام نمی‌شود.
    /// </summary>
    IReadOnlyList<StorefrontShippingUnavailableMethodView>? UnavailableMethods = null,
    /// <summary>
    /// TB-SHIPPING-STOREFRONT-GROUP-CARDS-CONNECTION-229: پیش‌نمایش <b>خواندنی</b> گروه‌های ارسال
    /// capture‌شده. <c>null</c> یعنی کلید پیش‌فرض-خاموش فعال نیست و پاسخ دقیقاً همان شکل قبلی است؛ هرگز
    /// به‌عنوان روش/قیمت/تاریخ قابل انتخاب خوانده نمی‌شود.
    /// </summary>
    StorefrontCapturedPreviewView? CapturedPreview = null);

/// <summary>
/// افق زمان‌بندی یک گروه ارسال. همهٔ مقادیر از منابع authoritative سرور می‌آید
/// (آماده‌سازی فروشنده × روزهای حمل روش)؛ هیچ تاریخ/پنجره‌ای از کلاینت یا از افق گروه دیگر
/// استنتاج نمی‌شود و هیچ تلاقی/دیرترین-حداقل سراسری اعمال نمی‌گردد.
/// </summary>
/// <param name="GroupKey">
/// کلید پایدار و سرور-مالکِ گروه در مرحلهٔ پیش‌نویس (پیش از نخستین ذخیره): ترکیب قطعی
/// (فروشندهٔ سبد × کد روش canonical). پس از ذخیرهٔ پیش‌نویس، <paramref name="ShippingGroupId"/> نیز پر
/// می‌شود و بازconciliation صریح با همین کلید انجام می‌گیرد؛ کلاینت هرگز شناسهٔ گروه نمی‌سازد.
/// </param>
/// <param name="ShippingGroupId">شناسهٔ پایدار و ذخیره‌شدهٔ گروه؛ تا پیش از نخستین ذخیره null است.</param>
/// <param name="SellerPartyId">فروشندهٔ مالک گروه (از سبد، نه از کلاینت).</param>
/// <param name="MethodCode">کد روش canonical همین گروه.</param>
/// <param name="MinimumDeliveryDate">حداقل تاریخ تحویلِ همین گروه (آماده‌سازی همین فروشنده + روزهای همین روش).</param>
/// <param name="EligibleDates">تاریخ‌های مجاز همین گروه از حداقل خودش آغاز می‌شود.</param>
/// <param name="EligibleTimeWindows">پنجره‌های مجاز همین گروه.</param>
/// <param name="SelectedDeliveryDate">تاریخ انتخاب‌شدهٔ ذخیره‌شدهٔ همین گروه (اگر ذخیره شده باشد).</param>
/// <param name="SelectedDeliveryTimeWindow">پنجرهٔ انتخاب‌شدهٔ ذخیره‌شدهٔ همین گروه.</param>
/// <param name="LeadDays">روزهای حمل مؤثر روش همین گروه.</param>
/// <param name="SellerPreparationDays">
/// روزهای آماده‌سازی فروشندهٔ همین گروه از پیکربندی سرور؛ برای شفاف‌سازی «دیرترین/زودترین» و اثبات تفاضل.
/// </param>
public sealed record StorefrontShippingGroupHorizonView(
    string GroupKey,
    Guid? ShippingGroupId,
    Guid SellerPartyId,
    string MethodCode,
    string MinimumDeliveryDate,
    IReadOnlyList<StorefrontDeliveryDateOption> EligibleDates,
    IReadOnlyList<StorefrontDeliveryTimeOption> EligibleTimeWindows,
    string? SelectedDeliveryDate,
    string? SelectedDeliveryTimeWindow,
    int LeadDays,
    int SellerPreparationDays);

/// <summary>
/// درخواست تصویر ارسال (سبد + مقصد اختیاری + روش انتخابی). <c>CityName</c> و <c>Destination</c> فقط
/// مشاهده‌ای‌اند: هر دو اختیاری و سازگار با مشتریان قدیمی که تنها استان می‌فرستند، و هیچ‌کدام قیمت،
/// روش انتخاب‌شده یا promise مشتری را تغییر نمی‌دهند.
/// </summary>
/// <param name="CartId">شناسهٔ سبد.</param>
/// <param name="ProvinceName">استان انتخاب‌شده (کد یا برچسب).</param>
/// <param name="MethodCode">روش انتخابی.</param>
/// <param name="Language">زبان نمایش.</param>
/// <param name="CityName">شهر انتخاب‌شده (نام یا کد) در صورت وجود.</param>
/// <param name="Destination">شناسه‌های پایدار Geography در صورت وجود؛ در نبود آن‌ها از متن استفاده می‌شود.</param>
public sealed record StorefrontShippingProjectionRequest(
    Guid CartId,
    string? ProvinceName = null,
    string? MethodCode = null,
    string? Language = null,
    string? CityName = null,
    Tooba.Geography.Contracts.Places.DestinationIdentifiers? Destination = null,
    /// <summary>
    /// قصد کوپن همین خواندن (TB-236)؛ همان توکنی که انتخاب/پیش‌نمایش/ثبت با آن سنجیده می‌شود.
    /// فاصله/تهی یعنی نبود. هیچ قیمت/تخفیفی از این فیلد پذیرفته نمی‌شود؛ قیمت از Promotion/Tax همان
    /// مسیر ۲۱۷ می‌آید.
    /// </summary>
    string? CouponCode = null);

/// <summary>
/// انتخاب یک گروه ارسال از سمت کلاینت: تنها قصد انتخاب (روش متعارف + خطوط سبد).
/// هیچ مبلغ، برچسب، فروشنده یا ارزی از کلاینت پذیرفته نمی‌شود؛ همه از منابع authoritative می‌آید.
/// </summary>
/// <param name="ShippingMethodCode">کد روش متعارف (مثل tipax:standard).</param>
/// <param name="CartLineIds">خطوط سبد این گروه (تمام-خط؛ هر خط دقیقاً در یک گروه).</param>
/// <param name="ServiceCode">کد سرویس؛ در صورت ارسال باید با هویت authoritative بخواند.</param>
/// <param name="OptionCode">کد گزینه؛ در صورت ارسال باید با هویت authoritative بخواند.</param>
/// <param name="OriginContext">
/// مبدأ fulfilment. در این اسلایس مرجع مورداعتمادی وجود ندارد، پس فقط خالی مجاز است و
/// مقدار اعلامی fail-closed رد می‌شود (بدون جعل انبار).
/// </param>
/// <param name="SelectedDeliveryDate">
/// TB-SHIPPING-INDEPENDENT-GROUP-BACKEND-149: زمان‌بندی مستقل همین گروه (اختیاری و افزایشی).
/// در نبود آن همان shorthand اسکالر قدیمی به‌کار می‌رود؛ در سبد چندگروهی نبودِ آن fail-closed رد می‌شود.
/// </param>
/// <param name="SelectedDeliveryTimeWindow">
/// پنجرهٔ ساعتی مستقل همین گروه؛ مجموعهٔ مجاز همیشه از محاسبهٔ authoritative سرور می‌آید.
/// </param>
public sealed record StorefrontShippingGroupSelection(
    string ShippingMethodCode,
    IReadOnlyList<Guid> CartLineIds,
    string? ServiceCode = null,
    string? OptionCode = null,
    string? OriginContext = null,
    string? SelectedDeliveryDate = null,
    string? SelectedDeliveryTimeWindow = null);
/// <summary>
/// قصد انتخاب یک گروه capture‌شده (TB-232). همهٔ فیلدهای <c>Expected*</c> فقط <b>بازتاب
/// مقایسه‌ای</b> همان چیزی هستند که سرور در خواندن اعلام کرده بود: هیچ‌کدام مجوز، مالک، مبلغ یا
/// صلاحیت نیست و سرور همه را دوباره resolve می‌کند. توکن‌های بزرگ به‌صورت متن دقیق می‌آیند تا
/// هیچ گردکردن JavaScript روی آن‌ها رخ ندهد.
/// </summary>
/// <param name="GroupKey">کلید پایدار سرورِ همان گروه (فقط برای مکان‌یابی برنامهٔ جاری).</param>
/// <param name="ServiceCode">کد خدمت انتخاب‌شده از همان خواندن.</param>
/// <param name="OptionCode">کد گزینهٔ انتخاب‌شده یا تهی.</param>
/// <param name="SelectedDeliveryDate">تاریخ ISO انتخاب‌شده از تقویم همان گروه.</param>
/// <param name="ExpectedSnapshotId">capture انتخاب‌شده که کلاینت دید.</param>
/// <param name="ExpectedSourceRevision">بازبینی دقیق به‌صورت متن.</param>
/// <param name="ExpectedSelectionVersion">نسخهٔ انتخاب به‌صورت متن.</param>
/// <param name="ExpectedAreaGroupId">گروه ناحیهٔ تطبیق‌داده‌شده که کلاینت دید.</param>
/// <param name="ExpectedRateRowId">ردیف تعرفهٔ تطبیق‌داده‌شده که کلاینت دید.</param>
public sealed record StorefrontCapturedGroupSelection(
    string GroupKey,
    string ServiceCode,
    string? OptionCode,
    string SelectedDeliveryDate,
    string? ExpectedSnapshotId = null,
    string? ExpectedSourceRevision = null,
    string? ExpectedSelectionVersion = null,
    string? ExpectedAreaGroupId = null,
    string? ExpectedRateRowId = null,
    /// <summary>
    /// بازتاب <b>مقایسه‌ای</b> مبلغ کالای همین گروه پس از تخفیف/مالیات که کلاینت در همان خواندن دید
    /// (TB-236؛ متن دقیق). نبود یعنی این بازتاب ارسال نشده و «صفر» نیست؛ مقدار ارسالی فقط برای مقایسه
    /// است و هرگز قیمت/مالیات/تخفیف نیست.
    /// </summary>
    string? ExpectedGoodsAmount = null,
    /// <summary>
    /// TB-241: تیک‌های دقیق شروع نوبت انتخاب‌شده (پاسخ سرور). این مقدار فقط برای
    /// <b>بازشناسی</b> همان نوبت در تقویم تازهٔ سرور است؛ هرگز پنجره/ساعت/اعتبار نیست و نبودش یعنی
    /// نوبت انتخاب نشده (فقط برای روش بدون مدت، پنجرهٔ کامل روز).
    /// </summary>
    long? SelectedSlotStartTicks = null,
    /// <summary>TB-241: تیک‌های دقیق پایان نوبت انتخاب‌شده؛ باید با نوبت موجود همان تاریخ بخواند.</summary>
    long? SelectedSlotEndTicks = null);

/// <summary>
/// نمای یک گروه ارسال در پیش‌نویس سبد. هویت/برچسب/مبلغ/ارز/زمان‌بندی همه از گروه ذخیره‌شده می‌آید.
/// </summary>
public sealed record StorefrontShippingGroupView(
    Guid ShippingGroupId,
    Guid SellerPartyId,
    string MethodCode,
    string? ServiceCode,
    string? OptionCode,
    string MethodLabel,
    decimal ShippingAmount,
    string Currency,
    string MinimumDeliveryDate,
    string? SelectedDeliveryDate,
    string? SelectedDeliveryTimeWindow,
    int LeadDays,
    int LineCount,
    /// <summary>
    /// شناسهٔ واقعی خطوط سبد همین گروه (Whole-line membership). برای بازیابی قطعی انتخاب «هر خط» پس از
    /// رفرش/برگشت لازم است؛ هیچ مبلغ/فروشنده‌ای از آن استنتاج نمی‌شود. در پاسخ قدیمی خالی می‌ماند.
    /// </summary>
    IReadOnlyList<Guid>? CartLineIds = null,
    /// <summary>
    /// شناسهٔ واقعی خطوط <b>سفارش</b> همین گروه برای گروه‌های <b>پایدارشده/تاریخی</b> (TB-235).
    /// <c>OrderLineId</c> هرگز جای <c>CartLineId</c> نیست؛ در نمای پیش‌نویس خالی و در نمای سفارشِ
    /// ذخیره‌شده پر می‌شود. هیچ مبلغی از این شناسه‌ها استنتاج نمی‌شود.
    /// </summary>
    IReadOnlyList<Guid>? OrderLineIds = null,
    /// <summary>
    /// TB240-R4: شاهد فشردهٔ <b>ذخیره‌شده در خود Order</b> برای گروه‌های پایدار. در نمای پیش‌نویس/زنده تهی است و
    /// فقط از ستون‌های `order.order_shipping_groups` خوانده می‌شود؛ هیچ lookup ارسال/تعرفه/گروه منطقه‌ای برای
    /// ساختن آن انجام نمی‌شود و هیچ مبلغی بازمحاسبه نمی‌گردد.
    /// </summary>
    StorefrontShippingGroupHistoryView? History = null);

/// <summary>
/// نمایش فقط-خواندنی شاهد پذیرفته‌شدهٔ یک گروه ارسالِ <b>ذخیره‌شده</b> (TB240-R4).
/// </summary>
/// <remarks>
/// همهٔ مقادیر عیناً از Order storage می‌آیند؛ نبودِ شاهد ⇒ <c>null</c> (گروه قدیمی) و هیچ مقدار پیش‌فرض/جعل‌شده
/// ساخته نمی‌شود. تیک‌های 100ns بازهٔ تحویل بدون کوتاه‌سازی منتقل می‌شوند.
/// </remarks>
public sealed record StorefrontShippingGroupHistoryView(
    string OwnerKind,
    string? OwnerTenantKey,
    Guid? OwnerSellerPartyId,
    Guid SnapshotId,
    Guid DraftId,
    long SourceRevision,
    int FormatVersion,
    long SelectionVersion,
    Guid AreaGroupId,
    Guid RateRowId,
    Guid? ShippingTypeId,
    decimal FreightAmount,
    decimal PackagingAmount,
    decimal PayableAtCheckoutAmount,
    decimal CarrierCollectAmount,
    string ReceivingDate,
    long ReceivingOpenTicks,
    long ReceivingCloseTicks,
    string ReceivingTimeZone,
    int? MinimumDeliveryDays,
    int? MaximumDeliveryDays);

/// <summary>
/// ذخیرهٔ انتخاب ارسال. <see cref="ShippingGroups"/> افزایشی و اختیاری است: در نبود آن همان
/// انتخاب تک‌روشی گذشته به‌عنوان shorthand سازگار پذیرفته و به یک گروه کامل تبدیل می‌شود.
/// </summary>
/// <remarks>
/// <see cref="CapturedGroups"/> قصد انتخاب گروه‌های capture‌شده است (TB-232). حضور آن هرگونه
/// shorthand قدیمی را مبهم می‌کند و صریحاً رد می‌شود؛ نبودش رفتار قدیمی را عیناً حفظ می‌کند.
/// <see cref="ExpectedSelectionRevision"/> نسخهٔ انتخاب مشاهده‌شده به‌صورت متن دقیق است (CAS اتمیک).
/// <see cref="CouponCode"/> تنها قصدِ تخفیف است و از همان مسیر ۲۱۷ (Pricing/Promotion/Tax) قیمت می‌گیرد؛
/// هیچ مبلغ/درصدی از کلاینت پذیرفته نمی‌شود.
/// </remarks>
public sealed record StorefrontShippingSelectionRequest(
    Guid CartId,
    int ExpectedCartVersion,
    string RecipientName,
    string ContactMobile,
    string ProvinceName,
    string CityName,
    string PostalAddress,
    string PostalCode,
    Guid? SavedAddressId,
    string ShippingMethodCode,
    string SelectedDeliveryDate,
    string SelectedDeliveryTimeWindow,
    string? CustomerNote,
    string FirstName = "",
    string LastName = "",
    IReadOnlyList<StorefrontShippingGroupSelection>? ShippingGroups = null,
    IReadOnlyList<StorefrontCapturedGroupSelection>? CapturedGroups = null,
    string? ExpectedSelectionRevision = null,
    string? CouponCode = null);

/// <summary>ثبت نهایی ارسال و ساخت checkout برای پرداخت.</summary>
public sealed record StorefrontShippingCommitRequest(
    Guid CartId,
    int ExpectedCartVersion,
    string IdempotencyKey,
    string? CouponCode = null);

/// <summary>
/// ورودی «کلیک نهایی پرداخت» فروشگاهی. مبلغ/شناسهٔ سفارش/رزرو/مهلت در بدنه نیست؛ CartId تنها از مسیر
/// می‌آید، هویت تنها از مرز اعتماد، و روش پرداخت انتخاب‌شده با <see cref="UseWallet"/> یا
/// <see cref="ProviderCode"/> اعلام می‌شود (UseWallet یا ProviderCode=wallet فقط وقتی موجودی کل مبلغ را
/// پوشش دهد مجاز است؛ mixed deferred). کلید idempotency باید پایدار و از سمت فراخوان باشد.
/// </summary>
public sealed record StorefrontStartPaymentBody(
    int ExpectedCartVersion,
    string IdempotencyKey,
    bool UseWallet = false,
    string? ProviderCode = null,
    string? CouponCode = null,
    /// <summary>
    /// نسخهٔ انتخاب capture‌شده به‌صورت <b>متن</b> (TB-233). فقط برای مسیر capture لازم است؛ نبود/تهی/بدشکل
    /// در آن مسیر امن‌بسته رد می‌شود و رفتار درخواست قدیمی تغییری نمی‌کند.
    /// </summary>
    string? ExpectedSelectionRevision = null);

/// <summary>
/// پاسخ موفق «کلیک نهایی پرداخت»: مرجع پایدار سفارش و پرداخت. هیچ فیلد مشتق/حدسی اضافه نمی‌شود.
/// </summary>
public sealed record StorefrontStartPaymentResponse(
    Guid CheckoutId,
    bool CheckoutReplayed,
    Guid PaymentId,
    Guid AttemptId,
    string PaymentStatus,
    string ProviderCode,
    string RedirectUrl,
    decimal Amount,
    string Currency,
    bool RequiresPspRedirect,
    int ReservationCount,
    DateTimeOffset? GuaranteedUntilUtc);

/// <summary>درخواست وابسته به سبد برای لغو/پنهان کارت.</summary>
public sealed record StorefrontPaymentCartRequest(Guid CartId);
public sealed record StorefrontPendingPaymentProofRequest(Guid CheckoutId, Guid CartId, string? GuestSecret);

/// <summary>درخواست تصویر دسته‌ای در انتظار پرداخت. مهمان فقط با proofs.</summary>
public sealed record StorefrontPendingPaymentQueryRequest(IReadOnlyList<StorefrontPendingPaymentProofRequest>? Proofs);

/// <summary>قلم فشرده برای کارت در انتظار پرداخت.</summary>
public sealed record StorefrontPendingPaymentLineView(string Title, decimal Quantity, Guid? MediaAssetId);

/// <summary>یک سفارش متعهد unpaid برای بخش در انتظار پرداخت.</summary>
public sealed record StorefrontPendingPaymentItemView(
    Guid CheckoutId,
    Guid CartId,
    string OrderReference,
    decimal PayableAmount,
    string Currency,
    IReadOnlyList<StorefrontPendingPaymentLineView> Items,
    string PaymentPresentation,
    bool CanInitiatePayment,
    bool CanRetryPayment,
    bool IsManualAwaitingReview,
    string PrimaryAction,
    string ReservationPresentation,
    int? CycleNumber,
    int SecondsRemaining,
    DateTimeOffset ServerTime,
    DateTimeOffset? HoldEndsAt,
    int MaxCycles,
    int RetryCountRemaining,
    string? SupplyStatus,
    Guid? PaymentId,
    bool HasReachedRetryLimit);

/// <summary>پاسخ دسته‌ای در انتظار پرداخت.</summary>
public sealed record StorefrontPendingPaymentPage(
    DateTimeOffset ServerTime,
    IReadOnlyList<StorefrontPendingPaymentItemView> Items);

/// <summary>
/// یک روز از تقویم <b>دریافت مشتری</b> برای یک روش ارسال.
/// </summary>
/// <remarks>
/// این تصویر فقط پیکربندی مورد اعتماد سمت سرور است و از مرورگر/لوکال مرورگر خوانده نمی‌شود.
/// <c>IsWorking</c> برابر false یعنی آن روز تعطیل است و هیچ بازه‌ای ارائه نمی‌شود؛ مقدار
/// <c>OpensAt</c>/<c>ClosesAt</c> در روز تعطیل معنا ندارد و حتی اگر تنظیم شده باشد نادیده گرفته
/// می‌شود (نه اصلاح و نه پیش‌فرض‌سازی). در روز کاری، بازه باید مثبت و غیرمعکوس باشد؛ هیچ clamp یا
/// جابه‌جایی انجام نمی‌شود.
/// </remarks>
public sealed record StorefrontShippingReceivingDay(
    DayOfWeek DayOfWeek,
    bool IsWorking,
    TimeOnly OpensAt,
    TimeOnly ClosesAt);

/// <summary>
/// تقویم هفتگی دریافت مشتری: دقیقاً هفت روز، هر روز یک‌بار و بدون تکرار/خارج از محدوده.
/// </summary>
public sealed record StorefrontShippingReceivingWeek(
    IReadOnlyList<StorefrontShippingReceivingDay> Days,
    /// <summary>
    /// TB-241: مدت اختیاری هر نوبت تحویل به دقیقه از همان capture تغییرناپذیر. تهی یعنی تقویم فقط
    /// یک پنجرهٔ کامل روز را برای هر تاریخ ارائه می‌دهد و هرگز به ۶۰ دقیقه استنتاج نمی‌شود.
    /// </summary>
    int? SlotDurationMinutes = null);

/// <summary>
/// یک تاریخ قابل انتخاب برای دریافت کالا، همراه با بازهٔ <b>همان روز</b> در تقویم روش.
/// </summary>
public sealed record StorefrontShippingReceivingSlot(DateOnly Date, TimeOnly OpensAt, TimeOnly ClosesAt);

/// <summary>
/// نتیجهٔ تقویم دریافت مشتری.
/// </summary>
/// <param name="EarliestEligibleDate">
/// زودترین تاریخ دریافت ممکن بر اساس وعدهٔ حداقل N روز کاری؛ اگر هیچ روز کاری در هفته نباشد null است.
/// </param>
/// <param name="AvailableDates">
/// تاریخ‌های قابل ارائه با بازهٔ همان روز؛ هرگز روز تعطیل در آن نیست. اگر افق درخواستی صفر باشد خالی است.
/// </param>
public sealed record StorefrontShippingReceivingCalendar(
    DateOnly? EarliestEligibleDate,
    IReadOnlyList<StorefrontShippingReceivingSlot> AvailableDates);
