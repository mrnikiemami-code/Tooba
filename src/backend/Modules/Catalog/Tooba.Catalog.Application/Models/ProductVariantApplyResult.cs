using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>نتیجهٔ اعمال ماتریس تنوع.</summary>
public sealed record ProductVariantApplyResult(
    int Created,
    int Unchanged,
    int Deactivated,
    IReadOnlyList<ProductVariantListItem> Variants);
