using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Tags.Models;
using Tooba.Content.Application.Tags.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Tags.Queries;

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
