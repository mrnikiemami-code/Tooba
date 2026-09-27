using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductPublishing.Ports;

namespace Tooba.Catalog.Application.ProductPublishing.Commands;

/// <summary>Handles <see cref="ArchiveProductCommand"/>.</summary>
public sealed class ArchiveProductHandler : IRequestHandler<ArchiveProductCommand, Result>
{
    private readonly IProductLifecycleDirectory _lifecycle;

    /// <summary>Creates the handler.</summary>
    public ArchiveProductHandler(IProductLifecycleDirectory lifecycle) => _lifecycle = lifecycle;

    /// <inheritdoc />
    public Task<Result> Handle(ArchiveProductCommand request, CancellationToken cancellationToken) =>
        _lifecycle.ArchiveAsync(request.ProductId, cancellationToken);
}
