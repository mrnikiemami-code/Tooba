using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductTaxonomy.Ports;

namespace Tooba.Catalog.Application.ProductTaxonomy.Commands;

/// <summary>Handles <see cref="AddAdditionalCategoryCommand"/>.</summary>
public sealed class AddAdditionalCategoryHandler : IRequestHandler<AddAdditionalCategoryCommand, Result>
{
    private readonly IProductTaxonomyDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public AddAdditionalCategoryHandler(IProductTaxonomyDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<Result> Handle(AddAdditionalCategoryCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Model);
        return _directory.AddAdditionalCategoryAsync(request.ProductId, request.Model, cancellationToken);
    }
}
