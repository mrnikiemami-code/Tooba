using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Media.Application.Composition;
using Tooba.Media.Application.Models;
using Tooba.Media.Application.Ports;
using Tooba.Media.Contracts.Errors;

namespace Tooba.Media.Application.Assets.Queries;

/// <summary>Load Ready media asset metadata by id.</summary>
public sealed record GetMediaAssetQuery(Guid MediaAssetId) : IRequest<Result<MediaAssetInfo>>;

/// <summary>Handler for <see cref="GetMediaAssetQuery"/>.</summary>
public sealed class GetMediaAssetQueryHandler(IMediaDirectory directory)
    : IRequestHandler<GetMediaAssetQuery, Result<MediaAssetInfo>>
{
    /// <inheritdoc />
    public async Task<Result<MediaAssetInfo>> Handle(
        GetMediaAssetQuery request,
        CancellationToken cancellationToken)
    {
        var outcome = await MediaOperation.ExecuteAsync(() =>
            directory.GetAsync(request.MediaAssetId, cancellationToken));
        if (outcome.IsFailure)
            return Result.Failure<MediaAssetInfo>(outcome.Errors);

        return outcome.Value is null
            ? Result.Failure<MediaAssetInfo>(new SemanticError(MediaErrorCodes.Missing))
            : Result.Success(outcome.Value);
    }
}
