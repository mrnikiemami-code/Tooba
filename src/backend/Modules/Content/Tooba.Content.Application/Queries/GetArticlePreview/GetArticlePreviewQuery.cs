using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;

namespace Tooba.Content.Application.Queries.GetArticlePreview;

public sealed record GetArticlePreviewQuery(Guid ArticleId) : IRequest<Result<ArticlePreviewSnapshot>>;

public sealed class GetArticlePreviewQueryHandler(IContentDirectory content)
    : IRequestHandler<GetArticlePreviewQuery, Result<ArticlePreviewSnapshot>>
{
    public async Task<Result<ArticlePreviewSnapshot>> Handle(
        GetArticlePreviewQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var preview = await content.GetPreviewAsync(request.ArticleId, cancellationToken);
            return preview is null
                ? Result.Failure<ArticlePreviewSnapshot>(new SemanticError(ContentErrorCodes.PreviewUnavailable))
                : Result.Success(preview);
        }
        catch (PlatformHttpException ex) when (!string.IsNullOrWhiteSpace(ex.ErrorCode))
        {
            return Result.Failure<ArticlePreviewSnapshot>(new SemanticError(ex.ErrorCode!));
        }
        catch (InvalidOperationException ex) when (ContentErrorCodes.IsKnownCode(ex.Message))
        {
            return Result.Failure<ArticlePreviewSnapshot>(new SemanticError(ex.Message));
        }
    }
}
