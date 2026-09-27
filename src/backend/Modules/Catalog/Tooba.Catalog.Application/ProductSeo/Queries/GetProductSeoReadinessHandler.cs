using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductSeo.Models;
using Tooba.Catalog.Application.ProductSeo.Ports;

namespace Tooba.Catalog.Application.ProductSeo.Queries;

/// <summary>Handles GetProductSeoReadinessQuery.</summary>
public sealed class GetProductSeoReadinessHandler
    : IRequestHandler<GetProductSeoReadinessQuery, Result<ProductSeoReadinessView>>
{
    private readonly IProductSeoDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public GetProductSeoReadinessHandler(IProductSeoDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public async Task<Result<ProductSeoReadinessView>> Handle(
        GetProductSeoReadinessQuery request,
        CancellationToken cancellationToken)
    {
        var readiness = await _directory.GetReadinessAsync(
            request.ProductId,
            request.Locale,
            cancellationToken);
        if (readiness.IsFailure)
        {
            return Result.Failure<ProductSeoReadinessView>(readiness.Errors);
        }

        return Result.Success(GetProductSeoHandler.MapReadiness(readiness.Value));
    }
}
