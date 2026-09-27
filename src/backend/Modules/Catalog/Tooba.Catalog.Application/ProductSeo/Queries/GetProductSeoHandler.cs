using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.ProductSeo.Models;
using Tooba.Catalog.Application.ProductSeo.Ports;

namespace Tooba.Catalog.Application.ProductSeo.Queries;

/// <summary>Handles GetProductSeoQuery.</summary>
public sealed class GetProductSeoHandler
    : IRequestHandler<GetProductSeoQuery, Result<ProductSeoDetailView>>
{
    private readonly IProductSeoDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public GetProductSeoHandler(IProductSeoDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public async Task<Result<ProductSeoDetailView>> Handle(
        GetProductSeoQuery request,
        CancellationToken cancellationToken)
    {
        var detail = await _directory.GetAsync(request.ProductId, request.Locale, cancellationToken);
        if (detail.IsFailure)
        {
            return Result.Failure<ProductSeoDetailView>(detail.Errors);
        }

        return Result.Success(Map(detail.Value));
    }

    internal static ProductSeoDetailView Map(ProductSeoDetail detail) =>
        new(
            detail.ProductId,
            detail.Locale,
            detail.Slug,
            detail.SeoTitle,
            detail.SeoDescription,
            detail.ProductName,
            detail.TitleFallback,
            detail.PublicPath,
            MapReadiness(detail.Readiness),
            detail.UpdatedAt);

    internal static ProductSeoReadinessView MapReadiness(ProductSeoReadiness readiness) =>
        new(
            readiness.HasValidSlug,
            readiness.HasSeoTitleOrFallback,
            readiness.HasSeoDescription,
            readiness.HasLocalizedIdentity,
            readiness.IsReady,
            readiness.MessageFa);
}
