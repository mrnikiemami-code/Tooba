using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts.Ports;
using Tooba.ProductWorkspace.Application.Composition;

namespace Tooba.ProductWorkspace.Application.Composition.Commands;

/// <summary>Unpublishes a workspace product to draft through the Catalog Contracts boundary.</summary>
public sealed record UnpublishWorkspaceProductCommand(
    Guid ProductId,
    CatalogAdminProductWorkspaceActor Actor) : IRequest<Result>;

/// <summary>Handles <see cref="UnpublishWorkspaceProductCommand"/>.</summary>
public sealed class UnpublishWorkspaceProductCommandHandler(
    ICatalogAdminProductWorkspaceMutationGateway catalog)
    : IRequestHandler<UnpublishWorkspaceProductCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(
        UnpublishWorkspaceProductCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ProductWorkspaceOperation.ExecuteAsync(
            () => catalog.UnpublishAsync(request.ProductId, request.Actor, cancellationToken));
    }
}
