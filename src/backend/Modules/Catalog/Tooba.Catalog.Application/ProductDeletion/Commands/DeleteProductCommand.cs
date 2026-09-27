using MediatR;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Catalog.Application.ProductDeletion.Commands;

/// <summary>Deletes a catalog product or soft-archives when Offer-referenced.</summary>
public sealed record DeleteProductCommand(Guid ProductId) : IRequest<Result>;
