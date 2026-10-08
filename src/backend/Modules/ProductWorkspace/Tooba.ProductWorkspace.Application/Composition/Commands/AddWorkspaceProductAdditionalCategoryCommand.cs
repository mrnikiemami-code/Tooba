using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts.Ports;
using Tooba.ProductWorkspace.Application.Composition;

namespace Tooba.ProductWorkspace.Application.Composition.Commands;

/// <summary>Adds an additional category link through the Catalog Contracts mutation boundary.</summary>
public sealed record AddWorkspaceProductAdditionalCategoryCommand(
    Guid ProductId,
    CatalogAdminProductWorkspaceActor Actor,
    CatalogAdminProductWorkspaceAdditionalCategoryRequest Model) : IRequest<Result>;

/// <summary>Handles <see cref="AddWorkspaceProductAdditionalCategoryCommand"/>.</summary>
public sealed class AddWorkspaceProductAdditionalCategoryCommandHandler(
    ICatalogAdminProductWorkspaceMutationGateway catalog)
    : IRequestHandler<AddWorkspaceProductAdditionalCategoryCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(
        AddWorkspaceProductAdditionalCategoryCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ProductWorkspaceOperation.ExecuteAsync(
            () => catalog.AddAdditionalCategoryAsync(request.ProductId, request.Actor, request.Model, cancellationToken));
    }
}
