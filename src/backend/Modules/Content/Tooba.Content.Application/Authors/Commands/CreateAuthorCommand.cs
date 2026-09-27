using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Authors.Models;
using Tooba.Content.Application.Authors.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Authors.Commands;

public sealed record CreateAuthorCommand(
    string DisplayName, string Slug, string? ShortBio, string? FullBio,
    Guid? ProfileImageMediaAssetId, Guid? CoverImageMediaAssetId,
    string? WebsiteUrl, string? InstagramUrl, string? TwitterUrl, string? LinkedInUrl)
    : IRequest<Result<ContentAuthorWorkspaceDto>>;

public sealed class CreateAuthorCommandHandler(IContentAuthorDirectory authors)
    : IRequestHandler<CreateAuthorCommand, Result<ContentAuthorWorkspaceDto>>
{
    public Task<Result<ContentAuthorWorkspaceDto>> Handle(
        CreateAuthorCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => authors.CreateAsync(request, cancellationToken),
            ContentErrorCodes.CreateRejected);
}
