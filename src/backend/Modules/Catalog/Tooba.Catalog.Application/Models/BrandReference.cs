using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>
/// مرجع برند تحریری.
/// </summary>
public sealed record BrandReference(Guid BrandId, string? SlugSeam, CatalogPublicationStatus Status);
