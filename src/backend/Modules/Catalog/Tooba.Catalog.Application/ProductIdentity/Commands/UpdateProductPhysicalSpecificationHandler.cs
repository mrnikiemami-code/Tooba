using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductIdentity.Ports;

namespace Tooba.Catalog.Application.ProductIdentity.Commands;

/// <summary>Handles <see cref="UpdateProductPhysicalSpecificationCommand"/>.</summary>
public sealed class UpdateProductPhysicalSpecificationHandler
    : IRequestHandler<UpdateProductPhysicalSpecificationCommand, Result>
{
    private readonly IProductIdentityDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public UpdateProductPhysicalSpecificationHandler(IProductIdentityDirectory directory)
        => _directory = directory;

    /// <inheritdoc />
    public Task<Result> Handle(
        UpdateProductPhysicalSpecificationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Model);
        return _directory.UpdatePhysicalSpecificationAsync(
            request.ProductId,
            request.Model,
            cancellationToken);
    }
}
