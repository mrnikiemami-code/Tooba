using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.Attributes.ProductValues.Ports;

/// <summary>Catalog-owned port for Product Attribute Editor/Readiness Admin operations.</summary>
public interface IProductAttributeDirectory
{
    /// <summary>Editor state for product attributes from primary-category effective schema.</summary>
    Task<Result<ProductAttributeEditorState>> GetEditorStateAsync(
        Guid productId,
        string locale,
        CancellationToken cancellationToken);

    /// <summary>Readiness for required/non-axis schema fields.</summary>
    Task<Result<ProductAttributeReadiness>> GetReadinessAsync(
        Guid productId,
        CancellationToken cancellationToken);

    /// <summary>Upserts one non-axis product attribute value.</summary>
    Task<Result> SetSingleAsync(
        Guid productId,
        Guid definitionId,
        string rawValue,
        Guid? enumOptionId,
        CancellationToken cancellationToken);

    /// <summary>Applies bulk values in one transaction and records product history; returns refreshed editor state.</summary>
    Task<Result<ProductAttributeEditorState>> SetBulkAsync(
        Guid productId,
        IReadOnlyList<ProductAttributeValueInput> values,
        string locale,
        CancellationToken cancellationToken);
}
