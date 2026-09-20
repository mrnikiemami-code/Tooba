using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure;

/// <summary>orchestration موقت نوشتن UnitOfMeasure روی Catalog DbContext.</summary>
public sealed class UnitOfMeasureDirectory : IUnitOfMeasureDirectory
{
    private readonly CatalogDbContext _catalog;
    private readonly IClock _clock;
    private readonly IIdGenerator _ids;
    private readonly IUnitOfMeasureLanguageGate _languages;

    /// <summary>دایرکتوری را به schema catalog وصل می‌کند.</summary>
    public UnitOfMeasureDirectory(
        CatalogDbContext catalog,
        IClock clock,
        IIdGenerator ids,
        IUnitOfMeasureLanguageGate languages)
    {
        _catalog = catalog;
        _clock = clock;
        _ids = ids;
        _languages = languages;
    }

    /// <inheritdoc />
    public async Task<Guid> CreateAsync(UnitOfMeasureWriteModel model, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var dimension = ParseDimension(model.Dimension);
        var unit = UnitOfMeasure.Create(_ids.NewId(), model.Code, dimension, model.IsActive, model.SortOrder, now);
        await EnsureUniqueCodeAsync(unit.Code, null, cancellationToken);
        await ValidateTranslationsAsync(model.Translations, cancellationToken);
        _catalog.UnitsOfMeasure.Add(unit);
        foreach (var t in model.Translations)
        {
            _catalog.UnitOfMeasureTranslations.Add(
                UnitOfMeasureTranslation.Create(unit.UnitOfMeasureId, t.LanguageId, t.Name, t.ShortName));
        }

        await _catalog.SaveChangesAsync(cancellationToken);
        return unit.UnitOfMeasureId;
    }

    /// <inheritdoc />
    public async Task<Guid> UpdateAsync(Guid unitId, UnitOfMeasureWriteModel model, CancellationToken cancellationToken)
    {
        var unit = await _catalog.UnitsOfMeasure.SingleOrDefaultAsync(x => x.UnitOfMeasureId == unitId, cancellationToken)
            ?? throw new PlatformHttpException(404, "Not Found", "unit.missing");
        var dimension = ParseDimension(model.Dimension);
        var code = model.Code.Trim().ToLowerInvariant();
        await EnsureUniqueCodeAsync(code, unitId, cancellationToken);
        await ValidateTranslationsAsync(model.Translations, cancellationToken);
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
        return unit.UnitOfMeasureId;
    }

    /// <inheritdoc />
    public async Task<(Guid UnitId, bool IsActive)> DeactivateAsync(Guid unitId, CancellationToken cancellationToken)
    {
        var unit = await _catalog.UnitsOfMeasure.SingleOrDefaultAsync(x => x.UnitOfMeasureId == unitId, cancellationToken)
            ?? throw new PlatformHttpException(404, "Not Found", "unit.missing");
        unit.SetActive(false, _clock.UtcNow);
        await _catalog.SaveChangesAsync(cancellationToken);
        return (unit.UnitOfMeasureId, unit.IsActive);
    }

    private static UnitOfMeasureDimension ParseDimension(string raw) =>
        Enum.TryParse<UnitOfMeasureDimension>(raw, true, out var d)
            ? d
            : throw new InvalidOperationException("unit.dimension.invalid");

    private async Task EnsureUniqueCodeAsync(string code, Guid? exceptId, CancellationToken cancellationToken)
    {
        var clash = await _catalog.UnitsOfMeasure.AsNoTracking()
            .AnyAsync(x => x.Code == code && (!exceptId.HasValue || x.UnitOfMeasureId != exceptId), cancellationToken);
        if (clash)
        {
            throw new InvalidOperationException("unit.code.duplicate");
        }
    }

    private async Task ValidateTranslationsAsync(
        IReadOnlyList<UnitOfMeasureTranslationWriteModel> translations,
        CancellationToken cancellationToken)
    {
        await _languages.EnsureKnownAsync(translations.Select(t => t.LanguageId).ToArray(), cancellationToken);
    }
}
