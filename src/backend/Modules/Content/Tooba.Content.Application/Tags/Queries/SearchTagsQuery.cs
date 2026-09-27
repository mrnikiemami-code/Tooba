using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Tags.Models;
using Tooba.Content.Application.Tags.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Tags.Queries;

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
