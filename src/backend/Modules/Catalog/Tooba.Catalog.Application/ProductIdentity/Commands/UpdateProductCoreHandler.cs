using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductIdentity.Ports;

namespace Tooba.Catalog.Application.ProductIdentity.Commands;

/// <summary>Handles <see cref="UpdateProductCoreCommand"/>.</summary>
public sealed class UpdateProductCoreHandler : IRequestHandler<UpdateProductCoreCommand, Result>
{
    private readonly IProductIdentityDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public UpdateProductCoreHandler(IProductIdentityDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<Result> Handle(
        UpdateProductCoreCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Model);
        return _directory.UpdateProductCoreAsync(request.ProductId, request.Model, cancellationToken);
    }
}
