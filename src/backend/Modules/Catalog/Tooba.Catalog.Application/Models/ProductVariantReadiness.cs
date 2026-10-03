using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>آمادگی تنوع‌ها برای انتشار بعدی.</summary>
public sealed record ProductVariantReadiness(
    bool IsValid,
    IReadOnlyList<string> MissingAxes,
    IReadOnlyList<string> InvalidVariants,
    IReadOnlyList<string> DuplicateCombinations,
    bool? NoDefaultVariant);
