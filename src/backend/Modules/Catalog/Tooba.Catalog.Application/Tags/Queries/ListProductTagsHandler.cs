using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Tags.Ports;

namespace Tooba.Catalog.Application.Tags.Queries;

/// <summary>Lists tags assigned to a product.</summary>
public sealed class ListProductTagsHandler
    : IRequestHandler<ListProductTagsQuery, Result<IReadOnlyList<TagView>>>
{
    private readonly ITagDirectory _tags;

    /// <summary>Creates the handler.</summary>
    public ListProductTagsHandler(ITagDirectory tags) => _tags = tags;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TagView>>> Handle(
        ListProductTagsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await _tags.ListProductTagsAsync(request.ProductId, request.Locale, cancellationToken);
        return Result.Success(items);
    }
}
