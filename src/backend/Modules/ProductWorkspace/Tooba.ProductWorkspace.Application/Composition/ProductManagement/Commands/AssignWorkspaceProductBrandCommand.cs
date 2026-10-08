using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts.Ports;
using Tooba.ProductWorkspace.Application.Composition;

namespace Tooba.ProductWorkspace.Application.Composition.ProductManagement.Commands;

/// <summary>Assigns or clears the product brand through the Catalog Contracts mutation boundary.</summary>
public sealed record AssignWorkspaceProductBrandCommand(
    Guid ProductId,
    CatalogAdminProductWorkspaceActor Actor,
    CatalogAdminProductWorkspaceBrandRequest Model) : IRequest<Result>;

/// <summary>Handles <see cref="AssignWorkspaceProductBrandCommand"/>.</summary>
public sealed class AssignWorkspaceProductBrandCommandHandler(
    ICatalogAdminProductWorkspaceMutationGateway catalog)
    : IRequestHandler<AssignWorkspaceProductBrandCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(
        AssignWorkspaceProductBrandCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ProductWorkspaceOperation.ExecuteAsync(
            () => catalog.AssignBrandAsync(request.ProductId, request.Actor, request.Model, cancellationToken));
    }
}
