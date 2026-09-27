using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;

namespace Tooba.Content.Application.Queries.SearchTags;

public sealed record SearchTagsQuery(string LanguageCode, string? Search, int Limit, bool ActiveOnly)
    : IRequest<Result<IReadOnlyList<ContentTagDto>>>;

public sealed class SearchTagsQueryHandler(IContentTagDirectory tags)
    : IRequestHandler<SearchTagsQuery, Result<IReadOnlyList<ContentTagDto>>>
{
    public Task<Result<IReadOnlyList<ContentTagDto>>> Handle(
        SearchTagsQuery request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => tags.SearchAsync(request.LanguageCode, request.Search, request.Limit, request.ActiveOnly, cancellationToken),
            ContentErrorCodes.TagNotFound);
}
