namespace Tooba.Media.Contracts.Assets;

/// <summary>
/// Media-owned Story seam: authorize Ready image/video assets for a shop and resolve the public URL.
/// </summary>
public interface IStoryMediaAssetPort
{
    /// <summary>
    /// Ensures the asset is Ready and usable by the given tenant+shop (seller-owned binding),
    /// or is an unbound legacy/admin asset when <paramref name="allowUnscopedAdminAssets"/> is true.
    /// Returns the canonical public display URL.
    /// </summary>
    Task<StoryMediaAssetAuthorization> AuthorizeForStoryAsync(
        Guid mediaAssetId,
        Guid tenantId,
        Guid? sellerPartyId,
        bool allowUnscopedAdminAssets,
        CancellationToken cancellationToken);

    /// <summary>Uploads a seller-owned Ready asset and binds it to tenant+shop before returning.</summary>
    Task<StoryMediaAssetAuthorization> UploadForSellerAsync(
        Stream stream,
        string fileName,
        string contentType,
        Guid tenantId,
        Guid sellerPartyId,
        Guid actorUserId,
        string? occurrenceKey,
        CancellationToken cancellationToken);

    /// <summary>
    /// Platform Story media upload with schema-free occurrence replay: deterministic asset id from
    /// tenant+actor+occurrence, unique object-store key per attempt, no seller binding.
    /// </summary>
    Task<StoryMediaAssetAuthorization> UploadForPlatformStoryAsync(
        Stream stream,
        string fileName,
        string contentType,
        Guid tenantId,
        Guid actorUserId,
        string? occurrenceKey,
        CancellationToken cancellationToken);

    /// <summary>Lists Ready assets owned by the seller shop.</summary>
    Task<IReadOnlyList<StoryMediaAssetAuthorization>> ListSellerAssetsAsync(
        Guid tenantId,
        Guid sellerPartyId,
        int take,
        CancellationToken cancellationToken);
}

/// <summary>Authorized Story media reference with Media-owned public URL.</summary>
public sealed record StoryMediaAssetAuthorization(
    Guid MediaAssetId,
    string ContentType,
    string DisplayUrl,
    bool IsVideo,
    Guid? SellerPartyId);
