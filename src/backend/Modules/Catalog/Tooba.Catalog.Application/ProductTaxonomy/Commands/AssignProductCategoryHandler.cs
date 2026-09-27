using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductTaxonomy.Ports;

namespace Tooba.Catalog.Application.ProductTaxonomy.Commands;

/// <summary>Handles <see cref="AssignProductCategoryCommand"/>.</summary>
public sealed class AssignProductCategoryHandler : IRequestHandler<AssignProductCategoryCommand, Result>
{
    private readonly IProductTaxonomyDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public AssignProductCategoryHandler(IProductTaxonomyDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<Result> Handle(AssignProductCategoryCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Model);
        return _directory.AssignPrimaryCategoryAsync(request.ProductId, request.Model, cancellationToken);
    }
}
