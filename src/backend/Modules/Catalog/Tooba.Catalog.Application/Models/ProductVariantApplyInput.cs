using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>ورودی اعمال ماتریس تنوع.</summary>
public sealed record ProductVariantApplyInput(
    string? Locale,
    IReadOnlyList<ProductVariantSelectedAxisInput> SelectedAxes,
    Guid? DefaultVariantId,
    IReadOnlyList<ProductVariantPatchInput>? VariantPatches);
