using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Categories.Ports;

namespace Tooba.Catalog.Application.Categories.Commands;

/// <summary>Publishes a category then returns workspace.</summary>
public sealed class PublishCategoryHandler
    : IRequestHandler<PublishCategoryCommand, Result<CategoryWorkspaceSummaryDto>>
{
    private readonly ICategoryDirectory _categories;

    /// <summary>Creates the handler.</summary>
    public PublishCategoryHandler(ICategoryDirectory categories) => _categories = categories;

    /// <inheritdoc />
    public async Task<Result<CategoryWorkspaceSummaryDto>> Handle(
        PublishCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var publish = await _categories.PublishAsync(request.CategoryId, cancellationToken);
        if (publish.IsFailure)
        {
            return Result.Failure<CategoryWorkspaceSummaryDto>(publish.Errors);
        }

        return await _categories.GetWorkspaceAsync(request.CategoryId, null, cancellationToken);
    }
}
