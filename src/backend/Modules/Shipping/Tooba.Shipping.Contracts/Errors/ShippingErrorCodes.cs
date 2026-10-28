namespace Tooba.Shipping.Contracts.Errors;

/// <summary>
/// کدهای خطای پایدار Shipping برای سیاست نرخ و پوشش. کدها مستقل از زبان‌اند و هیچ متن
/// محلی‌سازی‌شده‌ای در این لایه نگه‌داری نمی‌شود.
/// </summary>
public static class ShippingErrorCodes
{
    /// <summary>کد خدمت ارسال الزامی است.</summary>
    public const string ServiceCodeRequired = "shipping.service_code.required";

    /// <summary>کد خدمت ارسال خارج از محدودهٔ مجاز طول است.</summary>
    public const string ServiceCodeInvalid = "shipping.service_code.invalid";

    /// <summary>شناسهٔ کشور مقصد الزامی است.</summary>
    public const string DestinationCountryIdRequired = "shipping.destination.country_id.required";

    /// <summary>مبلغ هزینه نباید منفی باشد.</summary>
    public const string ChargeAmountNegative = "shipping.charge.amount.negative";

    /// <summary>مبلغ هزینه خارج از محدودهٔ مجاز است.</summary>
    public const string ChargeAmountOutOfRange = "shipping.charge.amount.out_of_range";

    /// <summary>کد ارز الزامی است و باید کد سه‌حرفی باشد.</summary>
    public const string CurrencyCodeInvalid = "shipping.charge.currency.invalid";

    /// <summary>روزهای آماده‌سازی نباید منفی باشد.</summary>
    public const string PreparationDaysNegative = "shipping.preparation_days.negative";

    /// <summary>روزهای حمل نباید منفی باشد.</summary>
    public const string LeadDaysNegative = "shipping.lead_days.negative";

    /// <summary>آستانهٔ ارسال رایگان نباید منفی باشد.</summary>
    public const string FreeAboveThresholdNegative = "shipping.free_above_threshold.negative";

    /// <summary>سطح دامنهٔ پوشش نامعتبر است (ترکیب شناسه‌های مقصد ناسازگار).</summary>
    public const string CoverageScopeInvalid = "shipping.coverage.scope.invalid";

    /// <summary>سیاست نرخ با این شناسه یافت نشد.</summary>
    public const string RatePolicyNotFound = "shipping.rate_policy.not_found";

    /// <summary>مجوز پوشش با این شناسه یافت نشد.</summary>
    public const string CoverageGrantNotFound = "shipping.coverage_grant.not_found";

    /// <summary>برای همین کد خدمت و همین دامنهٔ مقصد از قبل یک سیاست نرخ وجود دارد.</summary>
    public const string RatePolicyScopeDuplicate = "shipping.rate_policy.scope_duplicate";

    /// <summary>برای همین کد خدمت و همین دامنهٔ مقصد از قبل یک مجوز پوشش وجود دارد.</summary>
    public const string CoverageGrantScopeDuplicate = "shipping.coverage_grant.scope_duplicate";

    /// <summary>کد خدمت در هیچ ردیف کاتالوگ خدمت Fulfillment وجود ندارد.</summary>
    public const string ServiceCodeUnknown = "shipping.service_code.unknown";

    /// <summary>ردیف درخواستی به کد خدمت دیگری تعلق دارد؛ تغییر میان‌خدمتی مجاز نیست.</summary>
    public const string ServiceScopeMismatch = "shipping.service_scope.mismatch";

    /// <summary>دامنهٔ مقصد در فهرست مرجع Geography ناشناخته، غیرفعال یا ناهم‌بسته است.</summary>
    public const string DestinationScopeInvalid = "shipping.destination_scope.invalid";

    /// <summary>کد گزینهٔ روش ارسال خارج از محدودهٔ مجاز طول است.</summary>
    public const string OptionCodeInvalid = "shipping.option_code.invalid";

    /// <summary>کد گزینهٔ ارسال در کاتالوگ خدمت Fulfillment برای این خدمت وجود ندارد.</summary>
    public const string ServiceOptionUnknown = "shipping.service_option.unknown";

    /// <summary>هویت روش (خدمت + گزینه) یک ردیف با مسیر/بدنهٔ درخواست یکسان نیست.</summary>
    public const string RatePolicyOptionMismatch = "shipping.rate_policy.option_mismatch";

