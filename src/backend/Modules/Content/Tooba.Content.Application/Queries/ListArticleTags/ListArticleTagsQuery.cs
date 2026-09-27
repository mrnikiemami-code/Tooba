using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;

namespace Tooba.Content.Application.Queries.ListArticleTags;

public sealed record ListArticleTagsQuery(Guid ArticleId)
    : IRequest<Result<IReadOnlyList<ContentTagDto>>>;

public sealed class ListArticleTagsQueryHandler(IContentTagDirectory tags)
    : IRequestHandler<ListArticleTagsQuery, Result<IReadOnlyList<ContentTagDto>>>
{
    public Task<Result<IReadOnlyList<ContentTagDto>>> Handle(
        ListArticleTagsQuery request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => tags.ListArticleTagsAsync(request.ArticleId, cancellationToken),
            ContentErrorCodes.TagArticleNotFound);
}
