using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts.Ports;
using Tooba.ProductWorkspace.Application.Composition;

namespace Tooba.ProductWorkspace.Application.Composition.ProductManagement.Commands;

/// <summary>Updates a localized catalog product title through the Catalog Contracts mutation boundary.</summary>
public sealed record UpdateWorkspaceProductCatalogTitleCommand(
    Guid ProductId,
    CatalogAdminProductWorkspaceActor Actor,
    CatalogAdminProductWorkspaceCatalogTitleRequest Model) : IRequest<Result>;

/// <summary>Handles <see cref="UpdateWorkspaceProductCatalogTitleCommand"/>.</summary>
public sealed class UpdateWorkspaceProductCatalogTitleCommandHandler(
    ICatalogAdminProductWorkspaceMutationGateway catalog)
    : IRequestHandler<UpdateWorkspaceProductCatalogTitleCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(
        UpdateWorkspaceProductCatalogTitleCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ProductWorkspaceOperation.ExecuteAsync(
            () => catalog.UpdateCatalogTitleAsync(request.ProductId, request.Actor, request.Model, cancellationToken));
    }
}
