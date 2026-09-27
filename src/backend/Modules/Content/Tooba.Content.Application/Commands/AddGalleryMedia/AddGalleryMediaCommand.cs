using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;

namespace Tooba.Content.Application.Commands.AddGalleryMedia;

public sealed record AddGalleryMediaCommand(Guid ArticleId, IReadOnlyList<Guid> MediaAssetIds)
    : IRequest<Result<ArticleMediaWorkspaceDto>>;

public sealed class AddGalleryMediaCommandHandler(IContentArticleMediaDirectory media)
    : IRequestHandler<AddGalleryMediaCommand, Result<ArticleMediaWorkspaceDto>>
{
    public Task<Result<ArticleMediaWorkspaceDto>> Handle(
        AddGalleryMediaCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => media.AddGalleryItemsAsync(request.ArticleId, request.MediaAssetIds, cancellationToken),
            ContentErrorCodes.ArticleMediaRejected);
}
