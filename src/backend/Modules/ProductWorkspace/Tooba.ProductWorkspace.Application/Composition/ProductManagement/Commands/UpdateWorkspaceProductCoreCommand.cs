using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts.Ports;
using Tooba.ProductWorkspace.Application.Composition;

namespace Tooba.ProductWorkspace.Application.Composition.ProductManagement.Commands;

/// <summary>Updates product core identity and locale translations through the Catalog Contracts boundary.</summary>
public sealed record UpdateWorkspaceProductCoreCommand(
    Guid ProductId,
    CatalogAdminProductWorkspaceActor Actor,
    CatalogAdminProductWorkspaceCoreRequest Model) : IRequest<Result>;

/// <summary>Handles <see cref="UpdateWorkspaceProductCoreCommand"/>.</summary>
public sealed class UpdateWorkspaceProductCoreCommandHandler(
    ICatalogAdminProductWorkspaceMutationGateway catalog)
    : IRequestHandler<UpdateWorkspaceProductCoreCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(
        UpdateWorkspaceProductCoreCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ProductWorkspaceOperation.ExecuteAsync(
            () => catalog.UpdateCoreAsync(request.ProductId, request.Actor, request.Model, cancellationToken));
    }
}
