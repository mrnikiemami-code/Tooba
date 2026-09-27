using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Media.Models;
using Tooba.Content.Application.Media.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Media.Commands;

public sealed record AssignFeaturedMediaCommand(Guid ArticleId, Guid? MediaAssetId)
    : IRequest<Result<ArticleMediaWorkspaceDto>>;

public sealed class AssignFeaturedMediaCommandHandler(IContentArticleMediaDirectory media)
    : IRequestHandler<AssignFeaturedMediaCommand, Result<ArticleMediaWorkspaceDto>>
{
    public Task<Result<ArticleMediaWorkspaceDto>> Handle(
        AssignFeaturedMediaCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => media.AssignFeaturedAsync(request.ArticleId, request.MediaAssetId, cancellationToken),
            ContentErrorCodes.ArticleMediaRejected);
}
