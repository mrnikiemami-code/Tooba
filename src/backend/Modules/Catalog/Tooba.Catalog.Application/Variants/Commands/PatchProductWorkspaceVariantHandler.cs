using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Variants.Ports;

namespace Tooba.Catalog.Application.Variants.Commands;

/// <summary>Handles <see cref="PatchProductWorkspaceVariantCommand"/>.</summary>
public sealed class PatchProductWorkspaceVariantHandler
    : IRequestHandler<PatchProductWorkspaceVariantCommand, Result>
{
    private readonly IProductVariantDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public PatchProductWorkspaceVariantHandler(IProductVariantDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<Result> Handle(
        PatchProductWorkspaceVariantCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Model);
        return _directory.PatchWorkspaceVariantAsync(
            request.ProductId,
            request.VariantId,
            request.Model.Status,
            request.Model.CatalogCodeSeam,
            cancellationToken);
    }
}
