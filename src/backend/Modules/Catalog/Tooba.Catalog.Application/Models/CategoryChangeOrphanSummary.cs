using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>خلاصهٔ یک مقدار یتیم پس از تغییر رده.</summary>
public sealed record CategoryChangeOrphanSummary(
    Guid DefinitionId,
    string LocalizedName,
    string DisplayValue);
