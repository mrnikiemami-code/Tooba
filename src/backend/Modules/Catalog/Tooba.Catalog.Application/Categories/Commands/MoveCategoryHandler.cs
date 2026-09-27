using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Categories.Ports;

namespace Tooba.Catalog.Application.Categories.Commands;

/// <summary>Moves a category then returns workspace.</summary>
public sealed class MoveCategoryHandler : IRequestHandler<MoveCategoryCommand, Result<CategoryWorkspaceSummaryDto>>
{
    private readonly ICategoryDirectory _categories;

    /// <summary>Creates the handler.</summary>
    public MoveCategoryHandler(ICategoryDirectory categories) => _categories = categories;

    /// <inheritdoc />
    public async Task<Result<CategoryWorkspaceSummaryDto>> Handle(
        MoveCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var move = await _categories.MoveAsync(
            request.CategoryId,
            request.NewParentId,
            request.ExpectedUpdatedAt,
            cancellationToken);
        if (move.IsFailure)
        {
            return Result.Failure<CategoryWorkspaceSummaryDto>(move.Errors);
        }

        return await _categories.GetWorkspaceAsync(request.CategoryId, null, cancellationToken);
    }
}
