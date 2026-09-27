using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductPublishing.Ports;

namespace Tooba.Catalog.Application.ProductPublishing.Commands;

/// <summary>Handles <see cref="RestoreProductCommand"/>.</summary>
public sealed class RestoreProductHandler : IRequestHandler<RestoreProductCommand, Result>
{
    private readonly IProductLifecycleDirectory _lifecycle;

    /// <summary>Creates the handler.</summary>
    public RestoreProductHandler(IProductLifecycleDirectory lifecycle) => _lifecycle = lifecycle;

    /// <inheritdoc />
    public Task<Result> Handle(RestoreProductCommand request, CancellationToken cancellationToken) =>
        _lifecycle.RestoreAsync(request.ProductId, cancellationToken);
}
