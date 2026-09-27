using MediatR;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Catalog.Application.ProductTaxonomy.Commands;

/// <summary>Removes an additional category from a product.</summary>
public sealed record RemoveAdditionalCategoryCommand(
    Guid ProductId,
    Guid CategoryId,
    DateTimeOffset ExpectedUpdatedAt) : IRequest<Result>;
