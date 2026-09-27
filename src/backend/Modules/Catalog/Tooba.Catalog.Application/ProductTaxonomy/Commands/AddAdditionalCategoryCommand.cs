using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductTaxonomy.Models;

namespace Tooba.Catalog.Application.ProductTaxonomy.Commands;

/// <summary>Adds an additional category to a product.</summary>
public sealed record AddAdditionalCategoryCommand(
    Guid ProductId,
    WorkspaceProductAdditionalCategoryWriteModel Model) : IRequest<Result>;
