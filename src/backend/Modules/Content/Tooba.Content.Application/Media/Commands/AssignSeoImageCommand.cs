using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Media.Models;
using Tooba.Content.Application.Media.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Media.Commands;

public sealed record AssignSeoImageCommand(Guid ArticleId, Guid? MediaAssetId)
    : IRequest<Result<ArticleMediaWorkspaceDto>>;

public sealed class AssignSeoImageCommandHandler(IContentArticleMediaDirectory media)
    : IRequestHandler<AssignSeoImageCommand, Result<ArticleMediaWorkspaceDto>>
{
    public Task<Result<ArticleMediaWorkspaceDto>> Handle(
        AssignSeoImageCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => media.AssignSeoImageAsync(request.ArticleId, request.MediaAssetId, cancellationToken),
            ContentErrorCodes.ArticleMediaRejected);
}
