using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;

namespace Tooba.Content.Application.Commands.ReorderGallery;

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
