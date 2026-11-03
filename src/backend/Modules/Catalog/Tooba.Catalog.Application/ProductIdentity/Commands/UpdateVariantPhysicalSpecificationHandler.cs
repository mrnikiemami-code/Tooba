using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductIdentity.Ports;

namespace Tooba.Catalog.Application.ProductIdentity.Commands;

/// <summary>Handles <see cref="UpdateVariantPhysicalSpecificationCommand"/>.</summary>
public sealed class UpdateVariantPhysicalSpecificationHandler
    : IRequestHandler<UpdateVariantPhysicalSpecificationCommand, Result>
{
    private readonly IProductIdentityDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public UpdateVariantPhysicalSpecificationHandler(IProductIdentityDirectory directory)
        => _directory = directory;

    /// <inheritdoc />
    public Task<Result> Handle(
        UpdateVariantPhysicalSpecificationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Model);
        return _directory.UpdateVariantPhysicalSpecificationAsync(
            request.ProductId,
            request.VariantId,
            request.Model,
            cancellationToken);
    }
}
