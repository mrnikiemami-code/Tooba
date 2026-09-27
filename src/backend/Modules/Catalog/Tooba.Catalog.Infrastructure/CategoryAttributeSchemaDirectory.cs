using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Attributes.Schema.Models;
using Tooba.Catalog.Application.Attributes.Schema.Ports;
using Tooba.Catalog.Contracts.Errors;
using Tooba.Catalog.Domain;
using Tooba.Catalog.Infrastructure.Persistence;

namespace Tooba.Catalog.Infrastructure;

/// <summary>Catalog persistence for Category Attribute-Schema Admin operations.</summary>
public sealed class CategoryAttributeSchemaDirectory : ICategoryAttributeSchemaDirectory
{
    private readonly CatalogDbContext _db;
    private readonly ICatalogUseCaseGuard _guard;

    /// <summary>Creates the directory.</summary>
    public CategoryAttributeSchemaDirectory(CatalogDbContext db, ICatalogUseCaseGuard guard)
    {
        _db = db;
        _guard = guard;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<EffectiveSchemaEntry>>> GetEffectiveAsync(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        if (!await _db.Categories.AsNoTracking().AnyAsync(x => x.CategoryId == categoryId, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<EffectiveSchemaEntry>>(
                new SemanticError(CatalogErrorCodes.SchemaCategoryMissing));
        }

        var resolved = await ResolveEffectiveBindingsAsync(categoryId, cancellationToken);
        IReadOnlyList<EffectiveSchemaEntry> entries = resolved.Select(x => new EffectiveSchemaEntry(
            x.DefinitionId,
            x.Definition.Code,
            x.Definition.ValueKind,
            x.Definition.IsVariantAxisAllowed,
            x.IsVariantAxis,
            x.Definition.Unit,
            x.IsRequired,
            x.IsFilterable,
            x.IsComparable,
            x.Definition.IsMultivalue,
            x.DisplayOrder,
            x.InheritedFromCategoryId,
            x.Definition.IsActive,
            x.OverriddenFromCategoryId is Guid,
            x.OverriddenFromCategoryId)).ToList();
        return Result.Success(entries);
    }

    /// <inheritdoc />
    public async Task<Result<CategoryAttributeSchemaMutationResult>> BindAsync(
        Guid categoryId,
        Guid definitionId,
        int displayOrder,
        CategoryAttributeAssignmentFlags flags,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(flags);

        var definition = await _db.AttributeDefinitions.SingleOrDefaultAsync(
            x => x.DefinitionId == definitionId,
            cancellationToken);
        if (definition is null)
        {
            return Result.Failure<CategoryAttributeSchemaMutationResult>(
                new SemanticError(CatalogErrorCodes.AttributeMissing));
        }

        if (!await _db.Categories.AnyAsync(x => x.CategoryId == categoryId, cancellationToken))
        {
            return Result.Failure<CategoryAttributeSchemaMutationResult>(
                new SemanticError(CatalogErrorCodes.SchemaCategoryMissing));
        }

        if (!TryValidateVariantAxis(definition, flags.IsVariantAxis, out var axisError))
        {
            return Result.Failure<CategoryAttributeSchemaMutationResult>(axisError!);
        }

        if (await _db.CategoryAttributeBindings.AnyAsync(
                x => x.CategoryId == categoryId && x.DefinitionId == definitionId,
                cancellationToken))
        {
            return Result.Failure<CategoryAttributeSchemaMutationResult>(
                new SemanticError(CatalogErrorCodes.SchemaBindingDuplicate));
        }

        _db.CategoryAttributeBindings.Add(
            CatalogCategoryAttributeBinding.Bind(
                categoryId,
                definitionId,
                displayOrder,
                flags.IsRequired,
                flags.IsFilterable,
                flags.IsVariantAxis,
                flags.IsComparable,
                DateTimeOffset.UtcNow));
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success(new CategoryAttributeSchemaMutationResult());
    }

    /// <inheritdoc />
    public async Task<Result<CategoryAttributeSchemaMutationResult>> UpdateBindingAsync(
        Guid categoryId,
        Guid definitionId,
        CategoryAttributeAssignmentFlags flags,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(flags);

        var definition = await _db.AttributeDefinitions.SingleOrDefaultAsync(
            x => x.DefinitionId == definitionId,
            cancellationToken);
        if (definition is null)
        {
            return Result.Failure<CategoryAttributeSchemaMutationResult>(
                new SemanticError(CatalogErrorCodes.AttributeMissing));
        }

        var binding = await _db.CategoryAttributeBindings.SingleOrDefaultAsync(
            x => x.CategoryId == categoryId && x.DefinitionId == definitionId,
            cancellationToken);
        if (binding is null)
        {
            return Result.Failure<CategoryAttributeSchemaMutationResult>(
                new SemanticError(CatalogErrorCodes.SchemaBindingMissing));
        }

        if (!TryValidateVariantAxis(definition, flags.IsVariantAxis, out var axisError))
        {
            return Result.Failure<CategoryAttributeSchemaMutationResult>(axisError!);
        }

        binding.IsRequired = flags.IsRequired;
        binding.IsFilterable = flags.IsFilterable;
        binding.IsVariantAxis = flags.IsVariantAxis;
        binding.IsComparable = flags.IsComparable;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success(new CategoryAttributeSchemaMutationResult());
    }

    /// <inheritdoc />
    public async Task<Result<CategoryAttributeSchemaMutationResult>> UnbindAsync(
        Guid categoryId,
        Guid definitionId,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var binding = await _db.CategoryAttributeBindings.SingleOrDefaultAsync(
            x => x.CategoryId == categoryId && x.DefinitionId == definitionId,
            cancellationToken);
        if (binding is null)
        {
            return Result.Failure<CategoryAttributeSchemaMutationResult>(
                new SemanticError(CatalogErrorCodes.SchemaBindingMissing));
        }

        _db.CategoryAttributeBindings.Remove(binding);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success(new CategoryAttributeSchemaMutationResult());
    }

    /// <inheritdoc />
    public async Task<Result<CategoryAttributeSchemaMutationResult>> ReorderAsync(
        Guid categoryId,
        IReadOnlyList<Guid> orderedDefinitionIds,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(orderedDefinitionIds);

        var bindings = await _db.CategoryAttributeBindings
            .Where(x => x.CategoryId == categoryId)
            .ToListAsync(cancellationToken);
        if (bindings.Count != orderedDefinitionIds.Count
            || orderedDefinitionIds.Distinct().Count() != orderedDefinitionIds.Count
            || bindings.Select(b => b.DefinitionId).ToHashSet().SetEquals(orderedDefinitionIds) is false)
        {
            return Result.Failure<CategoryAttributeSchemaMutationResult>(
                new SemanticError(CatalogErrorCodes.SchemaReorderInvalid));
        }

        for (var i = 0; i < orderedDefinitionIds.Count; i++)
        {
            var binding = bindings.Single(b => b.DefinitionId == orderedDefinitionIds[i]);
            binding.DisplayOrder = i;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success(new CategoryAttributeSchemaMutationResult());
    }

    private static bool TryValidateVariantAxis(
        CatalogAttributeDefinition definition,
        bool isVariantAxisEnabled,
        out SemanticError? error)
    {
        error = null;
        if (!isVariantAxisEnabled)
        {
            return true;
        }

        if (!definition.IsVariantAxisAllowed)
        {
            error = new SemanticError(CatalogErrorCodes.AttributeVariantAxisCapabilityDisabled);
            return false;
        }

        if (!CatalogCategoryAttributeAssignmentRules.ValueKindSupportsVariantAxis(definition.ValueKind))
        {
            error = new SemanticError(CatalogErrorCodes.AttributeVariantAxisValueKindInvalid);
            return false;
        }

        return true;
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
        return CatalogCategorySchemaResolver.ResolveEffectiveSchema(
            categoryId,
            categoriesById,
            bindings,
            definitionsById);
    }
}
