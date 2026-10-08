using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts.Ports;
using Tooba.ProductWorkspace.Application.Composition;

namespace Tooba.ProductWorkspace.Application.Composition.Commands;

/// <summary>Patches one product variant through the Catalog Contracts mutation boundary.</summary>
public sealed record PatchWorkspaceProductVariantCommand(
    Guid ProductId,
    Guid VariantId,
    CatalogAdminProductWorkspaceActor Actor,
    CatalogAdminProductWorkspaceVariantPatchRequest Model) : IRequest<Result>;

/// <summary>Handles <see cref="PatchWorkspaceProductVariantCommand"/>.</summary>
public sealed class PatchWorkspaceProductVariantCommandHandler(
    ICatalogAdminProductWorkspaceMutationGateway catalog)
    : IRequestHandler<PatchWorkspaceProductVariantCommand, Result>
{
    /// <inheritdoc />
    public Task<Result> Handle(
        PatchWorkspaceProductVariantCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ProductWorkspaceOperation.ExecuteAsync(
            () => catalog.PatchVariantAsync(
                request.ProductId,
                request.VariantId,
                request.Actor,
                request.Model,
                cancellationToken));
    }
}
