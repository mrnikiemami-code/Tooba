using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>
/// نمای تعریف ویژگی برای schema authoring بدون نشت EF.
/// </summary>
public sealed record AttributeDefinitionView(
    Guid DefinitionId,
    string Code,
    CatalogAttributeValueKind ValueKind,
    bool IsVariantAxisAllowed,
    string? Unit,
    bool IsRequired,
    bool IsFilterable,
    bool IsComparable,
    bool IsMultivalue,
    int DisplayOrder,
    decimal? ValidationMin,
    decimal? ValidationMax,
    int? ValidationMaxLength,
    bool IsActive,
    DateTimeOffset CreatedAt);
