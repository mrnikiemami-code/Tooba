using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>
/// مرجع گونهٔ Catalog. هویت Offer فروشنده نیست.
/// </summary>
public sealed record VariantReference(Guid VariantId, Guid ProductId, string CombinationFingerprint, CatalogPublicationStatus Status);
