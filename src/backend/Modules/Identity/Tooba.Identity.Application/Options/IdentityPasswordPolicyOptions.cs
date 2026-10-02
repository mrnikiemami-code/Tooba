namespace Tooba.Identity.Application.Options;

/// <summary>
/// سیاست رمز قابل پیکربندی. قانون تجاری نهایی در دامنه hard-code نمی‌شود.
/// </summary>
public sealed class IdentityPasswordPolicyOptions
{
    /// <summary>
    /// حداقل طول فعلی برای ایمنی پایه؛ پیچیدگی محصول بعداً اضافه می‌شود.
    /// </summary>
    public int MinimumLength { get; set; } = 10;

    /// <summary>
    /// اگر true باشد باید حرف و رقم داشته باشد. پیش‌فرض خاموش است تا سیاست محصول جدا بماند.
    /// </summary>
    public bool RequireLetterAndDigit { get; set; }

    /// <summary>
    /// درز برای بررسی رمز افشاشده در آینده؛ این تسک اجرا نمی‌کند.
    /// </summary>
    public bool EnableBreachedPasswordCheckLater { get; init; }

    /// <summary>
    /// درز تاریخچهٔ رمز؛ این تسک ذخیرهٔ history ندارد.
    /// </summary>
    public bool EnablePasswordHistoryLater { get; init; }
}
