using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Categories.Ports;

namespace Tooba.Catalog.Application.Categories.Queries;

/// <summary>Reads category workspace.</summary>
public sealed class GetCategoryWorkspaceHandler
    : IRequestHandler<GetCategoryWorkspaceQuery, Result<CategoryWorkspaceSummaryDto>>
{
    private readonly ICategoryDirectory _categories;

    /// <summary>Creates the handler.</summary>
    public GetCategoryWorkspaceHandler(ICategoryDirectory categories) => _categories = categories;

    /// <inheritdoc />
    public Task<Result<CategoryWorkspaceSummaryDto>> Handle(
        GetCategoryWorkspaceQuery request,
        CancellationToken cancellationToken) =>
        _categories.GetWorkspaceAsync(request.CategoryId, request.Locale, cancellationToken);
}
