using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Categories.Ports;

namespace Tooba.Catalog.Application.Categories.Commands;

/// <summary>Creates a Catalog category.</summary>
public sealed class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, Result<CategoryReference>>
{
    private readonly ICategoryDirectory _categories;

    /// <summary>Creates the handler.</summary>
    public CreateCategoryHandler(ICategoryDirectory categories) => _categories = categories;

    /// <inheritdoc />
    public Task<Result<CategoryReference>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken) =>
        _categories.CreateAsync(request.Model, cancellationToken);
}
