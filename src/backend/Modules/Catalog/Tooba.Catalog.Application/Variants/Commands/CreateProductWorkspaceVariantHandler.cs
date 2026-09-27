using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Variants.Ports;
using Tooba.Catalog.Contracts.Errors;

namespace Tooba.Catalog.Application.Variants.Commands;

/// <summary>Handles <see cref="CreateProductWorkspaceVariantCommand"/>.</summary>
public sealed class CreateProductWorkspaceVariantHandler
    : IRequestHandler<CreateProductWorkspaceVariantCommand, Result>
{
    private readonly IProductVariantDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public CreateProductWorkspaceVariantHandler(IProductVariantDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public async Task<Result> Handle(
        CreateProductWorkspaceVariantCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Model);
        var axes = request.Model.Axes;
        if (axes is null || axes.Count == 0)
        {
            return Result.Failure(new SemanticError(CatalogErrorCodes.WorkspaceVariantAxesMissing));
        }

        var created = await _directory.CreateWorkspaceVariantAsync(
            request.ProductId,
            request.Model.CatalogCodeSeam,
            axes.Select(a => (a.DefinitionId, a.RawValue ?? string.Empty, a.EnumOptionId)).ToList(),
            cancellationToken);
        return created.IsFailure
            ? Result.Failure(created.Errors)
            : Result.Success();
    }
}
