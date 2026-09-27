using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Media.Models;
using Tooba.Content.Application.Media.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Media.Commands;

public sealed record ReorderGalleryCommand(Guid ArticleId, IReadOnlyList<Guid> OrderedMediaAssetIds)
    : IRequest<Result<ArticleMediaWorkspaceDto>>;

public sealed class ReorderGalleryCommandHandler(IContentArticleMediaDirectory media)
    : IRequestHandler<ReorderGalleryCommand, Result<ArticleMediaWorkspaceDto>>
{
    public Task<Result<ArticleMediaWorkspaceDto>> Handle(
        ReorderGalleryCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => media.ReorderGalleryAsync(request.ArticleId, request.OrderedMediaAssetIds, cancellationToken),
            ContentErrorCodes.ArticleMediaRejected);
}
