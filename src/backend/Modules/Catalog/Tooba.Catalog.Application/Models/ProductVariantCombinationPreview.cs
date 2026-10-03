using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>یک ترکیب در پیش‌نمایش ماتریس.</summary>
public sealed record ProductVariantCombinationPreview(
    string DesiredFingerprint,
    IReadOnlyList<ProductVariantAxisLabel> AxisLabels,
    Guid? ExistingVariantId,
    ProductVariantCombinationAction Action,
    bool? ReferencedByOffers);
