using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductTaxonomy.Ports;

namespace Tooba.Catalog.Application.ProductTaxonomy.Commands;

/// <summary>Handles <see cref="RemoveAdditionalCategoryCommand"/>.</summary>
public sealed class RemoveAdditionalCategoryHandler : IRequestHandler<RemoveAdditionalCategoryCommand, Result>
{
    private readonly IProductTaxonomyDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public RemoveAdditionalCategoryHandler(IProductTaxonomyDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<Result> Handle(RemoveAdditionalCategoryCommand request, CancellationToken cancellationToken) =>
        _directory.RemoveAdditionalCategoryAsync(
            request.ProductId,
            request.CategoryId,
            request.ExpectedUpdatedAt,
            cancellationToken);
}
