namespace Tooba.Localization.Application.Models;

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

/// <summary>ایجاد زبان (directory DTO; MediatR IRequest arrives in W3).</summary>
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
