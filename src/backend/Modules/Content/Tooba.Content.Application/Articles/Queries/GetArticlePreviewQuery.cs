using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Articles.Models;
using Tooba.Content.Application.Articles.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Articles.Queries;

public sealed record GetArticlePreviewQuery(Guid ArticleId) : IRequest<Result<ArticlePreviewSnapshot>>;

public sealed class GetArticlePreviewQueryHandler(IContentDirectory content)
    : IRequestHandler<GetArticlePreviewQuery, Result<ArticlePreviewSnapshot>>
{
    public Task<Result<ArticlePreviewSnapshot>> Handle(
        GetArticlePreviewQuery request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(async () =>
        {
            var preview = await content.GetPreviewAsync(request.ArticleId, cancellationToken);
            if (preview is null)
                throw new ContractOperationException(ContentErrorCodes.PreviewUnavailable);
            return preview;
        });
}
