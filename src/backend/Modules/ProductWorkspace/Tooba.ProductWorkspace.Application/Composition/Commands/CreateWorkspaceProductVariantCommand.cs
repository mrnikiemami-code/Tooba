using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts.Ports;
using Tooba.ProductWorkspace.Application.Composition;

namespace Tooba.ProductWorkspace.Application.Composition.Commands;

/// <summary>Creates one product variant through the Catalog Contracts mutation boundary.</summary>
public sealed record CreateWorkspaceProductVariantCommand(
    Guid ProductId,
    CatalogAdminProductWorkspaceActor Actor,
    CatalogAdminProductWorkspaceVariantCreateRequest Model) : IRequest<Result>;

/// <summary>Handles <see cref="CreateWorkspaceProductVariantCommand"/>.</summary>
public sealed class CreateWorkspaceProductVariantCommandHandler(
    ICatalogAdminProductWorkspaceMutationGateway catalog)
    : IRequestHandler<CreateWorkspaceProductVariantCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(
        CreateWorkspaceProductVariantCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ProductWorkspaceOperation.ExecuteAsync(
            () => catalog.CreateVariantAsync(request.ProductId, request.Actor, request.Model, cancellationToken));
    }
}
