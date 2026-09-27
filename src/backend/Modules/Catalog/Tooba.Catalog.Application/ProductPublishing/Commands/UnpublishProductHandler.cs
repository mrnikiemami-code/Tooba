using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductPublishing.Ports;

namespace Tooba.Catalog.Application.ProductPublishing.Commands;

/// <summary>Handles <see cref="UnpublishProductCommand"/>.</summary>
public sealed class UnpublishProductHandler : IRequestHandler<UnpublishProductCommand, Result>
{
    private readonly IProductLifecycleDirectory _lifecycle;

    /// <summary>Creates the handler.</summary>
    public UnpublishProductHandler(IProductLifecycleDirectory lifecycle) => _lifecycle = lifecycle;

    /// <inheritdoc />
    public Task<Result> Handle(UnpublishProductCommand request, CancellationToken cancellationToken) =>
        _lifecycle.UnpublishAsync(request.ProductId, cancellationToken);
}
