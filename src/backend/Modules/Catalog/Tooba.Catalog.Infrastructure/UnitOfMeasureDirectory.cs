using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Units.Models;
using Tooba.Catalog.Application.Units.Ports;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;
using Tooba.Localization.Contracts;

namespace Tooba.Catalog.Infrastructure;

/// <summary>Catalog persistence for UnitOfMeasure admin read/write via Localization.Contracts.</summary>
public sealed class UnitOfMeasureDirectory : IUnitOfMeasureDirectory
{
    private readonly CatalogDbContext _catalog;
    private readonly IClock _clock;
    private readonly IIdGenerator _ids;
    private readonly ILanguageLookup _languages;

    /// <summary>Creates the directory.</summary>
    public UnitOfMeasureDirectory(
        CatalogDbContext catalog,
        IClock clock,
        IIdGenerator ids,
        ILanguageLookup languages)
    {
        _catalog = catalog;
        _clock = clock;
        _ids = ids;
        _languages = languages;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UnitOfMeasureListItem>> ListAsync(
        string? language,
        CancellationToken cancellationToken)
    {
        var langId = await ResolveLanguageIdAsync(language, cancellationToken);
        var units = await _catalog.UnitsOfMeasure.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);
        var ids = units.Select(x => x.UnitOfMeasureId).ToArray();
        var translations = await _catalog.UnitOfMeasureTranslations.AsNoTracking()
            .Where(x => ids.Contains(x.UnitOfMeasureId))
            .ToListAsync(cancellationToken);
        var referenced = await _catalog.Products.AsNoTracking()
            .Where(p => ids.Contains(p.UnitOfMeasureId))
            .Select(p => p.UnitOfMeasureId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var refSet = referenced.ToHashSet();
        var byUnit = translations.GroupBy(x => x.UnitOfMeasureId).ToDictionary(g => g.Key, g => g.ToList());
        return units.Select(u =>
        {
            byUnit.TryGetValue(u.UnitOfMeasureId, out var rows);
            var picked = rows?.FirstOrDefault(r => r.LanguageId == langId) ?? rows?.FirstOrDefault();
            return new UnitOfMeasureListItem(
                u.UnitOfMeasureId,
                u.Code,
                u.Dimension.ToString(),
                picked?.Name ?? u.Code,
                picked?.ShortName ?? u.Code,
                u.IsActive,
                u.SortOrder,
                refSet.Contains(u.UnitOfMeasureId));
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<Result<UnitOfMeasureDetail>> GetAsync(Guid unitId, CancellationToken cancellationToken)
    {
        var unit = await _catalog.UnitsOfMeasure.AsNoTracking()
            .SingleOrDefaultAsync(x => x.UnitOfMeasureId == unitId, cancellationToken);
        if (unit is null)
        {
            return Result.Failure<UnitOfMeasureDetail>(new SemanticError(CatalogErrorCodes.UnitMissing));
        }

        var translations = await _catalog.UnitOfMeasureTranslations.AsNoTracking()
            .Where(x => x.UnitOfMeasureId == unitId)
            .ToListAsync(cancellationToken);
        var referenced = await _catalog.Products.AsNoTracking()
            .AnyAsync(p => p.UnitOfMeasureId == unitId, cancellationToken);
        return Result.Success(new UnitOfMeasureDetail(
            unit.UnitOfMeasureId,
            unit.Code,
            unit.Dimension.ToString(),
            unit.IsActive,
            unit.SortOrder,
            referenced,
            translations.Select(t => new UnitOfMeasureTranslationWriteModel(t.LanguageId, t.Name, t.ShortName)).ToList()));
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> CreateAsync(UnitOfMeasureWriteModel model, CancellationToken cancellationToken)
    {
        if (!TryParseDimension(model.Dimension, out var dimension))
        {
            return Result.Failure<Guid>(new SemanticError(CatalogErrorCodes.UnitDimensionInvalid));
        }

        var uniqueness = await EnsureUniqueCodeAsync(model.Code.Trim().ToLowerInvariant(), null, cancellationToken);
        if (uniqueness.IsFailure)
        {
            return Result.Failure<Guid>(uniqueness.Errors);
        }

        var languages = await ValidateTranslationsAsync(model.Translations, cancellationToken);
        if (languages.IsFailure)
        {
            return Result.Failure<Guid>(languages.Errors);
        }

        var now = _clock.UtcNow;
        var unit = UnitOfMeasure.Create(_ids.NewId(), model.Code, dimension, model.IsActive, model.SortOrder, now);
        _catalog.UnitsOfMeasure.Add(unit);
        foreach (var t in model.Translations)
        {
            _catalog.UnitOfMeasureTranslations.Add(
                UnitOfMeasureTranslation.Create(unit.UnitOfMeasureId, t.LanguageId, t.Name, t.ShortName));
        }

        await _catalog.SaveChangesAsync(cancellationToken);
        return Result.Success(unit.UnitOfMeasureId);
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> UpdateAsync(
        Guid unitId,
        UnitOfMeasureWriteModel model,
        CancellationToken cancellationToken)
    {
        var unit = await _catalog.UnitsOfMeasure.SingleOrDefaultAsync(x => x.UnitOfMeasureId == unitId, cancellationToken);
        if (unit is null)
        {
            return Result.Failure<Guid>(new SemanticError(CatalogErrorCodes.UnitMissing));
        }

        if (!TryParseDimension(model.Dimension, out var dimension))
        {
            return Result.Failure<Guid>(new SemanticError(CatalogErrorCodes.UnitDimensionInvalid));
        }

        var code = model.Code.Trim().ToLowerInvariant();
        var uniqueness = await EnsureUniqueCodeAsync(code, unitId, cancellationToken);
        if (uniqueness.IsFailure)
        {
            return Result.Failure<Guid>(uniqueness.Errors);
        }

        var languages = await ValidateTranslationsAsync(model.Translations, cancellationToken);
        if (languages.IsFailure)
        {
            return Result.Failure<Guid>(languages.Errors);
        }

        var now = _clock.UtcNow;
        unit.Update(code, dimension, model.SortOrder, now);
        unit.SetActive(model.IsActive, now);
        var existing = await _catalog.UnitOfMeasureTranslations.Where(x => x.UnitOfMeasureId == unitId).ToListAsync(cancellationToken);
        foreach (var t in model.Translations)
        {
            var row = existing.FirstOrDefault(x => x.LanguageId == t.LanguageId);
            if (row is null)
            {
                _catalog.UnitOfMeasureTranslations.Add(
                    UnitOfMeasureTranslation.Create(unitId, t.LanguageId, t.Name, t.ShortName));
            }
            else
            {
                row.SetText(t.Name, t.ShortName);
            }
        }

        await _catalog.SaveChangesAsync(cancellationToken);
        return Result.Success(unit.UnitOfMeasureId);
    }

    /// <inheritdoc />
    public async Task<Result<UnitOfMeasureDeactivateResult>> DeactivateAsync(
        Guid unitId,
        CancellationToken cancellationToken)
    {
        var unit = await _catalog.UnitsOfMeasure.SingleOrDefaultAsync(x => x.UnitOfMeasureId == unitId, cancellationToken);
        if (unit is null)
        {
            return Result.Failure<UnitOfMeasureDeactivateResult>(new SemanticError(CatalogErrorCodes.UnitMissing));
        }

        unit.SetActive(false, _clock.UtcNow);
        await _catalog.SaveChangesAsync(cancellationToken);
        return Result.Success(new UnitOfMeasureDeactivateResult(unit.UnitOfMeasureId, unit.IsActive));
    }

    private static bool TryParseDimension(string raw, out UnitOfMeasureDimension dimension) =>
        Enum.TryParse(raw, true, out dimension);

    private async Task<Result> EnsureUniqueCodeAsync(string code, Guid? exceptId, CancellationToken cancellationToken)
    {
        var clash = await _catalog.UnitsOfMeasure.AsNoTracking()
            .AnyAsync(x => x.Code == code && (!exceptId.HasValue || x.UnitOfMeasureId != exceptId), cancellationToken);
        return clash
            ? Result.Failure(new SemanticError(CatalogErrorCodes.UnitCodeDuplicate))
            : Result.Success();
    }

    private async Task<Result> ValidateTranslationsAsync(
        IReadOnlyList<UnitOfMeasureTranslationWriteModel> translations,
        CancellationToken cancellationToken)
    {
        var known = (await _languages.ListAsync(cancellationToken)).Select(x => x.LanguageId).ToHashSet();
        if (translations.Any(t => !known.Contains(t.LanguageId)))
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.UnitLanguageUnknown));
        }

        return Result.Success();
    }

    private async Task<Guid> ResolveLanguageIdAsync(string? language, CancellationToken cancellationToken)
    {
        var list = await _languages.ListAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(language))
        {
            var match = list.FirstOrDefault(x =>
                string.Equals(x.Code, language, StringComparison.OrdinalIgnoreCase)
                || string.Equals(x.UrlPrefix, language, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                return match.LanguageId;
            }
        }

        return (list.FirstOrDefault(x => x.IsDefault) ?? list.FirstOrDefault())?.LanguageId ?? Guid.Empty;
    }
}
