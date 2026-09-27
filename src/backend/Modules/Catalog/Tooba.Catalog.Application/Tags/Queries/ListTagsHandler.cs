using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Tags.Ports;

namespace Tooba.Catalog.Application.Tags.Queries;

/// <summary>Lists Catalog tags for Admin.</summary>
public sealed class ListTagsHandler : IRequestHandler<ListTagsQuery, Result<IReadOnlyList<TagView>>>
{
    private readonly ITagDirectory _tags;

    /// <summary>Creates the handler.</summary>
    public ListTagsHandler(ITagDirectory tags) => _tags = tags;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TagView>>> Handle(
        ListTagsQuery request,
        CancellationToken cancellationToken)
    {
        var locale = string.IsNullOrWhiteSpace(request.Locale) ? "fa-IR" : request.Locale;
        var items = await _tags.ListAsync(locale, request.Search, cancellationToken);
        return Result.Success(items);
    }
}
