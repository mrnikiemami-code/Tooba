using Tooba.BuildingBlocks;
using Tooba.Localization.Application.Models;
using Tooba.Localization.Contracts.Errors;
using Tooba.Localization.Domain.Aggregates;
using Tooba.Localization.Domain.Enums;

namespace Tooba.Localization.Application.Composition;

/// <summary>Mapping helpers between Language aggregate and Application snapshots.</summary>
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

    public static LanguageAdminResponse ToAdminResponse(LanguageAdminSnapshot row) =>
        ToAdminResponse(row.Snapshot, row.IsReferenced, row.CanEditCode, row.CanEditUrlPrefix);

    public static LanguageAdminResponse ToAdminResponse(
        LanguageSnapshot row,
        bool isReferenced,
        bool? canEditCode = null,
        bool? canEditUrlPrefix = null) => new(
        row.LanguageId,
        row.Code,
        row.UrlPrefix,
        row.DisplayName,
        row.NativeName,
        row.Direction,
        row.Culture,
        row.CalendarDisplay,
        row.IsActive,
        row.IsDefault,
        row.SortOrder,
        row.CreatedAt,
        row.UpdatedAt,
        isReferenced,
        canEditCode ?? !isReferenced,
        canEditUrlPrefix ?? !isReferenced);

    public static LanguageDirection ParseDirection(string? raw)
    {
        if (string.Equals(raw, "rtl", StringComparison.OrdinalIgnoreCase)
            || string.Equals(raw, "RTL", StringComparison.OrdinalIgnoreCase))
            return LanguageDirection.Rtl;

        if (string.Equals(raw, "ltr", StringComparison.OrdinalIgnoreCase)
            || string.Equals(raw, "LTR", StringComparison.OrdinalIgnoreCase))
            return LanguageDirection.Ltr;

        throw new SemanticException(new SemanticError(LanguageErrorCodes.InvalidDirection));
    }

    public static LanguageCalendarPolicy ParseCalendar(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)
            || raw.Equals("jalali", StringComparison.OrdinalIgnoreCase)
            || raw.Equals("Jalali", StringComparison.OrdinalIgnoreCase))
            return LanguageCalendarPolicy.Jalali;

        if (raw.Equals("gregorian", StringComparison.OrdinalIgnoreCase)
            || raw.Equals("Gregorian", StringComparison.OrdinalIgnoreCase))
            return LanguageCalendarPolicy.Gregorian;

        throw new SemanticException(new SemanticError(LanguageErrorCodes.InvalidCalendar));
    }
}
