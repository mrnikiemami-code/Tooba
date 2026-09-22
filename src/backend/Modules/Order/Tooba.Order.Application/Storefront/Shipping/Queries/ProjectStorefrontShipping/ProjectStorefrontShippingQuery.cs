using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Services;

namespace Tooba.Order.Application.Storefront.Shipping.Queries.ProjectStorefrontShipping;

public sealed record ProjectStorefrontShippingQuery(
    Guid CartId,
    string? GuestSecret,
    string? ProvinceName,
    string? MethodCode,
    string? Language) : IRequest<Result<StorefrontShippingProjection>>;

public sealed class ProjectStorefrontShippingHandler(StorefrontShippingService shipping)
    : IRequestHandler<ProjectStorefrontShippingQuery, Result<StorefrontShippingProjection>>
{
    public Task<Result<StorefrontShippingProjection>> Handle(ProjectStorefrontShippingQuery request, CancellationToken cancellationToken)
        => StorefrontOrderResult.ExecuteAsync(() => shipping.ProjectAsync(
            request.CartId, request.GuestSecret, request.ProvinceName, request.MethodCode, request.Language, cancellationToken));
}
