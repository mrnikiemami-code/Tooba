using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Attributes.Schema.Models;

namespace Tooba.Catalog.Application.Attributes.Schema.Ports;

/// <summary>Catalog-owned port for Category Attribute-Schema Admin operations.</summary>
public interface ICategoryAttributeSchemaDirectory
{
    /// <summary>Effective schema for a category after ancestry merge.</summary>
    Task<Result<IReadOnlyList<EffectiveSchemaEntry>>> GetEffectiveAsync(
        Guid categoryId,
        CancellationToken cancellationToken);

    /// <summary>Binds a definition to a category with local assignment flags.</summary>
    Task<Result<CategoryAttributeSchemaMutationResult>> BindAsync(
        Guid categoryId,
        Guid definitionId,
        int displayOrder,
        CategoryAttributeAssignmentFlags flags,
        CancellationToken cancellationToken);

    /// <summary>Updates local assignment flags on an existing binding.</summary>
    Task<Result<CategoryAttributeSchemaMutationResult>> UpdateBindingAsync(
        Guid categoryId,
        Guid definitionId,
        CategoryAttributeAssignmentFlags flags,
        CancellationToken cancellationToken);

    /// <summary>Removes a local category-attribute binding.</summary>
    Task<Result<CategoryAttributeSchemaMutationResult>> UnbindAsync(
        Guid categoryId,
        Guid definitionId,
        CancellationToken cancellationToken);

    /// <summary>Rewrites DisplayOrder for the exact local binding set.</summary>
    Task<Result<CategoryAttributeSchemaMutationResult>> ReorderAsync(
        Guid categoryId,
        IReadOnlyList<Guid> orderedDefinitionIds,
        CancellationToken cancellationToken);
}
