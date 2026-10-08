using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts.Ports;
using Tooba.ProductWorkspace.Application.Composition;

namespace Tooba.ProductWorkspace.Application.Composition.ProductManagement.Commands;

/// <summary>Assigns or replaces the product primary category through the Catalog Contracts boundary.</summary>
public sealed record AssignWorkspaceProductCategoryCommand(
    Guid ProductId,
    CatalogAdminProductWorkspaceActor Actor,
    CatalogAdminProductWorkspaceCategoryRequest Model) : IRequest<Result>;

/// <summary>Handles <see cref="AssignWorkspaceProductCategoryCommand"/>.</summary>
public sealed class AssignWorkspaceProductCategoryCommandHandler(
    ICatalogAdminProductWorkspaceMutationGateway catalog)
    : IRequestHandler<AssignWorkspaceProductCategoryCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(
        AssignWorkspaceProductCategoryCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ProductWorkspaceOperation.ExecuteAsync(
            () => catalog.AssignPrimaryCategoryAsync(request.ProductId, request.Actor, request.Model, cancellationToken));
    }
}
