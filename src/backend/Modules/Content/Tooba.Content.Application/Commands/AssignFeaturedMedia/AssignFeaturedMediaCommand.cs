using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;

namespace Tooba.Content.Application.Commands.AssignFeaturedMedia;

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