    /// <summary>شناسهٔ منطقهٔ زمانی برنامهٔ عملیاتی الزامی است.</summary>
    public const string ScheduleTimeZoneRequired = "shipping.schedule.timezone.required";

    /// <summary>شناسهٔ منطقهٔ زمانی روی این runtime قابل حل نیست (ترجیحاً IANA).</summary>
    public const string ScheduleTimeZoneInvalid = "shipping.schedule.timezone.invalid";

    /// <summary>شمارهٔ روز هفته خارج از بازهٔ ISO 1..7 است.</summary>
    public const string ScheduleWeekdayInvalid = "shipping.schedule.weekday.invalid";

    /// <summary>
    /// بازهٔ پنجرهٔ عملیاتی نامعتبر است: <c>end &lt;= start</c>. این حالت شامل پنجرهٔ معکوس،
    /// پنجرهٔ با طول صفر و پنجرهٔ شبانه (عبور از نیمه‌شب) است که در این نسخه پشتیبانی نمی‌شود.
    /// </summary>
    public const string ScheduleWindowRangeInvalid = "shipping.schedule.window.range_invalid";

    /// <summary>دو پنجرهٔ عملیاتی روی یک روز هفته هم‌پوشانی دارند.</summary>
    public const string ScheduleWindowOverlap = "shipping.schedule.window.overlap";

    /// <summary>ظرفیت اعلام‌شدهٔ پنجرهٔ عملیاتی باید مثبت باشد.</summary>
    public const string ScheduleWindowCapacityInvalid = "shipping.schedule.window.capacity.invalid";

    /// <summary>تعداد پنجره‌های عملیاتی از سقف مجاز بیشتر است.</summary>
    public const string ScheduleWindowsTooMany = "shipping.schedule.windows.too_many";

    /// <summary>برنامهٔ عملیاتی با این شناسه یافت نشد.</summary>
    public const string ScheduleNotFound = "shipping.schedule.not_found";

    /// <summary>برای همین خدمت و همین گزینه از قبل یک برنامهٔ عملیاتی وجود دارد.</summary>
    public const string ScheduleDuplicate = "shipping.schedule.duplicate";

    /// <summary>هویت روش برنامهٔ عملیاتی با مسیر/بدنهٔ درخواست یکسان نیست.</summary>
    public const string ScheduleOptionMismatch = "shipping.schedule.option_mismatch";

    /// <summary>TB-241: مدت هر نوبت تحویل باید عدد صحیح بین ۱ و ۱۴۴۰ دقیقه باشد؛ تهی یعنی پنجرهٔ کامل روز.</summary>
    public const string ScheduleSlotDurationInvalid = "shipping.schedule.slot_duration.invalid";

    /// <summary>بازهٔ وزن نامعتبر است: کران پایین منفی، یا کران بالا بزرگ‌تر از کران پایین نیست.</summary>
    public const string WeightTierInvalid = "shipping.rate_policy.weight_tier.invalid";

    /// <summary>بازهٔ وزن خارج از سقف متعارف است.</summary>
    public const string WeightTierOutOfRange = "shipping.rate_policy.weight_tier.out_of_range";

    /// <summary>بازهٔ تعداد اقلام نامعتبر است: کران پایین منفی، یا کران بالا بزرگ‌تر از کران پایین نیست.</summary>
    public const string CountTierInvalid = "shipping.rate_policy.count_tier.invalid";

    /// <summary>بازهٔ تعداد اقلام خارج از سقف متعارف است.</summary>
    public const string CountTierOutOfRange = "shipping.rate_policy.count_tier.out_of_range";

    /// <summary>
    /// دو پلهٔ فعال برای یک هویت (خدمت/گزینه/دامنه) هم‌پوشانی دارند؛ چنین داده‌ای ابهام تجاری است
    /// و به‌جای انتخاب سطر اول رد می‌شود.
    /// </summary>
    public const string RatePolicyTierOverlap = "shipping.rate_policy.tier_overlap";

    /// <summary>سیاست نرخ با این شناسه یافت نشد (برای شرط/پله).</summary>
    public const string RatePolicyConditionNotFound = "shipping.rate_policy.condition.not_found";

    /// <summary>هزینهٔ بسته‌بندی نباید منفی باشد.</summary>
    public const string PackagingFeeNegative = "shipping.packaging.fee.negative";

