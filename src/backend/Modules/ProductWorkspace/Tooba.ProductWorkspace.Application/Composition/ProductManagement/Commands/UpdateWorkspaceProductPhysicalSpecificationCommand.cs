using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts.Ports;
using Tooba.ProductWorkspace.Application.Composition;

namespace Tooba.ProductWorkspace.Application.Composition.ProductManagement.Commands;

/// <summary>Updates product physical specification through the Catalog Contracts mutation boundary.</summary>
public sealed record UpdateWorkspaceProductPhysicalSpecificationCommand(
    Guid ProductId,
    CatalogAdminProductWorkspaceActor Actor,
    CatalogAdminProductWorkspacePhysicalSpecificationRequest Model) : IRequest<Result>;

/// <summary>Handles <see cref="UpdateWorkspaceProductPhysicalSpecificationCommand"/>.</summary>
public sealed class UpdateWorkspaceProductPhysicalSpecificationCommandHandler(
    ICatalogAdminProductWorkspaceMutationGateway catalog)
    : IRequestHandler<UpdateWorkspaceProductPhysicalSpecificationCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(
        UpdateWorkspaceProductPhysicalSpecificationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ProductWorkspaceOperation.ExecuteAsync(
            () => catalog.UpdatePhysicalSpecificationAsync(
                request.ProductId,
                request.Actor,
                request.Model,
                cancellationToken));
    }
}
