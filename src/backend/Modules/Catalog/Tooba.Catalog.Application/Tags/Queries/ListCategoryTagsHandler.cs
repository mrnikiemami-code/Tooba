using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Tags.Ports;

namespace Tooba.Catalog.Application.Tags.Queries;

/// <summary>Lists tags assigned to a category.</summary>
public sealed class ListCategoryTagsHandler
    : IRequestHandler<ListCategoryTagsQuery, Result<IReadOnlyList<TagView>>>
{
    private readonly ITagDirectory _tags;

    /// <summary>Creates the handler.</summary>
    public ListCategoryTagsHandler(ITagDirectory tags) => _tags = tags;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TagView>>> Handle(
        ListCategoryTagsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _tags.ListCategoryTagsAsync(request.CategoryId, request.Locale, cancellationToken);
        return Result.Success(items);
    }
}
