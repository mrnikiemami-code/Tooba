using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts.Ports;
using Tooba.ProductWorkspace.Application.Composition;
using Tooba.ProductWorkspace.Contracts.Errors;

namespace Tooba.ProductWorkspace.Application.Composition.ProductManagement.Commands;

/// <summary>Creates a draft workspace product through the Catalog Contracts mutation boundary.</summary>
public sealed record CreateWorkspaceProductCommand(
    CatalogAdminProductWorkspaceActor Actor,
    CatalogAdminProductWorkspaceCreateRequest Model) : IRequest<Result<Guid>>;

/// <summary>Handles <see cref="CreateWorkspaceProductCommand"/>.</summary>
public sealed class CreateWorkspaceProductCommandHandler(
    ICatalogAdminProductWorkspaceMutationGateway catalog)
    : IRequestHandler<CreateWorkspaceProductCommand, Result<Guid>>
{
    /// <inheritdoc />
    public Task<Result<Guid>> Handle(
        CreateWorkspaceProductCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ProductWorkspaceOperation.ExecuteAsync(
            () => catalog.CreateProductAsync(request.Actor, request.Model, cancellationToken));
    }
}
