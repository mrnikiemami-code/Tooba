using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.Variants.Queries;

/// <summary>Loads product variant readiness.</summary>
public sealed record GetProductVariantReadinessQuery(Guid ProductId)
    : IRequest<Result<ProductVariantReadiness>>;