    /// <summary>هزینهٔ بسته‌بندی خارج از محدودهٔ مجاز است.</summary>
    public const string PackagingFeeOutOfRange = "shipping.packaging.fee.out_of_range";

    /// <summary>کد ارز هزینهٔ بسته‌بندی الزامی است و باید کد سه‌حرفی باشد.</summary>
    public const string PackagingCurrencyInvalid = "shipping.packaging.currency.invalid";

    /// <summary>سیاست هزینهٔ بسته‌بندی با این شناسه یافت نشد.</summary>
    public const string PackagingPolicyNotFound = "shipping.packaging.policy.not_found";

    /// <summary>برای همین کد خدمت/گزینه/دامنه از قبل یک سیاست هزینهٔ بسته‌بندی وجود دارد.</summary>
    public const string PackagingPolicyScopeDuplicate = "shipping.packaging.policy.scope_duplicate";

    /// <summary>هویت روش (خدمت + گزینه) یک سیاست بسته‌بندی با مسیر/بدنهٔ درخواست یکسان نیست.</summary>
    public const string PackagingPolicyOptionMismatch = "shipping.packaging.policy.option_mismatch";

    /// <summary>
    /// دو سیاست فعال بسته‌بندی روی همان هویت (خدمت/گزینه/دامنه) با هم مبهم‌اند؛ انتخاب سطر اول
    /// یک تصمیم تجاری پنهان است، پس نوشتن/حل صریحاً رد می‌شود.
    /// </summary>
    public const string PackagingPolicyAmbiguous = "shipping.packaging.policy.ambiguous";

    /// <summary>طرف پرداخت‌کنندهٔ حمل نامعتبر است (مقدار خارج از قرارداد پایدار).</summary>
    public const string PackagingFreightPayerModeInvalid = "shipping.packaging.freight_payer_mode.invalid";

    /// <summary>
    /// زمینهٔ نویسندگی Shipping قابل ساخت نیست: حالت نصب/ارز پیش‌فرض resolve نشده یا ناسازگار است،
    /// هیچ زبان محتوای فعالی وجود ندارد، پیش‌فرض فعال یکتا نیست، یا metadata الزامی مالک ناقص است.
    /// این یک خطای دسترس‌پذیری مصرف‌کننده است و هیچ زبان/ارز/حالتی را فعال یا حدس نمی‌زند.
    /// </summary>
    public const string AuthoringConfigurationUnavailable = "shipping.authoring.configuration_unavailable";

    /// <summary>
    /// مجوز کافی برای محدودهٔ مالک درخواستی وجود ندارد: actor برای فروشندهٔ درخواستی مجاز نشده،
    /// مجوز <c>fulfillment.view</c> در سطح کل مالک ندارد، یا فقط grant محدود به دسته/محصول/برند/انبار
    /// یا ردشده با سقف دارد. این پاسخ هیچ اطلاعاتی دربارهٔ وجود مالک/فروشنده leakage نمی‌کند.
    /// </summary>
    public const string AuthoringOwnerScopeDenied = "shipping.authoring.owner_scope_denied";

    /// <summary>
    /// مالکِ درخواستی در این نصب اصلاً وجود ندارد یا قابل ارائه نیست؛ مثلاً مسیر فروشنده در نصب
    /// SingleStore. این یک ناتوانی قابلیت است، نه رد مجوز و نه «فروشنده پیدا نشد».
    /// </summary>
    public const string AuthoringOwnerScopeUnavailable = "shipping.authoring.owner_scope_unavailable";

    /// <summary>
    /// پیش‌نویس در محدودهٔ مالک مجاز پیدا نشد. شناسهٔ موجودِ مالک دیگر یا شناسهٔ ساختگی همان پاسخ
    /// ایمن را می‌گیرد تا هیچ اطلاعاتی دربارهٔ مالکیت افشا نشود.
    /// </summary>
    public const string AuthoringDraftNotFound = "shipping.authoring.draft_not_found";

    /// <summary>
    /// بازبینی ارسالی با بازبینی ذخیره‌شده نمی‌خواند؛ جایگزینی رد می‌شود و هیچ داده‌ای تغییر نمی‌کند.
    /// </summary>
    public const string AuthoringDraftRevisionConflict = "shipping.authoring.draft_revision_conflict";

