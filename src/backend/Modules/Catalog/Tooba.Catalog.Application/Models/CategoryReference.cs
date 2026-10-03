using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>
/// مرجع ردهٔ طبقه‌بندی.
/// </summary>
public sealed record CategoryReference(Guid CategoryId, Guid? ParentCategoryId, CatalogPublicationStatus Status);
