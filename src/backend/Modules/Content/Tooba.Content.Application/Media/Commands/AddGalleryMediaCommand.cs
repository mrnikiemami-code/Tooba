using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Media.Models;
using Tooba.Content.Application.Media.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Media.Commands;

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
