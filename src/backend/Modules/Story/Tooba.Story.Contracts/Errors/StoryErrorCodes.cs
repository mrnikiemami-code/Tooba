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
}
