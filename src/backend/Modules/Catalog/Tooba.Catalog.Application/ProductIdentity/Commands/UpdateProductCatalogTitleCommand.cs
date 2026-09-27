using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductIdentity.Models;

namespace Tooba.Catalog.Application.ProductIdentity.Commands;

/// <summary>Updates a localized catalog product title.</summary>
public sealed record UpdateProductCatalogTitleCommand(
    Guid ProductId,
    WorkspaceProductCatalogTitleWriteModel Model) : IRequest<Result>;
