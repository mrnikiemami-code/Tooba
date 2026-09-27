namespace Tooba.Catalog.Application.Attributes.Schema.Models;

/// <summary>Body for POST category attribute-schema bindings.</summary>
public sealed record BindCategoryAttributeWriteModel(
    Guid DefinitionId,
    int DisplayOrder,
    bool IsRequired,
    bool IsFilterable,
    bool IsVariantAxis,
    bool IsComparable);
