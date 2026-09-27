using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.ProductSeo.Models;
using Tooba.Catalog.Application.ProductSeo.Ports;
using Tooba.Catalog.Application.ProductSeo.Queries;

namespace Tooba.Catalog.Application.ProductSeo.Commands;

/// <summary>Handles UpdateProductSeoCommand.</summary>
public sealed class UpdateProductSeoHandler
    : IRequestHandler<UpdateProductSeoCommand, Result<ProductSeoDetailView>>
{
    private readonly IProductSeoDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public UpdateProductSeoHandler(IProductSeoDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public async Task<Result<ProductSeoDetailView>> Handle(
        UpdateProductSeoCommand request,
        CancellationToken cancellationToken)
    {
        var updated = await _directory.UpdateAsync(
            request.ProductId,
            new ProductSeoUpdateInput(
                request.Model.Locale,
                request.Model.Slug,
                request.Model.SeoTitle,
                request.Model.SeoDescription,
                request.Model.ExpectedUpdatedAt),
            cancellationToken);
        if (updated.IsFailure)
        {
            return Result.Failure<ProductSeoDetailView>(updated.Errors);
        }

        return Result.Success(GetProductSeoHandler.Map(updated.Value));
    }
}
