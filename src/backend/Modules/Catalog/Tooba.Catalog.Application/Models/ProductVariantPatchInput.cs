using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>پچ اختیاری روی تنوع موجود هنگام اعمال.</summary>
public sealed record ProductVariantPatchInput(
    Guid VariantId,
    CatalogPublicationStatus? Status,
    string? CatalogCodeSeam,
    int? SortOrder,
    bool? IsDefault);
