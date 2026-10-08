using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts.Ports;
using Tooba.ProductWorkspace.Application.Composition;

namespace Tooba.ProductWorkspace.Application.Composition.Commands;

/// <summary>Publishes a workspace product through the Catalog Contracts mutation boundary.</summary>
public sealed record PublishWorkspaceProductCommand(
    Guid ProductId,
    CatalogAdminProductWorkspaceActor Actor) : IRequest<Result>;

/// <summary>Handles <see cref="PublishWorkspaceProductCommand"/>.</summary>
public sealed class PublishWorkspaceProductCommandHandler(
    ICatalogAdminProductWorkspaceMutationGateway catalog)
    : IRequestHandler<PublishWorkspaceProductCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(
        PublishWorkspaceProductCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ProductWorkspaceOperation.ExecuteAsync(
            () => catalog.PublishAsync(request.ProductId, request.Actor, cancellationToken));
    }
}
