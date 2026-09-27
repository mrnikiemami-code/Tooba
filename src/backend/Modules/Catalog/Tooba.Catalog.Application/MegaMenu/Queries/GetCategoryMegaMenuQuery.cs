using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.MegaMenu.Queries;

/// <summary>GET /v1/admin/catalog/categories/{categoryId}/mega-menu</summary>
public sealed record GetCategoryMegaMenuQuery(Guid CategoryId, string? Locale)
    : IRequest<Result<CategoryMegaMenuConfigurationView>>;
