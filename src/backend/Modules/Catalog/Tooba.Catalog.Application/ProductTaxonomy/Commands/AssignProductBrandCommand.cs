using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductTaxonomy.Models;

namespace Tooba.Catalog.Application.ProductTaxonomy.Commands;

/// <summary>Assigns or clears the product brand.</summary>
public sealed record AssignProductBrandCommand(
    Guid ProductId,
    WorkspaceProductBrandAssignWriteModel Model) : IRequest<Result>;
