using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;

namespace Tooba.Content.Application.Commands.UpdateAuthor;

public sealed record UpdateAuthorCommand(
    Guid AuthorId, string DisplayName, string Slug, string? ShortBio, string? FullBio,
    Guid? ProfileImageMediaAssetId, Guid? CoverImageMediaAssetId,
    string? WebsiteUrl, string? InstagramUrl, string? TwitterUrl, string? LinkedInUrl)
    : IRequest<Result<ContentAuthorWorkspaceDto>>;

public sealed class UpdateAuthorCommandHandler(IContentAuthorDirectory authors)
    : IRequestHandler<UpdateAuthorCommand, Result<ContentAuthorWorkspaceDto>>
{
    public Task<Result<ContentAuthorWorkspaceDto>> Handle(
        UpdateAuthorCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => authors.UpdateAsync(request.AuthorId, new UpdateContentAuthorCommand(
                request.DisplayName, request.Slug, request.ShortBio, request.FullBio,
                request.ProfileImageMediaAssetId, request.CoverImageMediaAssetId,
                request.WebsiteUrl, request.InstagramUrl, request.TwitterUrl, request.LinkedInUrl), cancellationToken),
            ContentErrorCodes.UpdateRejected);
}
