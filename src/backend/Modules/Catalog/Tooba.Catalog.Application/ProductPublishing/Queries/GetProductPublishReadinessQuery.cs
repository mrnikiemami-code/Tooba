using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductPublishing.Models;

namespace Tooba.Catalog.Application.ProductPublishing.Queries;

/// <summary>Admin GET product publish readiness (optional locale).</summary>
public sealed record GetProductPublishReadinessQuery(Guid ProductId, string? Locale)
    : IRequest<Result<ProductPublishReadinessView>>;
