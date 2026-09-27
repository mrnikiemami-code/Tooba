using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductDeletion.Ports;

namespace Tooba.Catalog.Application.ProductDeletion.Commands;

/// <summary>Handles <see cref="DeleteProductCommand"/>.</summary>
public sealed class DeleteProductHandler : IRequestHandler<DeleteProductCommand, Result>
{
    private readonly IProductDeletionDirectory _deletion;

    /// <summary>Creates the handler.</summary>
    public DeleteProductHandler(IProductDeletionDirectory deletion) => _deletion = deletion;

    /// <inheritdoc />
    public Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken) =>
        _deletion.DeleteOrSoftArchiveAsync(request.ProductId, cancellationToken);
}
