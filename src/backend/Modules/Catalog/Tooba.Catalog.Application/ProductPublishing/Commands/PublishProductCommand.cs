using MediatR;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Catalog.Application.ProductPublishing.Commands;

/// <summary>Publishes a catalog product (lifecycle).</summary>
public sealed record PublishProductCommand(Guid ProductId) : IRequest<Result>;
