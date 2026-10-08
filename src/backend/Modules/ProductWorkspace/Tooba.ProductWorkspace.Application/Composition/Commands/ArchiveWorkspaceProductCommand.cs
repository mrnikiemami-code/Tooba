using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts.Ports;
using Tooba.ProductWorkspace.Application.Composition;

namespace Tooba.ProductWorkspace.Application.Composition.Commands;

/// <summary>Archives a workspace product through the Catalog Contracts mutation boundary.</summary>
public sealed record ArchiveWorkspaceProductCommand(
    Guid ProductId,
    CatalogAdminProductWorkspaceActor Actor) : IRequest<Result>;

/// <summary>Handles <see cref="ArchiveWorkspaceProductCommand"/>.</summary>
public sealed class ArchiveWorkspaceProductCommandHandler(
    ICatalogAdminProductWorkspaceMutationGateway catalog)
    : IRequestHandler<ArchiveWorkspaceProductCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(
        ArchiveWorkspaceProductCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ProductWorkspaceOperation.ExecuteAsync(
            () => catalog.ArchiveAsync(request.ProductId, request.Actor, cancellationToken));
    }
}
