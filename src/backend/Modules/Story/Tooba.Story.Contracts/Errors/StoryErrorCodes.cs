namespace Tooba.Story.Contracts.Errors;

/// <summary>
/// کدهای خطای معنایی پایدارِ مالکیت‌شدهٔ ماژول Story.
/// این کدها بخشی از قرارداد بیرونی ماژول‌اند؛ مصرف‌کنندهٔ بین‌ماژولی و کلاینت فقط به همین رشته‌ها تکیه می‌کند،
/// بنابراین مقدار آن‌ها هرگز نباید تغییر یا بازاستفاده شود. متن قابل‌نمایش توسط مجموعهٔ منابع
/// <c>StoryErrors.resx</c> تأمین می‌شود و این کدها تنها شناسهٔ ماشینی‌اند.
/// </summary>
public static class StoryErrorCodes
{
    /// <summary>استوری درخواستی در scope جاری یافت نشد.</summary>
    public const string Missing = "story.missing";

    /// <summary>مقصد CTA استوری توسط قواعد امنیتی/دامنه رد شده است.</summary>
    public const string CtaRejected = "story.cta.rejected";

    /// <summary>تغییر درخواستی توسط قواعد دامنه (وضعیت یا مالکیت) رد شده است.</summary>
    public const string MutationRejected = "story.mutation.rejected";

    /// <summary>Tenant مؤثر برای Story قابل تشخیص نیست؛ این حالت fail-closed است و نباید با مقدار پیش‌فرض جبران شود.</summary>
    public const string TenantMissing = "story.tenant.missing";

    /// <summary>مقدار ورودی وضعیت بازبینی برای پارس به enum دامنه نامعتبر است.</summary>
    public const string ReviewStatusInvalid = "story.reviewStatus.invalid";

    /// <summary>فروشگاه انتخاب‌شده برای ایجاد از طرف ادمین نامعتبر/غیرمجاز است.</summary>
    public const string ShopInvalid = "story.shop.invalid";

    /// <summary>محتوای قابل‌پخش برای ارسال/انتشار آماده نیست.</summary>
    public const string ContentNotReady = "story.content.notReady";

    /// <summary>نسخهٔ بازبینی دیده‌شده الزامی است.</summary>
    public const string VersionRequired = "story.version.required";

    /// <summary>نسخهٔ بازبینی دیده‌شده کهنه است.</summary>
    public const string VersionStale = "story.version.stale";

    /// <summary>زمان‌بندی انتشار نامعتبر است.</summary>
    public const string ScheduleInvalid = "story.schedule.invalid";

    /// <summary>رسانه برای این مالک/فروشگاه مجاز نیست.</summary>
    public const string MediaForbidden = "story.media.forbidden";

    /// <summary>هویت رخداد فرمان با نتیجهٔ ذخیره‌شده در تضاد است.</summary>
    public const string OccurrenceConflict = "story.occurrence.conflict";
}
