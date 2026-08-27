namespace Tooba.UserPreference.Domain;

/// <summary>
/// ترجیح locale کاربر. فقط <c>fa</c> و <c>en</c> مجازند؛ theme/notification اینجا نیست.
/// </summary>
public sealed class UserPreference
{
    /// <summary>locale فارسی.</summary>
    public const string LocaleFa = "fa";

    /// <summary>locale انگلیسی.</summary>
    public const string LocaleEn = "en";

    /// <summary>حداکثر طول کد locale.</summary>
    public const int LocaleMaxLength = 8;

    private UserPreference()
    {
    }

    /// <summary>مالک سرورمحور؛ کلید اصلی و هرگز از بدنهٔ HTTP پذیرفته نمی‌شود.</summary>
    public Guid OwnerUserId { get; init; }

    /// <summary>کد locale پایدار (<c>fa</c> یا <c>en</c>).</summary>
    public string Locale { get; private set; } = LocaleFa;

    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان آخرین ویرایش UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>ترجیح جدید برای Actor مشخص می‌سازد.</summary>
    public static UserPreference Create(Guid ownerUserId, string locale, DateTimeOffset now)
    {
        if (ownerUserId == Guid.Empty)
        {
            throw new InvalidOperationException("Actor معتبر الزامی است.");
        }

        var preference = new UserPreference
        {
            OwnerUserId = ownerUserId,
            CreatedAt = now,
            UpdatedAt = now,
        };
        preference.ApplyLocale(locale, now);
        return preference;
    }

    /// <summary>locale مجاز را به‌روز می‌کند.</summary>
    public void Update(string locale, DateTimeOffset now) => ApplyLocale(locale, now);

    private void ApplyLocale(string locale, DateTimeOffset now)
    {
        Locale = NormalizeLocale(locale);
        UpdatedAt = now;
    }

    /// <summary>کد locale را نرمال و اعتبارسنجی می‌کند.</summary>
    public static string NormalizeLocale(string? locale)
    {
        var trimmed = locale?.Trim().ToLowerInvariant() ?? string.Empty;
        if (trimmed is not (LocaleFa or LocaleEn))
        {
            throw new InvalidOperationException("فقط localeهای fa و en مجاز هستند.");
        }

        return trimmed;
    }
}
