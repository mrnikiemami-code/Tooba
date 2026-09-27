using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductPublishing.Ports;

namespace Tooba.Catalog.Application.ProductPublishing.Commands;

/// <summary>Handles <see cref="PublishProductCommand"/>.</summary>
public sealed class PublishProductHandler : IRequestHandler<PublishProductCommand, Result>
{
    private readonly IProductLifecycleDirectory _lifecycle;

    /// <summary>Creates the handler.</summary>
    public PublishProductHandler(IProductLifecycleDirectory lifecycle) => _lifecycle = lifecycle;

    /// <inheritdoc />
    public Task<Result> Handle(PublishProductCommand request, CancellationToken cancellationToken) =>
        _lifecycle.PublishAsync(request.ProductId, cancellationToken);
}
