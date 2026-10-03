using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>ردیف فهرست تنوع‌ها؛ OfferCount اختیاری توسط Host پر می‌شود.</summary>
public sealed record ProductVariantListItem(
    Guid VariantId,
    string Fingerprint,
    CatalogPublicationStatus Status,
    int SortOrder,
    bool IsDefault,
    string? CatalogCodeSeam,
    IReadOnlyList<ProductVariantAxisLabel> AxisLabels,
    int? OfferCount);
