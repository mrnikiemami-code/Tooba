using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Attributes.Definitions.Models;
using Tooba.Catalog.Application.Attributes.Definitions.Ports;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure.Directories;

/// <summary>Catalog persistence for Attribute Definition Admin operations.</summary>
public sealed class AttributeDefinitionDirectory : IAttributeDefinitionDirectory
{
    private readonly CatalogDbContext _db;
    private readonly ICatalogUseCaseGuard _guard;

    /// <summary>Creates the directory.</summary>
    public AttributeDefinitionDirectory(CatalogDbContext db, ICatalogUseCaseGuard guard)
    {
        _db = db;
        _guard = guard;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<AttributeDefinitionView>>> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await _db.AttributeDefinitions.AsNoTracking()
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<AttributeDefinitionView>>(rows.Select(ToDefinitionView).ToList());
    }

    /// <inheritdoc />
    public async Task<Result<AttributeDefinitionView>> GetAsync(
        Guid definitionId,
        CancellationToken cancellationToken)
    {
        var row = await _db.AttributeDefinitions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.DefinitionId == definitionId, cancellationToken);
        return row is null
            ? Result.Failure<AttributeDefinitionView>(new SemanticError(CatalogErrorCodes.AttributeMissing))
            : Result.Success(ToDefinitionView(row));
    }

    /// <inheritdoc />
    public async Task<Result<AttributeDefinitionCreatedResult>> CreateAsync(
        string code,
        CatalogAttributeValueKind valueKind,
        bool isVariantAxisAllowed,
        IReadOnlyDictionary<string, string> localizedNames,
        AttributeDefinitionMetadataWriteModel? metadata,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var normalizedCode = code.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return Result.Failure<AttributeDefinitionCreatedResult>(
                new SemanticError(CatalogErrorCodes.AttributeInvalid));
        }

        var codeTaken = await _db.AttributeDefinitions.AsNoTracking()
            .AnyAsync(x => x.Code == normalizedCode, cancellationToken);
        if (codeTaken)
        {
            return Result.Failure<AttributeDefinitionCreatedResult>(
                new SemanticError(CatalogErrorCodes.AttributeCodeDuplicate));
        }

        foreach (var pair in localizedNames)
        {
            var name = pair.Value.Trim();
            if (name.Length == 0)
            {
                continue;
            }

            var locale = pair.Key.Trim();
            var nameTaken = await _db.LocalizedTexts.AsNoTracking().AnyAsync(
                t => t.OwnerKind == CatalogLocalizedOwnerKind.AttributeDefinition
                    && t.FieldKey == "name"
                    && t.Locale == locale
                    && t.Value.ToLower() == name.ToLower(),
                cancellationToken);
            if (nameTaken)
            {
                return Result.Failure<AttributeDefinitionCreatedResult>(
                    new SemanticError(CatalogErrorCodes.AttributeNameDuplicate));
            }
        }

        if (isVariantAxisAllowed
            && !CatalogCategoryAttributeAssignmentRules.ValueKindSupportsVariantAxis(valueKind))
        {
            return Result.Failure<AttributeDefinitionCreatedResult>(
                new SemanticError(CatalogErrorCodes.AttributeVariantAxisValueKindInvalid));
        }

        if (!TryValidateLocalizedNamesPresent(localizedNames, out var namesError))
        {
            return Result.Failure<AttributeDefinitionCreatedResult>(namesError!);
        }

        if (metadata is not null && !TryValidateMetadataBounds(metadata, out var metaError))
        {
            return Result.Failure<AttributeDefinitionCreatedResult>(metaError!);
        }

        var definition = CatalogAttributeDefinition.Create(
            normalizedCode,
            valueKind,
            isVariantAxisAllowed,
            DateTimeOffset.UtcNow);
        _db.AttributeDefinitions.Add(definition);
        AddLocalizedNames(CatalogLocalizedOwnerKind.AttributeDefinition, definition.DefinitionId, localizedNames);

        if (metadata is not null)
        {
            definition.UpdateMetadata(
                metadata.Unit,
                metadata.IsRequired,
                metadata.IsFilterable,
                metadata.IsComparable,
                metadata.IsMultivalue,
                metadata.DisplayOrder,
                metadata.ValidationMin,
                metadata.ValidationMax,
                metadata.ValidationMaxLength,
                metadata.IsActive);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success(new AttributeDefinitionCreatedResult(definition.DefinitionId));
    }

    /// <inheritdoc />
    public async Task<Result<AttributeDefinitionView>> UpdateMetadataAsync(
        Guid definitionId,
        AttributeDefinitionMetadataWriteModel metadata,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (!TryValidateMetadataBounds(metadata, out var metaError))
        {
            return Result.Failure<AttributeDefinitionView>(metaError!);
        }

        var definition = await _db.AttributeDefinitions
            .SingleOrDefaultAsync(x => x.DefinitionId == definitionId, cancellationToken);
        if (definition is null)
        {
            return Result.Failure<AttributeDefinitionView>(
                new SemanticError(CatalogErrorCodes.AttributeMissing));
        }

        definition.UpdateMetadata(
            metadata.Unit,
            metadata.IsRequired,
            metadata.IsFilterable,
            metadata.IsComparable,
            metadata.IsMultivalue,
            metadata.DisplayOrder,
            metadata.ValidationMin,
            metadata.ValidationMax,
            metadata.ValidationMaxLength,
            metadata.IsActive);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success(ToDefinitionView(definition));
    }

    /// <inheritdoc />
    public async Task<Result<VariantAxisCapabilityDisableImpactView>> PreviewVariantAxisCapabilityDisableAsync(
        Guid definitionId,
        CancellationToken cancellationToken)
    {
        var exists = await _db.AttributeDefinitions.AsNoTracking()
            .AnyAsync(x => x.DefinitionId == definitionId, cancellationToken);
        if (!exists)
        {
            return Result.Failure<VariantAxisCapabilityDisableImpactView>(
                new SemanticError(CatalogErrorCodes.AttributeMissing));
        }

        var variantBindings = await _db.CategoryAttributeBindings.AsNoTracking()
            .Where(b => b.DefinitionId == definitionId && b.IsVariantAxis)
            .ToListAsync(cancellationToken);

        var categoryIds = variantBindings.Select(b => b.CategoryId).Distinct().ToArray();
        var names = await GetCategoryNamesAsync(categoryIds, cancellationToken);

        var affected = variantBindings
            .GroupBy(b => b.CategoryId)
            .Select(g => new VariantAxisAffectedCategorySummary(
                g.Key,
                names.GetValueOrDefault(g.Key) ?? g.Key.ToString(),
                g.Count()))
            .ToList();

        var productCount = categoryIds.Length == 0
            ? 0
            : await _db.ProductCategories.AsNoTracking()
                .Where(x => x.Role == CatalogProductCategoryRole.Primary && categoryIds.Contains(x.CategoryId))
                .Select(x => x.ProductId)
                .Distinct()
                .CountAsync(cancellationToken);

        var variantCombinationCount = await _db.ProductVariantAxes.AsNoTracking()
            .CountAsync(x => x.DefinitionId == definitionId, cancellationToken);

        return Result.Success(new VariantAxisCapabilityDisableImpactView(
            variantBindings.Count,
            affected,
            productCount,
            variantCombinationCount,
            variantBindings.Count == 0));
    }

    /// <inheritdoc />
    public async Task<Result<AttributeDefinitionView>> SetVariantAxisCapabilityAsync(
        Guid definitionId,
        bool isVariantAxisAllowed,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var definition = await _db.AttributeDefinitions
            .SingleOrDefaultAsync(x => x.DefinitionId == definitionId, cancellationToken);
        if (definition is null)
        {
            return Result.Failure<AttributeDefinitionView>(
                new SemanticError(CatalogErrorCodes.AttributeMissing));
        }

        if (definition.IsVariantAxis == isVariantAxisAllowed)
        {
            return Result.Success(ToDefinitionView(definition));
        }

        if (isVariantAxisAllowed)
        {
            if (!CatalogCategoryAttributeAssignmentRules.ValueKindSupportsVariantAxis(definition.ValueKind))
            {
                return Result.Failure<AttributeDefinitionView>(
                    new SemanticError(CatalogErrorCodes.AttributeVariantAxisValueKindInvalid));
            }

            definition.SetVariantAxisAllowed(true);
        }
        else
        {
            var impact = await PreviewVariantAxisCapabilityDisableAsync(definitionId, cancellationToken);
            if (impact.IsFailure)
            {
                return Result.Failure<AttributeDefinitionView>(impact.FirstError);
            }

            if (!impact.Value.CanDisable)
            {
                return Result.Failure<AttributeDefinitionView>(
                    new SemanticError(CatalogErrorCodes.AttributeVariantAxisInUse));
            }

            definition.SetVariantAxisAllowed(false);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success(ToDefinitionView(definition));
    }

    /// <inheritdoc />
    public async Task<Result<AttributeOptionCreatedResult>> AddOptionAsync(
        Guid definitionId,
        string code,
        IReadOnlyDictionary<string, string> localizedNames,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var definition = await _db.AttributeDefinitions
            .SingleOrDefaultAsync(x => x.DefinitionId == definitionId, cancellationToken);
        if (definition is null)
        {
            return Result.Failure<AttributeOptionCreatedResult>(
                new SemanticError(CatalogErrorCodes.AttributeMissing));
        }

        if (definition.ValueKind != CatalogAttributeValueKind.Enumeration)
        {
            return Result.Failure<AttributeOptionCreatedResult>(
                new SemanticError(CatalogErrorCodes.AttributeInvalid));
        }

        if (!TryValidateLocalizedNamesPresent(localizedNames, out var namesError))
        {
            return Result.Failure<AttributeOptionCreatedResult>(namesError!);
        }

        var option = CatalogAttributeOption.Create(definitionId, code);
        _db.AttributeOptions.Add(option);
        AddLocalizedNames(CatalogLocalizedOwnerKind.AttributeOption, option.OptionId, localizedNames);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success(new AttributeOptionCreatedResult(option.OptionId));
    }

    private async Task<IReadOnlyDictionary<Guid, string>> GetCategoryNamesAsync(
        IReadOnlyCollection<Guid> categoryIds,
        CancellationToken cancellationToken)
    {
        if (categoryIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var ids = categoryIds.Distinct().ToArray();
        var translations = await _db.CategoryTranslations.AsNoTracking()
            .Where(x => ids.Contains(x.CategoryId))
            .OrderByDescending(x => x.Locale == "fa-IR")
            .ThenByDescending(x => x.Locale.StartsWith("fa"))
            .ThenBy(x => x.Locale)
            .ToListAsync(cancellationToken);
        var fromTranslations = translations
            .GroupBy(x => x.CategoryId)
            .ToDictionary(g => g.Key, g => g.First().Name);

        var missing = ids.Where(id => !fromTranslations.ContainsKey(id)).ToArray();
        if (missing.Length == 0)
        {
            return fromTranslations;
        }

        var rows = await _db.LocalizedTexts.AsNoTracking()
            .Where(x => x.OwnerKind == CatalogLocalizedOwnerKind.Category
                && x.FieldKey == "name" && missing.Contains(x.OwnerId))
            .OrderByDescending(x => x.Locale == "fa-IR")
            .ThenByDescending(x => x.Locale.StartsWith("fa"))
            .ThenBy(x => x.Locale)
            .ToListAsync(cancellationToken);
        foreach (var group in rows.GroupBy(x => x.OwnerId))
        {
            fromTranslations[group.Key] = group.First().Value;
        }

        return fromTranslations;
    }

    private static bool TryValidateMetadataBounds(
        AttributeDefinitionMetadataWriteModel metadata,
        out SemanticError? error)
    {
        if (metadata.ValidationMin is not null
            && metadata.ValidationMax is not null
            && metadata.ValidationMin > metadata.ValidationMax)
        {
            error = new SemanticError(CatalogErrorCodes.AttributeInvalid);
            return false;
        }

        if (metadata.ValidationMaxLength is < 0)
        {
            error = new SemanticError(CatalogErrorCodes.AttributeInvalid);
            return false;
        }

        error = null;
        return true;
    }

    private static bool TryValidateLocalizedNamesPresent(
        IReadOnlyDictionary<string, string> localizedNames,
        out SemanticError? error)
    {
        if (localizedNames.Count == 0
            || localizedNames.Values.All(string.IsNullOrWhiteSpace))
        {
            error = new SemanticError(CatalogErrorCodes.AttributeInvalid);
            return false;
        }

        error = null;
        return true;
    }

    private void AddLocalizedNames(
        CatalogLocalizedOwnerKind ownerKind,
        Guid ownerId,
        IReadOnlyDictionary<string, string> localizedNames)
    {
        foreach (var pair in localizedNames)
        {
            if (string.IsNullOrWhiteSpace(pair.Value))
            {
                continue;
            }

            _db.LocalizedTexts.Add(
                CatalogLocalizedText.Create(ownerKind, ownerId, "name", pair.Key, pair.Value));
        }
    }

    private static AttributeDefinitionView ToDefinitionView(CatalogAttributeDefinition definition) =>
        new(
            definition.DefinitionId,
            definition.Code,
            definition.ValueKind,
            definition.IsVariantAxisAllowed,
            definition.Unit,
            definition.IsRequired,
            definition.IsFilterable,
            definition.IsComparable,
            definition.IsMultivalue,
            definition.DisplayOrder,
            definition.ValidationMin,
            definition.ValidationMax,
            definition.ValidationMaxLength,
            definition.IsActive,
            definition.CreatedAt);
}