    /// <summary>
    /// برای همین مالک و همین Binding (کد خدمت + گزینه) از قبل یک پیش‌نویس وجود دارد؛
    ///مالک دیگری می‌تواند همان Binding را مستقل داشته باشد.
    /// </summary>
    public const string AuthoringDraftBindingDuplicate = "shipping.authoring.draft_binding_duplicate";

    /// <summary>
    /// دو انتخاب در یک پیش‌نویس هم‌پوشان‌اند: یکی جدِ دیگری است یا هر دو یکی هستند. این قاعده روی
    /// همهٔ گروه‌های همان پیش‌نویس اجرا می‌شود، نه فقط داخل یک گروه.
    /// </summary>
    public const string AuthoringRegionSelectionOverlap = "shipping.authoring.region_selection_overlap";

    /// <summary>
    /// درخت منطقه‌ای از نظر شکل نامعتبر است: گروه/انتخاب ناشناخته، شناسهٔ گروه متعلق به پیش‌نویس
    /// دیگر، یا عبور از سقف فنی payload.
    /// </summary>
    public const string AuthoringRegionPayloadInvalid = "shipping.authoring.region_payload_invalid";

    /// <summary>
    /// گروه منطقه‌ای درخت در محدودهٔ مالک مجاز پیدا نشد. شناسهٔ متعلق به پیش‌نویس دیگر یا شناسهٔ ساختگی
    /// همان پاسخ ایمن را می‌گیرد تا هیچ اطلاعاتی دربارهٔ مالکیت افشا نشود.
    /// </summary>
    public const string AuthoringRegionGroupNotFound = "shipping.authoring.region_group_not_found";

    /// <summary>
    /// مجموعهٔ ردیف‌های نرخ از نظر شکل/هویت نامعتبر است: ردیف null، شناسهٔ ردیف ناشناخته یا متعلق به
    /// گروه/پیش‌نویس دیگر، شناسهٔ تکراری، یا عبور از سقف فنی payload.
    /// </summary>
    public const string AuthoringRateRowPayloadInvalid = "shipping.authoring.rate_row_payload_invalid";

    /// <summary>بازهٔ شرط یک ردیف نرخ نامعتبر است: معکوس، صفرعرض، کران‌بالای بدون کران‌پایین یا خارج از سقف فنی.</summary>
    public const string AuthoringRateConditionInvalid = "shipping.authoring.rate_condition_invalid";

    /// <summary>مبلغ یک ردیف نرخ نامعتبر است: منفی، خارج از محدوده، یا با دقت اعشاری غیرقابل‌نمایش.</summary>
    public const string AuthoringRateAmountInvalid = "shipping.authoring.rate_amount_invalid";

    /// <summary>مدت تحویل یک ردیف نرخ نامعتبر است: منفی، بیشینه کوچک‌تر از کمینه، یا خارج از محدوده.</summary>
    public const string AuthoringRateDurationInvalid = "shipping.authoring.rate_duration_invalid";

    /// <summary>
    /// دو ردیف نرخ در یک گروه هم‌پوشانی دارند: حاصل‌ضرب کامل سه محور شرط، حداقل یک نقطهٔ معتبر مشترک
    /// دارد. ردیف‌های جایگزین یکدیگرند، پس هم‌پوشانی به‌جای انتخاب تصادفی رد می‌شود.
    /// </summary>
    public const string AuthoringRateOverlap = "shipping.authoring.rate_overlap";

    /// <summary>
    /// ارز معتبر فعلی با ارز ذخیره‌شدهٔ همین مجموعهٔ ردیف‌ها نمی‌خواند. مبالغ قدیمی هرگز با یک
    /// تغییر پیکربندی بازتفسیر یا تبدیل نمی‌شوند؛ نوشتن fail-closed رد می‌شود.
    /// </summary>
    public const string AuthoringRateCurrencyMismatch = "shipping.authoring.rate_currency_mismatch";

    /// <summary>بدنهٔ ارائهٔ پیش‌نویس از نظر شکل نامعتبر است (تکراری، خالی یا بیش‌ازحد طولانی).</summary>
    public const string AuthoringPresentationPayloadInvalid = "shipping.authoring.presentation_payload_invalid";

    /// <summary>زبان ارجاع‌شده در registry فعال authoritative وجود ندارد یا غیرفعال است.</summary>
    public const string AuthoringPresentationLanguageInvalid = "shipping.authoring.presentation_language_invalid";

