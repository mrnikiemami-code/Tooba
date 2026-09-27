using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Variants.Ports;

namespace Tooba.Catalog.Application.Variants.Queries;

/// <summary>Handles GetProductVariantReadinessQuery.</summary>
public sealed class GetProductVariantReadinessHandler
    : IRequestHandler<GetProductVariantReadinessQuery, Result<ProductVariantReadiness>>
{
    private readonly IProductVariantDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public GetProductVariantReadinessHandler(IProductVariantDirectory directory) =>
        _directory = directory;

    /// <inheritdoc />
    public Task<Result<ProductVariantReadiness>> Handle(
        GetProductVariantReadinessQuery request,
        CancellationToken cancellationToken) =>
        _directory.GetReadinessAsync(request.ProductId, cancellationToken);
}
