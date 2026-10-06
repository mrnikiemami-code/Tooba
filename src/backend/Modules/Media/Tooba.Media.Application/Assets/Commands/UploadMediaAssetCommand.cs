using MediatR;
using Microsoft.Extensions.Logging;
using Tooba.BuildingBlocks.Results;
using Tooba.Media.Application.Composition;
using Tooba.Media.Application.Models;
using Tooba.Media.Application.Ports;
using Tooba.Media.Contracts.Errors;

namespace Tooba.Media.Application.Assets.Commands;

/// <summary>Upload one media asset stream into the Media DAM.</summary>
public sealed record UploadMediaAssetCommand(
    Stream Content,
    string OriginalFileName,
    string ContentType,
    Guid? ActorUserId) : IRequest<Result<MediaAssetInfo>>;

/// <summary>Handler for <see cref="UploadMediaAssetCommand"/>.</summary>
public sealed class UploadMediaAssetCommandHandler(
    IMediaDirectory directory,
    ILogger<UploadMediaAssetCommandHandler> logger)
    : IRequestHandler<UploadMediaAssetCommand, Result<MediaAssetInfo>>
{
    /// <inheritdoc />
    public async Task<Result<MediaAssetInfo>> Handle(
        UploadMediaAssetCommand request,
        CancellationToken cancellationToken)
    {
        var outcome = await MediaOperation.ExecuteAsync(() =>
            directory.UploadAsync(
                request.Content,
                request.OriginalFileName,
                request.ContentType,
                request.ActorUserId,
                cancellationToken));

        if (outcome.IsFailure)
        {
            logger.LogInformation("{MediaUploadEvent}", MediaErrorCodes.UploadFailed);
            return outcome;
        }

        logger.LogInformation("media.upload.succeeded");
        return outcome;
    }
}
