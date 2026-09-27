using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Categories.Models;
using Tooba.Catalog.Application.Categories.Ports;

namespace Tooba.Catalog.Application.Categories.Commands;

/// <summary>Reorders category siblings.</summary>
public sealed class ReorderCategoriesHandler : IRequestHandler<ReorderCategoriesCommand, Result<CategoryOkResult>>
{
    private readonly ICategoryDirectory _categories;

    /// <summary>Creates the handler.</summary>
    public ReorderCategoriesHandler(ICategoryDirectory categories) => _categories = categories;

    /// <inheritdoc />
    public async Task<Result<CategoryOkResult>> Handle(
        ReorderCategoriesCommand request,
        CancellationToken cancellationToken)
    {
        var reorder = await _categories.ReorderAsync(
            request.ParentId,
            request.OrderedCategoryIds ?? Array.Empty<Guid>(),
            cancellationToken);
        if (reorder.IsFailure)
        {
            return Result.Failure<CategoryOkResult>(reorder.Errors);
        }

        return Result.Success(new CategoryOkResult(true));
    }
}
