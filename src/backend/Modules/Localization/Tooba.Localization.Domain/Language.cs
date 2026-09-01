using Tooba.BuildingBlocks;

namespace Tooba.Localization.Domain;

/// <summary>جهت نوشتار زبان.</summary>
public enum LanguageDirection
{
    /// <summary>راست‌به‌چپ.</summary>
    Rtl = 0,
    /// <summary>چپ‌به‌راست.</summary>
    Ltr = 1,
}

/// <summary>سیاست نمایش تقویم — فقط UI.</summary>
public enum LanguageCalendarPolicy
{
    /// <summary>نمایش جلالی.</summary>
    Jalali = 0,
    /// <summary>نمایش میلادی.</summary>
    Gregorian = 1,
}

/// <summary>زبان/محلیهٔ کانونی پایدار برای Content و ویترین.</summary>
public sealed class Language
{
    public const int CodeMaxLength = 16;
    public const int UrlPrefixMaxLength = 8;
    public const int DisplayNameMaxLength = 100;
    public const int NativeNameMaxLength = 100;
    public const int CultureMaxLength = 16;

    private Language() { }

    public Guid LanguageId { get; init; }
    public string Code { get; private set; } = string.Empty;
    public string UrlPrefix { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string NativeName { get; private set; } = string.Empty;
    public LanguageDirection Direction { get; private set; }
    public string Culture { get; private set; } = string.Empty;
    public LanguageCalendarPolicy CalendarDisplay { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDefault { get; private set; }
    public int SortOrder { get; private set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static Language Create(
        string code,
        string urlPrefix,
        string displayName,
        string nativeName,
        LanguageDirection direction,
        string culture,
        LanguageCalendarPolicy calendarDisplay,
        bool isActive,
        bool isDefault,
        int sortOrder,
        DateTimeOffset now)
    {
        ValidateIdentity(code, urlPrefix, displayName, nativeName, culture);
        if (!isActive && isDefault)
        {
            throw new InvalidOperationException(LanguageErrorCodes.DefaultMustBeActive);
        }

        return new Language
        {
            LanguageId = UuidV7.New(),
            Code = NormalizeCode(code),
            UrlPrefix = NormalizeUrlPrefix(urlPrefix),
            DisplayName = displayName.Trim(),
            NativeName = nativeName.Trim(),
            Direction = direction,
            Culture = culture.Trim(),
            CalendarDisplay = calendarDisplay,
            IsActive = isActive,
            IsDefault = isDefault,
            SortOrder = sortOrder,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void UpdateMutableFields(
        string displayName,
        string nativeName,
        LanguageDirection direction,
        string culture,
        LanguageCalendarPolicy calendarDisplay,
        bool isActive,
        bool isDefault,
        int sortOrder,
        DateTimeOffset now)
    {
        ValidateIdentity(Code, UrlPrefix, displayName, nativeName, culture);
        if (!isActive && isDefault)
        {
            throw new InvalidOperationException(LanguageErrorCodes.DefaultMustBeActive);
        }

        DisplayName = displayName.Trim();
        NativeName = nativeName.Trim();
        Direction = direction;
        Culture = culture.Trim();
        CalendarDisplay = calendarDisplay;
        IsActive = isActive;
        IsDefault = isDefault;
        SortOrder = sortOrder;
        UpdatedAt = now;
    }

    public void SetDefault(bool isDefault, DateTimeOffset now)
    {
        if (isDefault && !IsActive)
        {
            throw new InvalidOperationException(LanguageErrorCodes.DefaultMustBeActive);
        }

        IsDefault = isDefault;
        UpdatedAt = now;
    }

    public void SetActive(bool isActive, DateTimeOffset now)
    {
        if (!isActive && IsDefault)
        {
            throw new InvalidOperationException(LanguageErrorCodes.DefaultMustBeActive);
        }

        IsActive = isActive;
        UpdatedAt = now;
    }

    public static string NormalizeCode(string code) => code.Trim();

    public static string NormalizeUrlPrefix(string urlPrefix) => urlPrefix.Trim().ToLowerInvariant();

    private static void ValidateIdentity(
        string code,
        string urlPrefix,
        string displayName,
        string nativeName,
        string culture)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length > CodeMaxLength)
        {
            throw new InvalidOperationException(LanguageErrorCodes.InvalidCode);
        }

        if (string.IsNullOrWhiteSpace(urlPrefix) || urlPrefix.Trim().Length > UrlPrefixMaxLength)
        {
            throw new InvalidOperationException(LanguageErrorCodes.InvalidUrlPrefix);
        }

        if (string.IsNullOrWhiteSpace(displayName) || displayName.Trim().Length > DisplayNameMaxLength)
        {
            throw new InvalidOperationException(LanguageErrorCodes.InvalidDisplayName);
        }

        if (string.IsNullOrWhiteSpace(nativeName) || nativeName.Trim().Length > NativeNameMaxLength)
        {
            throw new InvalidOperationException(LanguageErrorCodes.InvalidNativeName);
        }

        if (string.IsNullOrWhiteSpace(culture) || culture.Trim().Length > CultureMaxLength)
        {
            throw new InvalidOperationException(LanguageErrorCodes.InvalidCulture);
        }
    }
}

/// <summary>کدهای خطای پایدار زبان.</summary>
public static class LanguageErrorCodes
{
    public const string NotFound = "localization.language.not_found";
    public const string CodeDuplicate = "localization.language.code_duplicate";
    public const string UrlPrefixDuplicate = "localization.language.url_prefix_duplicate";
    public const string DefaultMustBeActive = "localization.language.default_must_be_active";
    public const string AtLeastOneActive = "localization.language.at_least_one_active";
    public const string ExactlyOneDefault = "localization.language.exactly_one_default";
    public const string CodeImmutable = "localization.language.code_immutable";
    public const string UrlPrefixImmutable = "localization.language.url_prefix_immutable";
    public const string Referenced = "localization.language.referenced";
    public const string InvalidCode = "localization.language.invalid_code";
    public const string InvalidUrlPrefix = "localization.language.invalid_url_prefix";
    public const string InvalidDisplayName = "localization.language.invalid_display_name";
    public const string InvalidNativeName = "localization.language.invalid_native_name";
    public const string InvalidCulture = "localization.language.invalid_culture";
    public const string InvalidDirection = "localization.language.invalid_direction";
    public const string InvalidCalendar = "localization.language.invalid_calendar";
    public const string Inactive = "localization.language.inactive";
}
