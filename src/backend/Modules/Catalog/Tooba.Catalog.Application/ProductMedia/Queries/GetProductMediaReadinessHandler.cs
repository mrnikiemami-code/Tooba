using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductMedia.Models;
using Tooba.Catalog.Application.ProductMedia.Ports;

namespace Tooba.Catalog.Application.ProductMedia.Queries;

/// <summary>Handles GetProductMediaReadinessQuery.</summary>
public sealed class GetProductMediaReadinessHandler
    : IRequestHandler<GetProductMediaReadinessQuery, Result<ProductMediaReadinessView>>
{
    private readonly IProductMediaDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public GetProductMediaReadinessHandler(IProductMediaDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public async Task<Result<ProductMediaReadinessView>> Handle(
        GetProductMediaReadinessQuery request,
        CancellationToken cancellationToken)
    {
        var readiness = await _directory.GetReadinessAsync(request.ProductId, cancellationToken);
        if (readiness.IsFailure)
        {
            return Result.Failure<ProductMediaReadinessView>(readiness.Errors);
        }

        var r = readiness.Value;
        return Result.Success(new ProductMediaReadinessView(
            r.HasPrimaryImage,
            r.MediaCount,
            r.IsReady,
            r.MessageFa));
    }
}
