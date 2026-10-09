namespace Tooba.Support.Application.Validation;

/// <summary>
/// کدهای ماشینی پایدار اعتبارسنجی شکلِ transport برای Support.
/// <para>
/// این کدها هویتِ transport‌اند و هرگز بومی‌سازی نمی‌شوند و هرگز معنای کسب‌وکار را رمزگذاری
/// نمی‌کنند. عمداً به‌عنوان توصیف‌گر کاتالوگ ثبت نمی‌شوند: پایپ‌لاین canonical
/// <c>ValidationBehavior</c> آن‌ها را داخل پوشش <c>validation.failed</c> و در نقشهٔ
/// <c>validationErrors</c> به‌ازای هر property به کلاینت می‌رساند.
/// </para>
/// <para>
/// فقط شکلِ transport این‌جا اعتبارسنجی می‌شود؛ هر قاعدهٔ کسب‌وکار/دامنه (مالکیت، انتقال وضعیت،
/// idempotency واقعی، وجود رکورد) در Application/Domain می‌ماند و هرگز در Validator تکرار نمی‌شود.
/// </para>
/// </summary>
public static class SupportValidationCodes
{
    /// <summary>مقدار وضعیت تیکت باید یک وضعیت شناخته‌شده باشد.</summary>
    public const string StatusInvalid = "support.validation.status_invalid";

    /// <summary>مقدار اولویت تیکت باید یک اولویت شناخته‌شده باشد.</summary>
    public const string PriorityInvalid = "support.validation.priority_invalid";

    /// <summary>مقدار دستهٔ تیکت باید یک دستهٔ شناخته‌شده باشد.</summary>
    public const string CategoryInvalid = "support.validation.category_invalid";

    /// <summary>مقدار نوع درخواست‌کننده باید یک نوع شناخته‌شده باشد.</summary>
    public const string RequesterKindInvalid = "support.validation.requester_kind_invalid";

    /// <summary>موضوع تیکت الزامی است.</summary>
    public const string SubjectRequired = "support.validation.subject_required";

    /// <summary>موضوع تیکت از طول مجاز بیشتر است.</summary>
    public const string SubjectTooLong = "support.validation.subject_too_long";

    /// <summary>دستهٔ تیکت الزامی است.</summary>
    public const string CategoryRequired = "support.validation.category_required";

    /// <summary>بدنهٔ پیام الزامی است.</summary>
    public const string BodyRequired = "support.validation.body_required";

    /// <summary>بدنهٔ پیام از طول مجاز بیشتر است.</summary>
    public const string BodyTooLong = "support.validation.body_too_long";

    /// <summary>کلید idempotency از طول مجاز بیشتر است.</summary>
    public const string IdempotencyKeyTooLong = "support.validation.idempotency_key_too_long";

    /// <summary>نوع موجودیت مرتبط از طول مجاز بیشتر است.</summary>
    public const string RelatedEntityTypeTooLong = "support.validation.related_entity_type_too_long";

    /// <summary>شناسهٔ موجودیت مرتبط وقتی نوع آن ارسال شده باشد الزامی است.</summary>
    public const string RelatedEntityIdRequired = "support.validation.related_entity_id_required";

    /// <summary>شناسهٔ موجودیت مرتبط بدون نوع آن مجاز نیست.</summary>
    public const string RelatedEntityTypeRequired = "support.validation.related_entity_type_required";

    /// <summary>طول عبارت جست‌وجوی فهرست مدیر از حد مجاز بیشتر است.</summary>
    public const string SearchTooLong = "support.validation.search_too_long";
}
