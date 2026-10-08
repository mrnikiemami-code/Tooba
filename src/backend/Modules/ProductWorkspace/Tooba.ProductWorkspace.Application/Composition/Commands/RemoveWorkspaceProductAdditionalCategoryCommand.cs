using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts.Ports;
using Tooba.ProductWorkspace.Application.Composition;

namespace Tooba.ProductWorkspace.Application.Composition.Commands;

/// <summary>Removes an additional category link through the Catalog Contracts mutation boundary.</summary>
public sealed record RemoveWorkspaceProductAdditionalCategoryCommand(
    Guid ProductId,
    Guid CategoryId,
    CatalogAdminProductWorkspaceActor Actor,
    DateTimeOffset ExpectedUpdatedAt) : IRequest<Result>;

/// <summary>Handles <see cref="RemoveWorkspaceProductAdditionalCategoryCommand"/>.</summary>
public sealed class RemoveWorkspaceProductAdditionalCategoryCommandHandler(
    ICatalogAdminProductWorkspaceMutationGateway catalog)
    : IRequestHandler<RemoveWorkspaceProductAdditionalCategoryCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(
        RemoveWorkspaceProductAdditionalCategoryCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ProductWorkspaceOperation.ExecuteAsync(
            () => catalog.RemoveAdditionalCategoryAsync(
                request.ProductId,
                request.CategoryId,
                request.Actor,
                request.ExpectedUpdatedAt,
                cancellationToken));
    }
}
