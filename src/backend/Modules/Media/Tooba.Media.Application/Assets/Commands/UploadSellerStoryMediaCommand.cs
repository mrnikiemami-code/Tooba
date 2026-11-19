using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Media.Application.Composition;
using Tooba.Media.Contracts.Assets;

namespace Tooba.Media.Application.Assets.Commands;

/// <summary>Seller/admin-for-shop Story media upload through Media-owned CQRS.</summary>
public sealed record UploadSellerStoryMediaCommand(
    Stream Content,
    string OriginalFileName,
    string ContentType,
    Guid TenantId,
    Guid SellerPartyId,
    Guid ActorUserId,
    string? OccurrenceKey) : IRequest<Result<StoryMediaAssetAuthorization>>;

/// <summary>Handler for <see cref="UploadSellerStoryMediaCommand"/>.</summary>
public sealed class UploadSellerStoryMediaCommandHandler(IStoryMediaAssetPort media)
    : IRequestHandler<UploadSellerStoryMediaCommand, Result<StoryMediaAssetAuthorization>>
{
    /// <inheritdoc />
    public Task<Result<StoryMediaAssetAuthorization>> Handle(
        UploadSellerStoryMediaCommand request,
        CancellationToken cancellationToken) =>
        MediaOperation.ExecuteAsync(() => media.UploadForSellerAsync(
            request.Content,
            request.OriginalFileName,
            request.ContentType,
            request.TenantId,
            request.SellerPartyId,
            request.ActorUserId,
            request.OccurrenceKey,
            cancellationToken));
}

/// <summary>Lists seller-owned Story media assets.</summary>
public sealed record ListSellerStoryMediaQuery(Guid TenantId, Guid SellerPartyId, int Take)
    : IRequest<Result<IReadOnlyList<StoryMediaAssetAuthorization>>>;

/// <summary>Handler for <see cref="ListSellerStoryMediaQuery"/>.</summary>
public sealed class ListSellerStoryMediaQueryHandler(IStoryMediaAssetPort media)
    : IRequestHandler<ListSellerStoryMediaQuery, Result<IReadOnlyList<StoryMediaAssetAuthorization>>>
{
    /// <inheritdoc />
    public Task<Result<IReadOnlyList<StoryMediaAssetAuthorization>>> Handle(
        ListSellerStoryMediaQuery request,
        CancellationToken cancellationToken) =>
        MediaOperation.ExecuteAsync(() => media.ListSellerAssetsAsync(
            request.TenantId, request.SellerPartyId, request.Take, cancellationToken));
}

/// <summary>Gets one seller-owned Story media asset.</summary>
public sealed record GetSellerStoryMediaQuery(Guid TenantId, Guid SellerPartyId, Guid MediaAssetId)
    : IRequest<Result<StoryMediaAssetAuthorization>>;

/// <summary>Handler for <see cref="GetSellerStoryMediaQuery"/>.</summary>
public sealed class GetSellerStoryMediaQueryHandler(IStoryMediaAssetPort media)
    : IRequestHandler<GetSellerStoryMediaQuery, Result<StoryMediaAssetAuthorization>>
{
    /// <inheritdoc />
    public Task<Result<StoryMediaAssetAuthorization>> Handle(
        GetSellerStoryMediaQuery request,
        CancellationToken cancellationToken) =>
        MediaOperation.ExecuteAsync(() => media.AuthorizeForStoryAsync(
            request.MediaAssetId,
            request.TenantId,
            request.SellerPartyId,
            allowUnscopedAdminAssets: false,
            cancellationToken));
}
