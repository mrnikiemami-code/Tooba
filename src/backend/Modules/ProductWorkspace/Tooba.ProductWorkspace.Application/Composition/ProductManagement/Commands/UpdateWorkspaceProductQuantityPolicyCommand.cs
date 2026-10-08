using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts.Ports;
using Tooba.ProductWorkspace.Application.Composition;

namespace Tooba.ProductWorkspace.Application.Composition.ProductManagement.Commands;

/// <summary>Updates product quantity policy through the Catalog Contracts mutation boundary.</summary>
public sealed record UpdateWorkspaceProductQuantityPolicyCommand(
    Guid ProductId,
    CatalogAdminProductWorkspaceActor Actor,
    CatalogAdminProductWorkspaceQuantityPolicyRequest Model) : IRequest<Result>;

/// <summary>Handles <see cref="UpdateWorkspaceProductQuantityPolicyCommand"/>.</summary>
public sealed class UpdateWorkspaceProductQuantityPolicyCommandHandler(
    ICatalogAdminProductWorkspaceMutationGateway catalog)
    : IRequestHandler<UpdateWorkspaceProductQuantityPolicyCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(
        UpdateWorkspaceProductQuantityPolicyCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ProductWorkspaceOperation.ExecuteAsync(
            () => catalog.UpdateQuantityPolicyAsync(request.ProductId, request.Actor, request.Model, cancellationToken));
    }
}
