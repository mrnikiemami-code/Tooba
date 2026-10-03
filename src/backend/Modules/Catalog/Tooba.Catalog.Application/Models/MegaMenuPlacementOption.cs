using Tooba.BuildingBlocks;
using Tooba.Catalog.Domain;

namespace Tooba.Catalog.Application.Models;

/// <summary>گزینهٔ والد presentation با مسیر انسانی.</summary>
public sealed record MegaMenuPlacementOption(
    Guid MegaMenuItemId,
    Guid CategoryId,
    string Label,
    string MenuPath,
    int Level);
