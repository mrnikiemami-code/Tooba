using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;

namespace Tooba.Content.Application.Commands.PatchGalleryMedia;

public sealed record PatchGalleryMediaCommand(Guid ArticleId, Guid MediaAssetId, string? AltText, string? Caption)
    : IRequest<Result<ArticleMediaWorkspaceDto>>;

public sealed class PatchGalleryMediaCommandHandler(IContentArticleMediaDirectory media)
    : IRequestHandler<PatchGalleryMediaCommand, Result<ArticleMediaWorkspaceDto>>
{
    public Task<Result<ArticleMediaWorkspaceDto>> Handle(
        PatchGalleryMediaCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => media.PatchGalleryItemAsync(request.ArticleId, request.MediaAssetId, request.AltText, request.Caption, cancellationToken),
            ContentErrorCodes.ArticleMediaRejected);
}
