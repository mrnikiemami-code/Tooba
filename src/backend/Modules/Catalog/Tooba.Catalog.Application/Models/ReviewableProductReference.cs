using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>مرجع محصول برای Reviews شامل گونه‌های Catalog و بدون نشت موجودیت EF.</summary>
public sealed record ReviewableProductReference(
    Guid ProductId,
    string Slug,
    string Title,
    CatalogPublicationStatus Status,
    IReadOnlyList<Guid> VariantIds);
