using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>
/// ردیف schema مؤثر رده پس از ارث والدین.
/// </summary>
public sealed record EffectiveSchemaEntry(
    Guid DefinitionId,
    string Code,
    CatalogAttributeValueKind ValueKind,
    bool IsVariantAxisAllowed,
    bool IsVariantAxis,
    string? Unit,
    bool IsRequired,
    bool IsFilterable,
    bool IsComparable,
    bool IsMultivalue,
    int DisplayOrder,
    Guid InheritedFromCategoryId,
    bool DefinitionIsActive,
    /// <summary>پیوند محلی که رفتار ارثی والد را برای همین رده override می‌کند.</summary>
    bool IsLocalOverride = false,
    /// <summary>نزدیک‌ترین والد منبع قبل از override محلی (در صورت وجود).</summary>
    Guid? OverriddenFromCategoryId = null);
