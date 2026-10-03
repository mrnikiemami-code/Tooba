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

/// <summary>Directory create payload (not MediatR).</summary>
public sealed record CreateLanguageSpec(
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

/// <summary>Directory update payload (not MediatR).</summary>
public sealed record UpdateLanguageSpec(
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

/// <summary>Directory patch payload (not MediatR).</summary>
public sealed record PatchLanguageSpec(
    bool? IsActive,
    bool? IsDefault,
    int? SortOrder);

/// <summary>Admin HTTP response shape for language registry.</summary>
public sealed record LanguageAdminResponse(
    Guid LanguageId,
    string Code,
    string UrlPrefix,
    string DisplayName,
    string NativeName,
    string Direction,
    string Culture,
    string CalendarDisplay,
    bool Active,
    bool IsDefault,
    int SortOrder,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsReferenced,
    bool CanEditCode,
    bool CanEditUrlPrefix);
