using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductTaxonomy.Models;

namespace Tooba.Catalog.Application.ProductTaxonomy.Commands;

/// <summary>Assigns or replaces the product primary category.</summary>
public sealed record AssignProductCategoryCommand(
    Guid ProductId,
    WorkspaceProductCategoryAssignWriteModel Model) : IRequest<Result>;
