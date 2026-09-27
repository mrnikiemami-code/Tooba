namespace Tooba.Catalog.Application.Attributes.Schema.Models;

/// <summary>Body for PATCH category attribute-schema binding flags.</summary>
public sealed record UpdateCategoryAttributeBindingWriteModel(
    bool IsRequired,
    bool IsFilterable,
    bool IsVariantAxis,
    bool IsComparable);
