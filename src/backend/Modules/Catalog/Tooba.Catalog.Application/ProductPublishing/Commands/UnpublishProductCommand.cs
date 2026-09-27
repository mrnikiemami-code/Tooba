using MediatR;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Catalog.Application.ProductPublishing.Commands;

/// <summary>Unpublishes a catalog product to draft.</summary>
public sealed record UnpublishProductCommand(Guid ProductId) : IRequest<Result>;
