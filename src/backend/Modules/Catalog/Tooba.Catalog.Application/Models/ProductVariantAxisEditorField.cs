using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>فیلد محور تنوع برای انتخاب مقادیر محصول.</summary>
public sealed record ProductVariantAxisEditorField(
    Guid DefinitionId,
    string Code,
    string LocalizedName,
    CatalogAttributeValueKind ValueKind,
    IReadOnlyList<ProductVariantAxisOption> Options,
    IReadOnlyList<Guid> SelectedOptionIds);
