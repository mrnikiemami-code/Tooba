using Tooba.BuildingBlocks;
using Tooba.Localization.Contracts.Errors;

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
            throw new SemanticException(new SemanticError(LanguageErrorCodes.DefaultMustBeActive));
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
            throw new SemanticException(new SemanticError(LanguageErrorCodes.DefaultMustBeActive));
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

    public void UpdateIdentityFields(string code, string urlPrefix, DateTimeOffset now)
    {
        ValidateIdentity(code, urlPrefix, DisplayName, NativeName, Culture);
        Code = NormalizeCode(code);
        UrlPrefix = NormalizeUrlPrefix(urlPrefix);
        UpdatedAt = now;
    }

    public void SetDefault(bool isDefault, DateTimeOffset now)
    {
        if (isDefault && !IsActive)
        {
            throw new SemanticException(new SemanticError(LanguageErrorCodes.DefaultMustBeActive));
        }

        IsDefault = isDefault;
        UpdatedAt = now;
    }

    public void SetActive(bool isActive, DateTimeOffset now)
    {
        if (!isActive && IsDefault)
        {
            throw new SemanticException(new SemanticError(LanguageErrorCodes.DefaultMustBeActive));
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
            throw new SemanticException(new SemanticError(LanguageErrorCodes.InvalidCode));
        }

        if (string.IsNullOrWhiteSpace(urlPrefix) || urlPrefix.Trim().Length > UrlPrefixMaxLength)
        {
            throw new SemanticException(new SemanticError(LanguageErrorCodes.InvalidUrlPrefix));
        }

        if (string.IsNullOrWhiteSpace(displayName) || displayName.Trim().Length > DisplayNameMaxLength)
        {
            throw new SemanticException(new SemanticError(LanguageErrorCodes.InvalidDisplayName));
        }

        if (string.IsNullOrWhiteSpace(nativeName) || nativeName.Trim().Length > NativeNameMaxLength)
        {
            throw new SemanticException(new SemanticError(LanguageErrorCodes.InvalidNativeName));
        }

        if (string.IsNullOrWhiteSpace(culture) || culture.Trim().Length > CultureMaxLength)
        {
            throw new SemanticException(new SemanticError(LanguageErrorCodes.InvalidCulture));
        }
    }
}
