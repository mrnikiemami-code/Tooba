using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.Attributes.ProductValues.Queries;

/// <summary>Reads product attribute readiness.</summary>
public sealed record GetProductAttributeReadinessQuery(Guid ProductId)
    : IRequest<Result<ProductAttributeReadiness>>;
