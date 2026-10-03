using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>
/// مرجع پایدار محصول توصیفی برای ماژول‌های بعدی بدون نشت EF. قیمت ندارد.
/// </summary>
public sealed record ProductReference(Guid ProductId, CatalogProductKind Kind, CatalogPublicationStatus Status);
