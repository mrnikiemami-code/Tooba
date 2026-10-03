using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>نتیجهٔ پیش‌نمایش ماتریس تنوع.</summary>
public sealed record ProductVariantPreviewResult(
    IReadOnlyList<ProductVariantCombinationPreview> Combinations,
    int UnchangedCount,
    int NewCount,
    int DeactivateCount,
    int TotalDesired,
    bool Capped,
    string? WarningFa,
    string? MessageFa);
