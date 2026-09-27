using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Queries.GetArticleMediaWorkspace;

public sealed record GetArticleMediaWorkspaceQuery(Guid ArticleId)
    : IRequest<Result<ArticleMediaWorkspaceDto>>;

public sealed class GetArticleMediaWorkspaceQueryHandler(IContentArticleMediaDirectory media)
    : IRequestHandler<GetArticleMediaWorkspaceQuery, Result<ArticleMediaWorkspaceDto>>
{
    public Task<Result<ArticleMediaWorkspaceDto>> Handle(
        GetArticleMediaWorkspaceQuery request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => media.GetWorkspaceAsync(request.ArticleId, cancellationToken),
            ContentErrorCodes.ArticleMissing);
}
