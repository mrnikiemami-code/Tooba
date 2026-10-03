using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>پیوند دستهٔ محصول با مسیر انسانی.</summary>
public sealed record ProductCategoryAssignmentInfo(
    Guid CategoryId,
    string CategoryPath,
    ProductCategoryAssignmentRole Role);
