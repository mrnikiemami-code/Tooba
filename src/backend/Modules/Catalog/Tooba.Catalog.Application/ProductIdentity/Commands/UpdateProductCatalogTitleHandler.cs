using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductIdentity.Ports;

namespace Tooba.Catalog.Application.ProductIdentity.Commands;

/// <summary>Handles <see cref="UpdateProductCatalogTitleCommand"/>.</summary>
public sealed class UpdateProductCatalogTitleHandler
    : IRequestHandler<UpdateProductCatalogTitleCommand, Result>
{
    private readonly IProductIdentityDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public UpdateProductCatalogTitleHandler(IProductIdentityDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<Result> Handle(
        UpdateProductCatalogTitleCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Model);
        return _directory.UpdateCatalogTitleAsync(request.ProductId, request.Model, cancellationToken);
    }
}
