using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Categories.Ports;

namespace Tooba.Catalog.Application.Categories.Commands;

/// <summary>Archives a category then returns workspace.</summary>
public sealed class ArchiveCategoryHandler
    : IRequestHandler<ArchiveCategoryCommand, Result<CategoryWorkspaceSummaryDto>>
{
    private readonly ICategoryDirectory _categories;

    /// <summary>Creates the handler.</summary>
    public ArchiveCategoryHandler(ICategoryDirectory categories) => _categories = categories;

    /// <inheritdoc />
    public async Task<Result<CategoryWorkspaceSummaryDto>> Handle(
        ArchiveCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var archive = await _categories.ArchiveAsync(request.CategoryId, cancellationToken);
        if (archive.IsFailure)
        {
            return Result.Failure<CategoryWorkspaceSummaryDto>(archive.Errors);
        }

        return await _categories.GetWorkspaceAsync(request.CategoryId, null, cancellationToken);
    }
}
