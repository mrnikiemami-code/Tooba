using Tooba.Localization.Domain;

namespace Tooba.Localization.Application;

/// <summary>نمای API زبان پایدار.</summary>
public sealed record LanguageSnapshot(
    Guid LanguageId,
    string Code,
    string UrlPrefix,
    string DisplayName,
    string NativeName,
    string Direction,
    string Culture,
    string CalendarDisplay,
    bool IsActive,
    bool IsDefault,
    int SortOrder,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>نمای Admin زبان همراه با قابلیت‌های ویرایش هویت.</summary>
public sealed record LanguageAdminSnapshot(
    LanguageSnapshot Snapshot,
    bool IsReferenced,
    bool CanEditCode,
    bool CanEditUrlPrefix);

/// <summary>ایجاد زبان.</summary>
public sealed record CreateLanguageCommand(
    string Code,
    string UrlPrefix,
    string DisplayName,
    string NativeName,
    string Direction,
    string Culture,
    string CalendarDisplay,
    bool IsActive,
    bool IsDefault,
    int SortOrder);

/// <summary>به‌روزرسانی زبان — کد و UrlPrefix پس از ارجاع تغییر نمی‌کند.</summary>
public sealed record UpdateLanguageCommand(
    string? Code,
    string? UrlPrefix,
    string DisplayName,
    string NativeName,
    string Direction,
    string Culture,
    string CalendarDisplay,
    bool IsActive,
    bool IsDefault,
    int SortOrder);

/// <summary>به‌روزرسانی جزئی (سازگار با PATCH قدیمی).</summary>
public sealed record PatchLanguageCommand(
    bool? IsActive,
    bool? IsDefault,
    int? SortOrder);

/// <summary>دایرکتوری زبان پایدار.</summary>
public interface ILanguageDirectory
{
    Task<IReadOnlyList<LanguageSnapshot>> ListAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<LanguageAdminSnapshot>> ListAdminAsync(CancellationToken cancellationToken);
    Task<LanguageSnapshot?> GetByCodeAsync(string code, CancellationToken cancellationToken);
    Task<LanguageAdminSnapshot?> GetAdminByCodeAsync(string code, CancellationToken cancellationToken);
    Task EnsureActiveLanguageCodeAsync(string code, CancellationToken cancellationToken);
    Task<LanguageSnapshot> CreateAsync(CreateLanguageCommand command, CancellationToken cancellationToken);
    Task<LanguageSnapshot> UpdateAsync(string code, UpdateLanguageCommand command, CancellationToken cancellationToken);
    Task<LanguageSnapshot> PatchAsync(string code, PatchLanguageCommand command, CancellationToken cancellationToken);
    Task BootstrapAsync(CancellationToken cancellationToken);
}

/// <summary>بررسی ارجاع زبان در ماژول‌های دیگر.</summary>
public interface ILanguageReferenceGuard
{
    Task<bool> IsReferencedAsync(string languageCode, CancellationToken cancellationToken);
}

public static class LanguageMappings
{
    public static LanguageSnapshot ToSnapshot(Language language) => new(
        language.LanguageId,
        language.Code,
        language.UrlPrefix,
        language.DisplayName,
        language.NativeName,
        language.Direction == LanguageDirection.Rtl ? "rtl" : "ltr",
        language.Culture,
        language.CalendarDisplay == LanguageCalendarPolicy.Jalali ? "Jalali" : "Gregorian",
        language.IsActive,
        language.IsDefault,
        language.SortOrder,
        language.CreatedAt,
        language.UpdatedAt);

    public static LanguageAdminSnapshot ToAdminSnapshot(LanguageSnapshot snapshot, bool isReferenced) => new(
        snapshot,
        isReferenced,
        CanEditCode: !isReferenced,
        CanEditUrlPrefix: !isReferenced);

    public static LanguageDirection ParseDirection(string? raw)
    {
        if (string.Equals(raw, "rtl", StringComparison.OrdinalIgnoreCase)
            || string.Equals(raw, "RTL", StringComparison.OrdinalIgnoreCase))
        {
            return LanguageDirection.Rtl;
        }

        if (string.Equals(raw, "ltr", StringComparison.OrdinalIgnoreCase)
            || string.Equals(raw, "LTR", StringComparison.OrdinalIgnoreCase))
        {
            return LanguageDirection.Ltr;
        }

        throw new InvalidOperationException(LanguageErrorCodes.InvalidDirection);
    }

    public static LanguageCalendarPolicy ParseCalendar(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)
            || raw.Equals("jalali", StringComparison.OrdinalIgnoreCase)
            || raw.Equals("Jalali", StringComparison.OrdinalIgnoreCase))
        {
            return LanguageCalendarPolicy.Jalali;
        }

        if (raw.Equals("gregorian", StringComparison.OrdinalIgnoreCase)
            || raw.Equals("Gregorian", StringComparison.OrdinalIgnoreCase))
        {
            return LanguageCalendarPolicy.Gregorian;
        }

        throw new InvalidOperationException(LanguageErrorCodes.InvalidCalendar);
    }
}
