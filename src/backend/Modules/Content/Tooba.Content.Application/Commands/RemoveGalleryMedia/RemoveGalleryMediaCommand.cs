using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;

namespace Tooba.Content.Application.Commands.RemoveGalleryMedia;

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
