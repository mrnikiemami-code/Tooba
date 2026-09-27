using MediatR;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Catalog.Application.MegaMenu.Commands;

/// <summary>DELETE /v1/admin/catalog/categories/{categoryId}/mega-menu</summary>
public sealed record RemoveCategoryMegaMenuCommand(Guid CategoryId) : IRequest<Result>;
