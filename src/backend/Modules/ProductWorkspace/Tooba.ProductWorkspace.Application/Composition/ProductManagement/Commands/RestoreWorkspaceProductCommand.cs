using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts.Ports;
using Tooba.ProductWorkspace.Application.Composition;

namespace Tooba.ProductWorkspace.Application.Composition.ProductManagement.Commands;

/// <summary>Restores an archived workspace product to draft through the Catalog Contracts boundary.</summary>
public sealed record RestoreWorkspaceProductCommand(
    Guid ProductId,
    CatalogAdminProductWorkspaceActor Actor) : IRequest<Result>;

/// <summary>Handles <see cref="RestoreWorkspaceProductCommand"/>.</summary>
public sealed class RestoreWorkspaceProductCommandHandler(
    ICatalogAdminProductWorkspaceMutationGateway catalog)
    : IRequestHandler<RestoreWorkspaceProductCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(
        RestoreWorkspaceProductCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ProductWorkspaceOperation.ExecuteAsync(
            () => catalog.RestoreAsync(request.ProductId, request.Actor, cancellationToken));
    }
}
