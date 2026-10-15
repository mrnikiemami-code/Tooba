namespace Tooba.Shipping.Contracts.Scheduling;

/// <summary>
/// یک پنجرهٔ عملیاتی در قرارداد ادمین: بازهٔ نیم‌باز <c>[StartTime, EndTime)</c> روی یک روز هفته.
/// </summary>
/// <param name="Weekday">روز هفتهٔ ISO: ۱ دوشنبه تا ۷ یکشنبه.</param>
/// <param name="StartTime">شروع به وقت محلی منطقهٔ زمانی برنامه؛ شامل.</param>
/// <param name="EndTime">پایان به وقت محلی منطقهٔ زمانی برنامه؛ <b>شامل نیست</b>.</param>
/// <param name="Capacity">
/// ظرفیت اختیاری این پنجره. فقط یک <em>پیکربندی</em> است و اجرا نمی‌شود، چون هیچ مسیر
/// رزرو/شمارش اتمی در این نسخه وجود ندارد.
/// </param>
public sealed record ShippingOperatingWindowWrite(
    int Weekday,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int? Capacity = null);

/// <summary>
/// بدنهٔ ایجاد/ویرایش برنامهٔ عملیاتی هفتگی یک کد خدمت.
/// </summary>
/// <param name="ServiceCode">کد خدمت Fulfillment؛ فقط کلید سازگاری، بدون تکرار ردیف کاتالوگ.</param>
/// <param name="TimeZoneId">
/// شناسهٔ منطقهٔ زمانی (ترجیحاً IANA، مثلاً <c>Asia/Tehran</c>). اجباری است: بدون آن «13:00»
/// قابل تفسیر نیست.
/// </param>
/// <param name="Windows">پنجره‌های هفتگی؛ جایگزینی کامل مجموعه در هر نوشتن.</param>
/// <param name="OptionCode">
/// گزینهٔ روش ارسال در صورت وجود؛ تهی یعنی برنامه در سطح خود خدمت است. این مقدار هویت گزینه است،
/// نه کد خدمت: «tipax:express» خدمت tipax با گزینهٔ express است.
/// </param>
public sealed record ShippingOperatingScheduleWriteRequest(
    string ServiceCode,
    string TimeZoneId,
    IReadOnlyList<ShippingOperatingWindowWrite> Windows,
    string? OptionCode = null);

/// <summary>بدنهٔ تغییر وضعیت فعال/غیرفعال یک برنامهٔ عملیاتی.</summary>
/// <param name="IsActive">وضعیت هدف.</param>
public sealed record ShippingOperatingScheduleActiveRequest(bool IsActive);

/// <summary>یک پنجرهٔ عملیاتی همان‌طور که ادمین می‌بیند.</summary>
/// <param name="WindowId">شناسهٔ پایدار پنجره.</param>
/// <param name="Weekday">روز هفتهٔ ISO ۱..۷.</param>
/// <param name="StartTime">شروع؛ شامل.</param>
/// <param name="EndTime">پایان؛ شامل نیست.</param>
/// <param name="Capacity">ظرفیت پیکربندی‌شده؛ تهی یعنی تعیین‌نشده.</param>
public sealed record ShippingOperatingWindowAdminItem(
    Guid WindowId,
    int Weekday,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int? Capacity);

/// <summary>یک برنامهٔ عملیاتی همان‌طور که ادمین می‌بیند.</summary>
/// <param name="ScheduleId">شناسهٔ پایدار برنامه.</param>
/// <param name="ServiceCode">کد خدمت Fulfillment.</param>
/// <param name="OptionCode">گزینهٔ روش ارسال؛ تهی یعنی برنامهٔ سطح‌خدمت.</param>
/// <param name="TimeZoneId">شناسهٔ منطقهٔ زمانی برنامه.</param>
/// <param name="IsActive">آیا برنامه فعال است.</param>
/// <param name="Windows">پنجره‌های هفتگی، مرتب‌شده بر پایهٔ روز و زمان شروع.</param>
/// <param name="ConfiguredCapacityTotal">
/// مجموع ظرفیت پیکربندی‌شده؛ تهی یعنی هیچ پنجره‌ای ظرفیت ندارد. این عدد ظرفیت <em>اجراشده</em>
/// نیست و نباید به‌عنوان ظرفیت باقی‌مانده تفسیر شود.
/// </param>
/// <param name="CreatedAt">زمان ایجاد UTC.</param>
/// <param name="UpdatedAt">زمان آخرین ویرایش UTC.</param>
public sealed record ShippingOperatingScheduleAdminItem(
    Guid ScheduleId,
    string ServiceCode,
    string? OptionCode,
    string TimeZoneId,
    bool IsActive,
    IReadOnlyList<ShippingOperatingWindowAdminItem> Windows,
    int? ConfiguredCapacityTotal,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// نمای کامل برنامه‌های عملیاتی یک کد خدمت. لیست خالی یک پاسخ واقعی است: یعنی برای این خدمت هیچ
/// برنامهٔ صریحی ثبت نشده و هیچ برنامهٔ پیش‌فرضی ساخته نمی‌شود.
/// </summary>
/// <param name="ServiceCode">کد خدمت این نما.</param>
/// <param name="Schedules">برنامه‌های ثبت‌شدهٔ این خدمت (سطح خدمت و گزینه‌ها).</param>
public sealed record ShippingOperatingSchedulesView(
    string ServiceCode,
    IReadOnlyList<ShippingOperatingScheduleAdminItem> Schedules);
