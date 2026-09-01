using Microsoft.EntityFrameworkCore;
using Tooba.Localization.Application;
using Tooba.Localization.Domain;
using Tooba.Localization.Infrastructure.Persistence;

namespace Tooba.Localization.Infrastructure;

/// <summary>دایرکتوری DB-backed زبان با invariantهای کانونی.</summary>
public sealed class LanguageDirectory : ILanguageDirectory
{
    private readonly LocalizationDbContext _db;
    private readonly ILanguageReferenceGuard _referenceGuard;

    public LanguageDirectory(LocalizationDbContext db, ILanguageReferenceGuard referenceGuard)
    {
        _db = db;
        _referenceGuard = referenceGuard;
    }

    public async Task<IReadOnlyList<LanguageSnapshot>> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await _db.Languages.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);
        return rows.Select(LanguageMappings.ToSnapshot).ToList();
    }

    public async Task<IReadOnlyList<LanguageAdminSnapshot>> ListAdminAsync(CancellationToken cancellationToken)
    {
        var rows = await ListAsync(cancellationToken);
        var result = new List<LanguageAdminSnapshot>(rows.Count);
        foreach (var row in rows)
        {
            var referenced = await _referenceGuard.IsReferencedAsync(row.Code, cancellationToken);
            result.Add(LanguageMappings.ToAdminSnapshot(row, referenced));
        }

        return result;
    }

    public async Task<LanguageAdminSnapshot?> GetAdminByCodeAsync(string code, CancellationToken cancellationToken)
    {
        var row = await GetByCodeAsync(code, cancellationToken);
        if (row is null)
        {
            return null;
        }

        var referenced = await _referenceGuard.IsReferencedAsync(row.Code, cancellationToken);
        return LanguageMappings.ToAdminSnapshot(row, referenced);
    }

    public async Task<LanguageSnapshot?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        var row = await FindByCodeAsync(code, cancellationToken);
        return row is null ? null : LanguageMappings.ToSnapshot(row);
    }

    public async Task EnsureActiveLanguageCodeAsync(string code, CancellationToken cancellationToken)
    {
        var normalized = Language.NormalizeCode(code);
        var row = await _db.Languages.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == normalized, cancellationToken);
        if (row is null || !row.IsActive)
        {
            throw new InvalidOperationException(LanguageErrorCodes.Inactive);
        }
    }

    public async Task<LanguageSnapshot> CreateAsync(CreateLanguageCommand command, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var code = Language.NormalizeCode(command.Code);
        var urlPrefix = Language.NormalizeUrlPrefix(command.UrlPrefix);
        if (await _db.Languages.AnyAsync(x => x.Code == code, cancellationToken))
        {
            throw new InvalidOperationException(LanguageErrorCodes.CodeDuplicate);
        }

        if (await _db.Languages.AnyAsync(x => x.UrlPrefix == urlPrefix, cancellationToken))
        {
            throw new InvalidOperationException(LanguageErrorCodes.UrlPrefixDuplicate);
        }

        var language = Language.Create(
            code,
            urlPrefix,
            command.DisplayName,
            command.NativeName,
            LanguageMappings.ParseDirection(command.Direction),
            command.Culture,
            LanguageMappings.ParseCalendar(command.CalendarDisplay),
            command.IsActive,
            command.IsDefault,
            command.SortOrder,
            now);

        if (command.IsDefault)
        {
            await ClearDefaultFlagsExceptAsync(language.LanguageId, cancellationToken);
        }

        _db.Languages.Add(language);
        await SaveWithInvariantChecksAsync(cancellationToken);
        return LanguageMappings.ToSnapshot(language);
    }

    public async Task<LanguageSnapshot> UpdateAsync(
        string code,
        UpdateLanguageCommand command,
        CancellationToken cancellationToken)
    {
        var language = await FindByCodeTrackedAsync(code, cancellationToken)
            ?? throw new InvalidOperationException(LanguageErrorCodes.NotFound);
        var referenced = await _referenceGuard.IsReferencedAsync(language.Code, cancellationToken);
        var nextCode = string.IsNullOrWhiteSpace(command.Code)
            ? language.Code
            : Language.NormalizeCode(command.Code);
        var nextUrlPrefix = string.IsNullOrWhiteSpace(command.UrlPrefix)
            ? language.UrlPrefix
            : Language.NormalizeUrlPrefix(command.UrlPrefix);

        if (referenced)
        {
            if (!string.Equals(nextCode, language.Code, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(LanguageErrorCodes.CodeInUse);
            }

            if (!string.Equals(nextUrlPrefix, language.UrlPrefix, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(LanguageErrorCodes.UrlPrefixInUse);
            }
        }
        else if (!string.Equals(nextCode, language.Code, StringComparison.Ordinal)
            || !string.Equals(nextUrlPrefix, language.UrlPrefix, StringComparison.Ordinal))
        {
            if (!string.Equals(nextCode, language.Code, StringComparison.Ordinal)
                && await _db.Languages.AnyAsync(x => x.Code == nextCode && x.LanguageId != language.LanguageId, cancellationToken))
            {
                throw new InvalidOperationException(LanguageErrorCodes.CodeDuplicate);
            }

            if (!string.Equals(nextUrlPrefix, language.UrlPrefix, StringComparison.Ordinal)
                && await _db.Languages.AnyAsync(x => x.UrlPrefix == nextUrlPrefix && x.LanguageId != language.LanguageId, cancellationToken))
            {
                throw new InvalidOperationException(LanguageErrorCodes.UrlPrefixDuplicate);
            }

            var nowIdentity = DateTimeOffset.UtcNow;
            language.UpdateIdentityFields(nextCode, nextUrlPrefix, nowIdentity);
        }

        var now = DateTimeOffset.UtcNow;
        language.UpdateMutableFields(
            command.DisplayName,
            command.NativeName,
            LanguageMappings.ParseDirection(command.Direction),
            command.Culture,
            LanguageMappings.ParseCalendar(command.CalendarDisplay),
            command.IsActive,
            command.IsDefault,
            command.SortOrder,
            now);

        if (command.IsDefault)
        {
            await ClearDefaultFlagsExceptAsync(language.LanguageId, cancellationToken);
        }

        await SaveWithInvariantChecksAsync(cancellationToken);
        return LanguageMappings.ToSnapshot(language);
    }

    public async Task<LanguageSnapshot> PatchAsync(string code, PatchLanguageCommand command, CancellationToken cancellationToken)
    {
        var language = await FindByCodeTrackedAsync(code, cancellationToken)
            ?? throw new InvalidOperationException(LanguageErrorCodes.NotFound);
        var now = DateTimeOffset.UtcNow;
        var nextActive = command.IsActive ?? language.IsActive;
        var nextDefault = command.IsDefault ?? language.IsDefault;
        var nextSort = command.SortOrder ?? language.SortOrder;
        language.UpdateMutableFields(
            language.DisplayName,
            language.NativeName,
            language.Direction,
            language.Culture,
            language.CalendarDisplay,
            nextActive,
            nextDefault,
            nextSort,
            now);

        if (command.IsDefault == true)
        {
            await ClearDefaultFlagsExceptAsync(language.LanguageId, cancellationToken);
        }

        await SaveWithInvariantChecksAsync(cancellationToken);
        return LanguageMappings.ToSnapshot(language);
    }

    public async Task BootstrapAsync(CancellationToken cancellationToken)
    {
        if (await _db.Languages.AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        _db.Languages.AddRange(
            Language.Create("fa-IR", "fa", "فارسی", "فارسی", LanguageDirection.Rtl, "fa-IR", LanguageCalendarPolicy.Jalali, true, true, 0, now),
            Language.Create("en-US", "en", "English", "English", LanguageDirection.Ltr, "en-US", LanguageCalendarPolicy.Gregorian, true, false, 1, now));
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Language?> FindByCodeAsync(string code, CancellationToken cancellationToken)
    {
        var normalized = Language.NormalizeCode(code);
        return await _db.Languages.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == normalized, cancellationToken);
    }

    private async Task<Language?> FindByCodeTrackedAsync(string code, CancellationToken cancellationToken)
    {
        var normalized = Language.NormalizeCode(code);
        return await _db.Languages.FirstOrDefaultAsync(x => x.Code == normalized, cancellationToken);
    }

    private async Task ClearDefaultFlagsExceptAsync(Guid keepLanguageId, CancellationToken cancellationToken)
    {
        var others = await _db.Languages.Where(x => x.LanguageId != keepLanguageId && x.IsDefault).ToListAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        foreach (var row in others)
        {
            row.SetDefault(false, now);
        }
    }

    private async Task SaveWithInvariantChecksAsync(CancellationToken cancellationToken)
    {
        await _db.SaveChangesAsync(cancellationToken);
        var activeCount = await _db.Languages.CountAsync(x => x.IsActive, cancellationToken);
        if (activeCount == 0)
        {
            throw new InvalidOperationException(LanguageErrorCodes.AtLeastOneActive);
        }

        var defaultCount = await _db.Languages.CountAsync(
            x => x.IsActive && x.IsDefault,
            cancellationToken);
        if (defaultCount != 1)
        {
            throw new InvalidOperationException(LanguageErrorCodes.ExactlyOneDefault);
        }
    }
}
