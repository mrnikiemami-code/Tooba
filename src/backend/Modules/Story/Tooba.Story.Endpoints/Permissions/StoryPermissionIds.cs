namespace Tooba.Story.Endpoints.Permissions;

/// <summary>
/// شناسه‌های پایدار capability که کاتالوگ Access Control برای Story ثبت می‌کند.
/// این‌ها تاکسونومی تازه نیستند؛ همان شناسه‌های موجود PermissionCatalog هستند و مرز HTTP فقط
/// آن‌ها را به authorizer مالک می‌دهد تا دسترسی سرور با همان اختیاری که منو را نشان می‌دهد یکی باشد.
/// </summary>
public static class StoryPermissionIds
{
    /// <summary>مشاهدهٔ فهرست/جزئیات/گزینه‌های استوری.</summary>
    public const string View = "story.view";

    /// <summary>ایجاد پیش‌نویس تازه.</summary>
    public const string Create = "story.create";

    /// <summary>ویرایش متادیتا/آیتم‌ها و حذف.</summary>
    public const string Edit = "story.edit";

    /// <summary>ارسال برای بررسی.</summary>
    public const string Submit = "story.submit";

    /// <summary>تأیید استوری.</summary>
    public const string Approve = "story.approve";

    /// <summary>رد استوری.</summary>
    public const string Reject = "story.reject";

    /// <summary>انتشار/زمان‌بندی/فعال‌سازی/توقف.</summary>
    public const string Publish = "story.publish";
}
