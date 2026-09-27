using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductTaxonomy.Ports;

namespace Tooba.Catalog.Application.ProductTaxonomy.Commands;

/// <summary>Handles <see cref="AssignProductBrandCommand"/>.</summary>
public sealed class AssignProductBrandHandler : IRequestHandler<AssignProductBrandCommand, Result>
{
    private readonly IProductTaxonomyDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public AssignProductBrandHandler(IProductTaxonomyDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<Result> Handle(AssignProductBrandCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Model);
        return _directory.AssignBrandAsync(request.ProductId, request.Model, cancellationToken);
    }
}