    /// <summary>
    /// مجموعهٔ روزهای هفتهٔ برنامهٔ مالک دقیقاً هفت روز یکتا با هویت ISO ۱..۷ نیست: روز تکراری،
    /// روز حذف‌شده یا تعداد نادرست. روز غایب «تعطیل» نیست، پیکربندی ناقص است.
    /// </summary>
    public const string AuthoringScheduleWeekdaySetInvalid = "shipping.authoring.schedule_weekday_set_invalid";

    /// <summary>
    /// شکل یک روز برنامهٔ مالک نامعتبر است: روز کاری بدون هر دو ساعت، یا یک ساعت بدون جفت. بازهٔ
    /// معکوس/مساوی/شبانه کد جداگانهٔ <c>shipping.schedule.window.range_invalid</c> را می‌گیرد.
    /// </summary>
    public const string AuthoringScheduleDayInvalid = "shipping.authoring.schedule_day_invalid";

    /// <summary>
    /// تنظیمات روش پیش‌نویس نامعتبر است: زمان پرداخت کرایه یا حالت محاسبه غایب/تهی است، یا مقدار
    /// عددی خارج از مقادیر تعریف‌شده آمده است. مقدار صفر هرگز «پیش‌فرض» یک فیلد ارسال‌نشده نیست.
    /// </summary>
    public const string AuthoringMethodSettingsInvalid = "shipping.authoring.method_settings_invalid";

    /// <summary>
    /// شکل انتخاب نوع ارسال نامعتبر است: شناسهٔ خالی (<c>Guid.Empty</c>) هرگز «انتخاب‌نشده» نیست.
    /// </summary>
    public const string AuthoringTypeAssignmentInvalid = "shipping.authoring.type_assignment_invalid";

    /// <summary>
    /// تعریف نوع انتخاب‌شده در همین لحظه قابل استفاده نیست: در کاتالوگ Fulfillment وجود ندارد یا
    /// غیرفعال است، یا فکت برگشتی با شناسهٔ درخواستی/مالک نمی‌خواند. هیچ نوشتنی انجام نمی‌شود.
    /// </summary>
    public const string AuthoringTypeSelectionUnavailable = "shipping.authoring.type_selection_unavailable";

    /// <summary>
    /// بدنهٔ ذخیره‌شدهٔ یک snapshot بازبینی پیش‌نویس قابل رمزگشایی نیست: نسخهٔ قالب ناشناخته، فیلد
    /// الزامی غایب یا مقدار ناخوانا. هیچ بازیابی «تهی» جای این خطا را نمی‌گیرد.
    /// </summary>
    public const string AuthoringRevisionSnapshotInvalid = "shipping.authoring.revision_snapshot_invalid";

    /// <summary>
    /// capture درخواستی برای همان (مالک، پیش‌نویس، بازبینی مبدأ) وجود ندارد. این «پیدا نشد» امن است و
    /// هیچ جایگزینی با آخرین/فعال/جاری ندارد.
    /// </summary>
    public const string AuthoringRevisionSnapshotNotFound = "shipping.authoring.revision_snapshot_not_found";

    /// <summary>
    /// نسخهٔ اشاره‌گر انتخاب با وضعیت ذخیره‌شده نمی‌خواند (race/آخرین‌نویس‌برنده ممنوع). هیچ نوشتنی
    /// انجام نشده و هیچ تلاش دوبارهٔ خودکار وجود ندارد.
    /// </summary>
    public const string SelectedCaptureConflict = "shipping.selected_capture.conflict";

    /// <summary>
    /// انتخاب درخواستی به یک capture موجود و متعلق به همین مالک اشاره نمی‌کند؛ یا payload تأییدشده با
    /// هدر/مالک/Binding نمی‌خواند. هیچ نوشتنی انجام نمی‌شود.
    /// </summary>
    public const string SelectedCaptureSnapshotUnavailable = "shipping.selected_capture.snapshot_unavailable";

    /// <summary>Binding درخواستی با Binding استخراج‌شده از capture تأییدشده یکسان نیست.</summary>
    public const string SelectedCaptureBindingMismatch = "shipping.selected_capture.binding_mismatch";

    /// <summary>ورودی اشاره‌گر انتخاب نامعتبر است (نسخهٔ غیرمثبت، شناسهٔ تهی، سرویس خالی).</summary>
    public const string SelectedCaptureInvalid = "shipping.selected_capture.invalid";
}
