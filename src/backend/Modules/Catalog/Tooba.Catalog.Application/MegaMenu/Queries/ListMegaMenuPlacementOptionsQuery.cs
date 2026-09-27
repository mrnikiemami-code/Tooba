using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.MegaMenu.Queries;

/// <summary>GET /v1/admin/catalog/categories/{categoryId}/mega-menu/placement-options</summary>
public sealed record ListMegaMenuPlacementOptionsQuery(Guid CategoryId, string? Locale)
    : IRequest<Result<IReadOnlyList<MegaMenuPlacementOption>>>;
