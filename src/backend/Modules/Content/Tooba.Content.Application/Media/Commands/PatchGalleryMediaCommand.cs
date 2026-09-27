using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Media.Models;
using Tooba.Content.Application.Media.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Media.Commands;

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
