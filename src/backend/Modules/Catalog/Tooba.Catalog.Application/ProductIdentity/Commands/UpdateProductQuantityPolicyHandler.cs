using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductIdentity.Ports;

namespace Tooba.Catalog.Application.ProductIdentity.Commands;

/// <summary>Handles <see cref="UpdateProductQuantityPolicyCommand"/>.</summary>
public sealed class UpdateProductQuantityPolicyHandler
    : IRequestHandler<UpdateProductQuantityPolicyCommand, Result>
{
    private readonly IProductIdentityDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public UpdateProductQuantityPolicyHandler(IProductIdentityDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<Result> Handle(
        UpdateProductQuantityPolicyCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Model);
        return _directory.UpdateQuantityPolicyAsync(request.ProductId, request.Model, cancellationToken);
    }
}
