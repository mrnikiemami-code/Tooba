using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain.ValueObjects;

/// <summary>
/// ردیف میانی حل schema مؤثر قبل از DTO لایهٔ Application.
/// </summary>
public sealed record CatalogEffectiveSchemaBinding(
    Guid DefinitionId,
    int DisplayOrder,
    bool IsRequired,
    bool IsFilterable,
    bool IsVariantAxis,
    bool IsComparable,
    Guid InheritedFromCategoryId,
    CatalogAttributeDefinition Definition,
    /// <summary>اگر فرزند override محلی دارد، نزدیک‌ترین والدِ منبع قبل از override.</summary>
    Guid? OverriddenFromCategoryId = null);
