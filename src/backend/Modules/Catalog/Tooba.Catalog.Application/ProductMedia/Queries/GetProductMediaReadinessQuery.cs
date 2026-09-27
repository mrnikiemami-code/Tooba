using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductMedia.Models;

namespace Tooba.Catalog.Application.ProductMedia.Queries;

/// <summary>Get product media gallery readiness.</summary>
public sealed record GetProductMediaReadinessQuery(Guid ProductId)
    : IRequest<Result<ProductMediaReadinessView>>;
