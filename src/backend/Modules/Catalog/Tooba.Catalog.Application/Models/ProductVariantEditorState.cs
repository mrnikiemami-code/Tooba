using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>حالت کامل ویرایشگر ماتریس تنوع.</summary>
public sealed record ProductVariantEditorState(
    Guid ProductId,
    string? CategoryPath,
    IReadOnlyList<ProductVariantAxisEditorField> Axes,
    IReadOnlyList<ProductVariantListItem> Variants,
    ProductVariantReadiness Readiness,
    int MaxCombinations,
    string? MessageFa);
