using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.MegaMenu.Queries;

/// <summary>GET /v1/storefront/mega-menu</summary>
public sealed record GetStorefrontMegaMenuQuery(string? Locale)
    : IRequest<Result<IReadOnlyList<StorefrontMegaMenuItem>>>;
