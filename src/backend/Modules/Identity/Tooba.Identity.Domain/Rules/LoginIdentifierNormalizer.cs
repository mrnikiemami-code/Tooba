using Tooba.BuildingBlocks;
using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Auth;
using Tooba.Identity.Contracts.Problems;

namespace Tooba.Identity.Domain.Rules;

/// <summary>
/// نرمال‌سازی نوع‌ویژه برای جستجوی پایدار شناسه. یک Lowercase عمومی روی همهٔ انواع اعمال نمی‌شود.
/// </summary>
public static class LoginIdentifierNormalizer
{
    /// <summary>
    /// مقدار نمایشی را trim می‌کند و مقدار نرمال را بر اساس گونه می‌سازد.
    /// </summary>
    /// <param name="kind">گونهٔ شناسه؛ قوانین جدا دارند.</param>
    /// <param name="rawValue">ورودی کاربر؛ نباید لاگ شود اگر محرمانه تلقی شود.</param>
    /// <returns>جفت نمایش و کلید نرمال.</returns>
    /// <exception cref="ContractOperationException">وقتی مقدار پس از نرمال تهی است؛ Code همان کد ماشین‌پایدار Identity است.</exception>
    public static (string Display, string Normalized) Normalize(LoginIdentifierKind kind, string rawValue)
    {
        ArgumentNullException.ThrowIfNull(rawValue);
        var display = rawValue.Trim();
        if (display.Length == 0)
        {
            throw Invalid();
        }

        var normalized = kind switch
        {
            LoginIdentifierKind.Email => NormalizeEmail(display),
            LoginIdentifierKind.Username => NormalizeUsername(display),
            LoginIdentifierKind.Phone => NormalizePhone(display),
            LoginIdentifierKind.NationalId => NormalizeNationalId(display),
            LoginIdentifierKind.ExternalProvider => display.Trim(),
            _ => throw Invalid(),
        };

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw Invalid();
        }

        return (display, normalized);
    }

    /// <summary>
    /// ایمیل: پیرایش و lowercase اینورینت؛ نقطه/plus collapsing انجام نمی‌شود تا رفتار ارائه‌دهنده حدس زده نشود.
    /// </summary>
    public static string NormalizeEmail(string display)
    {
        var trimmed = display.Trim();
        var at = trimmed.LastIndexOf('@');
        if (at <= 0 || at == trimmed.Length - 1)
        {
            throw Invalid();
        }

        return trimmed.ToLowerInvariant();
    }

    /// <summary>
    /// نام کاربری: پیرایش و lowercase اینورینت برای یکتایی؛ فاصلهٔ داخلی حذف نمی‌شود تا قانون محصول جدا بماند.
    /// </summary>
    public static string NormalizeUsername(string display) => display.Trim().ToLowerInvariant();

    /// <summary>
    /// تلفن: فقط ارقام و حداکثر یک + ابتدایی؛ پیش‌فرض کشور ایران hard-code نمی‌شود.
    /// </summary>
    public static string NormalizePhone(string display)
    {
        var trimmed = display.Trim();
        var chars = new List<char>(trimmed.Length);
        var i = 0;
        if (trimmed.StartsWith('+'))
        {
            chars.Add('+');
            i = 1;
        }

        for (; i < trimmed.Length; i++)
        {
            if (char.IsAsciiDigit(trimmed[i]))
            {
                chars.Add(trimmed[i]);
            }
        }

        if (chars.Count == 0 || (chars.Count == 1 && chars[0] == '+'))
        {
            throw Invalid();
        }

        return new string(chars.ToArray());
    }

    /// <summary>
    /// شناسهٔ ملی آینده: پیرایش و حذف جداکننده؛ قانون رقم ایران اینجا قفل نمی‌شود.
    /// </summary>
    public static string NormalizeNationalId(string display)
    {
        var chars = display.Trim().Where(char.IsLetterOrDigit).ToArray();
        if (chars.Length == 0)
        {
            throw Invalid();
        }

        return new string(chars).ToUpperInvariant();
    }

    /// <summary>
    /// خرابی قالب شناسه با کد ماشین‌پایدار؛ متن پیام کاربرپسند نیست و از مسیر localization می‌آید.
    /// </summary>
    private static ContractOperationException Invalid() =>
        new(IdentityErrorCodes.ValidationFailed);
}
