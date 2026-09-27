using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Attributes.ProductValues.Ports;

namespace Tooba.Catalog.Application.Attributes.ProductValues.Queries;

/// <summary>Handles GetProductAttributeReadinessQuery.</summary>
public sealed class GetProductAttributeReadinessHandler
    : IRequestHandler<GetProductAttributeReadinessQuery, Result<ProductAttributeReadiness>>
{
    private readonly IProductAttributeDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public GetProductAttributeReadinessHandler(IProductAttributeDirectory directory) =>
        _directory = directory;

    /// <inheritdoc />
    public Task<Result<ProductAttributeReadiness>> Handle(
        GetProductAttributeReadinessQuery request,
        CancellationToken cancellationToken) =>
        _directory.GetReadinessAsync(request.ProductId, cancellationToken);
}
