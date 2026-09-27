using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Media.Models;
using Tooba.Content.Application.Media.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Media.Commands;

public sealed record RemoveGalleryMediaCommand(Guid ArticleId, Guid MediaAssetId)
    : IRequest<Result<ArticleMediaWorkspaceDto>>;

public sealed class RemoveGalleryMediaCommandHandler(IContentArticleMediaDirectory media)
    : IRequestHandler<RemoveGalleryMediaCommand, Result<ArticleMediaWorkspaceDto>>
{
    public Task<Result<ArticleMediaWorkspaceDto>> Handle(
        RemoveGalleryMediaCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => media.RemoveGalleryItemAsync(request.ArticleId, request.MediaAssetId, cancellationToken),
            ContentErrorCodes.ArticleMediaRejected);
}
