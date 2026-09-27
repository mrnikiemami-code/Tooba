using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.MegaMenu.Commands;

/// <summary>PUT /v1/admin/catalog/categories/{categoryId}/mega-menu</summary>
public sealed record UpsertCategoryMegaMenuCommand(
    Guid CategoryId,
    string? Locale,
    CategoryMegaMenuBindingInput Input) : IRequest<Result>;
