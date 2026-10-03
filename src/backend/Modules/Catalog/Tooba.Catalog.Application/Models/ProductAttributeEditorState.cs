using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>حالت کامل ویرایشگر ویژگی محصول.</summary>
public sealed record ProductAttributeEditorState(
    Guid ProductId,
    Guid? CategoryId,
    string? CategoryPath,
    IReadOnlyList<ProductAttributeEditorField> Fields,
    ProductAttributeReadiness Readiness);
