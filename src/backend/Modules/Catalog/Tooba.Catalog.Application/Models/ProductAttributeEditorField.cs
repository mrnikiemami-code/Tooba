using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>
/// فیلد ویرایشگر ویژگی محصول؛ محورهای تنوع ورودی ویرایش ندارند.
/// </summary>
public sealed record ProductAttributeEditorField(
    Guid DefinitionId,
    string Code,
    string LocalizedName,
    CatalogAttributeValueKind ValueKind,
    string? Unit,
    bool IsRequired,
    bool IsVariantAxis,
    bool IsFilterable,
    bool IsComparable,
    bool IsMultivalue,
    int DisplayOrder,
    IReadOnlyList<ProductAttributeEditorOption> Options,
    string? CurrentCanonicalValue,
    Guid? CurrentEnumOptionId,
    string? DisplayValue,
    bool IsMissingRequired);
