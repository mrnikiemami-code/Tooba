using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Facets.Ports;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Directories;

/// <summary>Catalog persistence for Facet Admin + Storefront operations.</summary>
public sealed class FacetDirectory : IFacetDirectory
{
    private readonly CatalogDbContext _db;
    private readonly ICatalogUseCaseGuard _guard;

    /// <summary>Creates the directory.</summary>
    public FacetDirectory(CatalogDbContext db, ICatalogUseCaseGuard guard)
    {
        _db = db;
        _guard = guard;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<EffectiveCategoryFacet>>> GetEffectiveFacetsAsync(
        Guid categoryId,
        string locale,
        CancellationToken cancellationToken)
    {
        if (!await _db.Categories.AnyAsync(x => x.CategoryId == categoryId, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<EffectiveCategoryFacet>>(
                new SemanticError(CatalogErrorCodes.FacetCategoryMissing));
        }

        var resolved = await ResolveEffectiveFacetsAsync(categoryId, cancellationToken);
        if (resolved.Count == 0)
        {
            return Result.Success<IReadOnlyList<EffectiveCategoryFacet>>(Array.Empty<EffectiveCategoryFacet>());
        }

        var names = await GetAttributeDefinitionNamesAsync(
            resolved.Select(x => x.DefinitionId).ToArray(),
            locale,
            cancellationToken);
        var list = resolved.Select(x => new EffectiveCategoryFacet(
            x.DefinitionId,
            x.Definition.Code,
            names.GetValueOrDefault(x.DefinitionId) ?? x.Definition.Code,
            x.Definition.ValueKind,
            x.DisplayType,
            x.SortOrder,
            x.IsVisible,
            x.IsSearchable,
            x.IsCollapsedByDefault,
            x.ShowCounts,
            x.SourceCategoryId,
            x.SourceCategoryId != categoryId)).ToList();
        return Result.Success<IReadOnlyList<EffectiveCategoryFacet>>(list);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CategoryFacetConfigurationView>>> ListLocalConfigurationsAsync(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        if (!await _db.Categories.AnyAsync(x => x.CategoryId == categoryId, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<CategoryFacetConfigurationView>>(
                new SemanticError(CatalogErrorCodes.FacetCategoryMissing));
        }

        var configs = await _db.CategoryFacetConfigurations.AsNoTracking()
            .Where(x => x.CategoryId == categoryId)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);
        if (configs.Count == 0)
        {
            return Result.Success<IReadOnlyList<CategoryFacetConfigurationView>>(
                Array.Empty<CategoryFacetConfigurationView>());
        }

        var definitionIds = configs.Select(x => x.DefinitionId).ToArray();
        var definitions = await _db.AttributeDefinitions.AsNoTracking()
            .Where(x => definitionIds.Contains(x.DefinitionId))
            .ToDictionaryAsync(x => x.DefinitionId, cancellationToken);

        var views = new List<CategoryFacetConfigurationView>(configs.Count);
        foreach (var config in configs)
        {
            if (!definitions.TryGetValue(config.DefinitionId, out var definition))
            {
                return Result.Failure<IReadOnlyList<CategoryFacetConfigurationView>>(
                    new SemanticError(CatalogErrorCodes.FacetDefinitionMissing));
            }

            views.Add(new CategoryFacetConfigurationView(
                config.FacetConfigurationId,
                config.CategoryId,
                config.DefinitionId,
                definition.Code,
                definition.ValueKind,
                config.DisplayType,
                config.SortOrder,
                config.IsVisible,
                config.IsSearchable,
                config.IsCollapsedByDefault,
                config.ShowCounts));
        }

        return Result.Success<IReadOnlyList<CategoryFacetConfigurationView>>(views);
    }

    /// <inheritdoc />
    public async Task<Result> UpsertConfigurationAsync(
        Guid categoryId,
        Guid definitionId,
        CategoryFacetConfigurationInput input,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(input);
        if (!await _db.Categories.AnyAsync(x => x.CategoryId == categoryId, cancellationToken))
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.FacetCategoryMissing));
        }

        var definition = await _db.AttributeDefinitions.SingleOrDefaultAsync(
            x => x.DefinitionId == definitionId,
            cancellationToken);
        if (definition is null)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.FacetDefinitionMissing));
        }

        var effective = await ResolveEffectiveBindingsAsync(categoryId, cancellationToken);
        var schemaRow = effective.SingleOrDefault(x => x.DefinitionId == definitionId);
        if (schemaRow is null)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.FacetSchemaMissing));
        }

        if (!schemaRow.IsFilterable)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.FacetNotFilterable));
        }

        var displayViolation = CatalogCategoryFacetRules.ValidateDisplayType(definition, input.DisplayType);
        if (displayViolation != CatalogFacetDisplayTypeViolation.None)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.FacetDisplayTypeInvalid));
        }

        var isSearchable = input.IsSearchable && CatalogCategoryFacetRules.IsSearchableAllowed(input.DisplayType);

        var existing = await _db.CategoryFacetConfigurations.SingleOrDefaultAsync(
            x => x.CategoryId == categoryId && x.DefinitionId == definitionId,
            cancellationToken);
        if (existing is null)
        {
            _db.CategoryFacetConfigurations.Add(
                CatalogCategoryFacetConfiguration.Create(
                    categoryId,
                    definitionId,
                    input.DisplayType,
                    input.SortOrder,
                    input.IsVisible,
                    isSearchable,
                    input.IsCollapsedByDefault,
                    input.ShowCounts,
                    DateTimeOffset.UtcNow));
        }
        else
        {
            existing.DisplayType = input.DisplayType;
            existing.SortOrder = input.SortOrder;
            existing.IsVisible = input.IsVisible;
            existing.IsSearchable = isSearchable;
            existing.IsCollapsedByDefault = input.IsCollapsedByDefault;
            existing.ShowCounts = input.ShowCounts;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> RemoveOverrideAsync(
        Guid categoryId,
        Guid definitionId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var config = await _db.CategoryFacetConfigurations.SingleOrDefaultAsync(
            x => x.CategoryId == categoryId && x.DefinitionId == definitionId,
            cancellationToken);
        if (config is null)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.FacetOverrideMissing));
        }

        _db.CategoryFacetConfigurations.Remove(config);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> ReorderConfigurationsAsync(
        Guid categoryId,
        IReadOnlyList<Guid> orderedDefinitionIds,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(orderedDefinitionIds);
        var configs = await _db.CategoryFacetConfigurations
            .Where(x => x.CategoryId == categoryId)
            .ToListAsync(cancellationToken);
        if (configs.Count != orderedDefinitionIds.Count
            || orderedDefinitionIds.Distinct().Count() != orderedDefinitionIds.Count
            || configs.Select(c => c.DefinitionId).ToHashSet().SetEquals(orderedDefinitionIds) is false)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.FacetReorderInvalid));
        }

        for (var i = 0; i < orderedDefinitionIds.Count; i++)
        {
            var config = configs.Single(c => c.DefinitionId == orderedDefinitionIds[i]);
            config.SortOrder = i;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<IReadOnlyList<CatalogEffectiveSchemaBinding>> ResolveEffectiveBindingsAsync(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var categories = await _db.Categories.AsNoTracking().ToListAsync(cancellationToken);
        var categoriesById = categories.ToDictionary(x => x.CategoryId);
        var bindings = await _db.CategoryAttributeBindings.AsNoTracking().ToListAsync(cancellationToken);
        var definitions = await _db.AttributeDefinitions.AsNoTracking().ToListAsync(cancellationToken);
        var definitionsById = definitions.ToDictionary(x => x.DefinitionId);
        return CatalogCategorySchemaResolver.ResolveEffectiveSchema(categoryId, categoriesById, bindings, definitionsById);
    }

    private async Task<IReadOnlyList<CatalogEffectiveFacetBinding>> ResolveEffectiveFacetsAsync(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var categories = await _db.Categories.AsNoTracking().ToListAsync(cancellationToken);
        var categoriesById = categories.ToDictionary(x => x.CategoryId);
        var configs = await _db.CategoryFacetConfigurations.AsNoTracking().ToListAsync(cancellationToken);
        var effectiveSchema = await ResolveEffectiveBindingsAsync(categoryId, cancellationToken);
        var definitions = await _db.AttributeDefinitions.AsNoTracking().ToListAsync(cancellationToken);
        var definitionsById = definitions.ToDictionary(x => x.DefinitionId);
        return CatalogCategoryFacetResolver.ResolveEffectiveFacets(
            categoryId,
            categoriesById,
            configs,
            effectiveSchema,
            definitionsById);
    }

    private async Task<IReadOnlyDictionary<Guid, string>> GetAttributeDefinitionNamesAsync(
        IReadOnlyCollection<Guid> definitionIds,
        string locale,
        CancellationToken cancellationToken)
    {
        if (definitionIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var normalizedLocale = locale.Trim();
        var localePrefix = normalizedLocale.Split('-')[0];
        var ids = definitionIds.Distinct().ToArray();
        var rows = await _db.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.AttributeDefinition
                && x.FieldKey == "name"
                && ids.Contains(x.OwnerId))
            .OrderByDescending(x => x.Locale == normalizedLocale)
            .ThenByDescending(x => x.Locale.StartsWith(localePrefix))
            .ThenBy(x => x.Locale)
            .ToListAsync(cancellationToken);
        var names = rows.GroupBy(x => x.OwnerId).ToDictionary(g => g.Key, g => g.First().Value);
        var definitions = await _db.AttributeDefinitions.AsNoTracking()
            .Where(x => ids.Contains(x.DefinitionId))
            .Select(x => new { x.DefinitionId, x.Code })
            .ToListAsync(cancellationToken);
        foreach (var def in definitions)
        {
            names.TryAdd(def.DefinitionId, def.Code);
        }

        return names;
    }
}
