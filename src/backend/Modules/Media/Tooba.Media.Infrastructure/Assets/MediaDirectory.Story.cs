using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Media.Contracts.Assets;
using Tooba.Media.Contracts.Errors;
using Tooba.Media.Domain.Aggregates;
using Tooba.Media.Domain.Enums;

namespace Tooba.Media.Infrastructure.Assets;

/// <summary>Story-facing Media ownership and seller upload seam.</summary>
public sealed partial class MediaDirectory : IStoryMediaAssetPort
{
    /// <inheritdoc />
    public async Task<StoryMediaAssetAuthorization> AuthorizeForStoryAsync(
        Guid mediaAssetId,
        Guid tenantId,
        Guid? sellerPartyId,
        bool allowUnscopedAdminAssets,
        CancellationToken cancellationToken)
    {
        var asset = await _db.Assets.AsNoTracking()
            .FirstOrDefaultAsync(
                row => row.MediaAssetId == mediaAssetId && row.Status == MediaAssetStatus.Ready,
                cancellationToken)
            ?? throw new ContractOperationException(MediaErrorCodes.Missing);

        if (!IsImageOrVideo(asset.ContentType))
            throw new ContractOperationException(MediaErrorCodes.TypeUnsupported);

        var binding = await _db.AssetOwnerBindings.AsNoTracking()
            .FirstOrDefaultAsync(row => row.MediaAssetId == mediaAssetId, cancellationToken);

        if (binding is null)
        {
            if (!allowUnscopedAdminAssets)
                throw new ContractOperationException(MediaErrorCodes.OwnerForbidden);
        }
        else
        {
            if (binding.TenantId != tenantId)
                throw new ContractOperationException(MediaErrorCodes.OwnerForbidden);
            if (sellerPartyId is Guid shop && shop != Guid.Empty && binding.SellerPartyId != shop)
                throw new ContractOperationException(MediaErrorCodes.OwnerForbidden);
        }

        return MapStory(asset, binding?.SellerPartyId);
    }

    /// <inheritdoc />
    public async Task<StoryMediaAssetAuthorization> UploadForSellerAsync(
        Stream stream,
        string fileName,
        string contentType,
        Guid tenantId,
        Guid sellerPartyId,
        Guid actorUserId,
        string? occurrenceKey,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || sellerPartyId == Guid.Empty || actorUserId == Guid.Empty)
            throw new ContractOperationException(MediaErrorCodes.OwnerForbidden);
        if (string.IsNullOrWhiteSpace(occurrenceKey))
            throw new ContractOperationException(MediaErrorCodes.ValidationFailed);
        if (stream is null || !stream.CanRead)
            throw new ContractOperationException(MediaErrorCodes.UploadFailed);

        var normalizedType = NormalizeContentType(contentType);
        if (!IsImageOrVideo(normalizedType) || !AllowedContentTypes.Contains(normalizedType))
            throw new ContractOperationException(MediaErrorCodes.TypeUnsupported);

        await using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length <= 0)
            throw new ContractOperationException(MediaErrorCodes.UploadFailed);
        if (buffer.Length > _maxUploadBytes)
            throw new ContractOperationException(MediaErrorCodes.TooLarge);

        var bytes = buffer.ToArray();
        var digest = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var safeName = SanitizeOriginalFileName(fileName);
        var intentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            string.Join('\u001f',
                Len(tenantId.ToString("D")),
                Len(sellerPartyId.ToString("D")),
                Len(normalizedType),
                Len(safeName),
                Len(digest))))).ToLowerInvariant();
        var key = occurrenceKey.Trim();

        var prior = await _db.AssetOwnerOccurrences.AsNoTracking()
            .FirstOrDefaultAsync(
                row => row.OccurrenceKey == key
                    && row.TenantId == tenantId
                    && row.SellerPartyId == sellerPartyId,
                cancellationToken);
        if (prior is not null)
        {
            if (!string.Equals(prior.IntentHash, intentHash, StringComparison.Ordinal))
                throw new ContractOperationException(MediaErrorCodes.OwnerForbidden);
            var existing = await _db.Assets.AsNoTracking()
                .FirstOrDefaultAsync(row => row.MediaAssetId == prior.MediaAssetId, cancellationToken)
                ?? throw new ContractOperationException(MediaErrorCodes.Missing);
            return MapStory(existing, sellerPartyId);
        }

        var now = DateTimeOffset.UtcNow;
        var assetId = UuidV7.New();
        var extension = ExtensionByContentType[normalizedType];
        var storageKey = $"{now:yyyy}/{now:MM}/{assetId:N}{extension}";

        buffer.Position = 0;
        try
        {
            await _store.SaveAsync(buffer, storageKey, normalizedType, cancellationToken);
        }
        catch (ContractOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ContractOperationException(MediaErrorCodes.StorageUnavailable, ex);
        }

        var asset = MediaAsset.CreateReady(
            storageKey,
            safeName,
            normalizedType,
            buffer.Length,
            digest,
            width: null,
            height: null,
            actorUserId,
            now,
            assetId);
        var binding = MediaAssetOwnerBinding.Create(assetId, tenantId, sellerPartyId, actorUserId, now);
        var occurrence = new MediaAssetOwnerOccurrence
        {
            OccurrenceKey = key,
            TenantId = tenantId,
            SellerPartyId = sellerPartyId,
            MediaAssetId = assetId,
            ActorUserId = actorUserId,
            IntentHash = intentHash,
            CreatedAt = now,
        };

        _db.Assets.Add(asset);
        _db.AssetOwnerBindings.Add(binding);
        _db.AssetOwnerOccurrences.Add(occurrence);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            DetachAttemptOwned(asset, binding, occurrence);
            try { await _store.DeleteAsync(storageKey, cancellationToken); } catch { /* attempt-owned cleanup */ }

            // Concurrent duplicate occurrence: fold to the winner if intent matches.
            var winner = await _db.AssetOwnerOccurrences.AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.OccurrenceKey == key
                        && row.TenantId == tenantId
                        && row.SellerPartyId == sellerPartyId,
                    cancellationToken);
            if (winner is not null)
            {
                if (!string.Equals(winner.IntentHash, intentHash, StringComparison.Ordinal))
                    throw new ContractOperationException(MediaErrorCodes.OwnerForbidden, ex);
                var existing = await _db.Assets.AsNoTracking()
                    .FirstAsync(row => row.MediaAssetId == winner.MediaAssetId, cancellationToken);
                return MapStory(existing, sellerPartyId);
            }

            throw new ContractOperationException(MediaErrorCodes.UploadFailed, ex);
        }
        catch (Exception ex)
        {
            DetachAttemptOwned(asset, binding, occurrence);
            try { await _store.DeleteAsync(storageKey, cancellationToken); } catch { /* attempt-owned cleanup */ }
            throw new ContractOperationException(MediaErrorCodes.UploadFailed, ex);
        }

        return MapStory(asset, sellerPartyId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StoryMediaAssetAuthorization>> ListSellerAssetsAsync(
        Guid tenantId,
        Guid sellerPartyId,
        int take,
        CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(take, 1, 100);
        var ids = await _db.AssetOwnerBindings.AsNoTracking()
            .Where(row => row.TenantId == tenantId && row.SellerPartyId == sellerPartyId)
            .OrderByDescending(row => row.CreatedAt)
            .Select(row => row.MediaAssetId)
            .Take(limit)
            .ToListAsync(cancellationToken);
        if (ids.Count == 0)
            return [];

        var assets = await _db.Assets.AsNoTracking()
            .Where(row => ids.Contains(row.MediaAssetId) && row.Status == MediaAssetStatus.Ready)
            .ToListAsync(cancellationToken);
        var byId = assets.ToDictionary(row => row.MediaAssetId);
        return ids
            .Where(byId.ContainsKey)
            .Select(id => MapStory(byId[id], sellerPartyId))
            .Where(row => IsImageOrVideo(row.ContentType))
            .ToList();
    }

    private void DetachAttemptOwned(
        MediaAsset asset,
        MediaAssetOwnerBinding binding,
        MediaAssetOwnerOccurrence occurrence)
    {
        var assetEntry = _db.Entry(asset);
        if (assetEntry.State != EntityState.Detached)
            assetEntry.State = EntityState.Detached;
        var bindingEntry = _db.Entry(binding);
        if (bindingEntry.State != EntityState.Detached)
            bindingEntry.State = EntityState.Detached;
        var occurrenceEntry = _db.Entry(occurrence);
        if (occurrenceEntry.State != EntityState.Detached)
            occurrenceEntry.State = EntityState.Detached;
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is Npgsql.PostgresException { SqlState: "23505" } postgres
        && (postgres.ConstraintName?.Contains("asset_owner_occurrences", StringComparison.OrdinalIgnoreCase) == true
            || postgres.TableName?.Contains("asset_owner_occurrences", StringComparison.OrdinalIgnoreCase) == true
            || string.Equals(postgres.ConstraintName, "pk_asset_owner_occurrences", StringComparison.OrdinalIgnoreCase));

    private static string Len(string value) =>
        value.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + value;

    private static bool IsImageOrVideo(string contentType) =>
        contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
        || contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);

    private static StoryMediaAssetAuthorization MapStory(MediaAsset asset, Guid? sellerPartyId) =>
        new(
            asset.MediaAssetId,
            asset.ContentType,
            $"/v1/storefront/media/{asset.MediaAssetId:D}",
            asset.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase),
            sellerPartyId);
}

/// <summary>Replay row for seller media uploads.</summary>
public sealed class MediaAssetOwnerOccurrence
{
    /// <summary>Occurrence key.</summary>
    public string OccurrenceKey { get; init; } = string.Empty;
    /// <summary>Tenant.</summary>
    public Guid TenantId { get; init; }
    /// <summary>Seller party.</summary>
    public Guid SellerPartyId { get; init; }
    /// <summary>Media asset.</summary>
    public Guid MediaAssetId { get; init; }
    /// <summary>Actor.</summary>
    public Guid ActorUserId { get; init; }
    /// <summary>SHA-256 of upload intent (type|name|content digest).</summary>
    public string IntentHash { get; init; } = string.Empty;
    /// <summary>Created at.</summary>
    public DateTimeOffset CreatedAt { get; init; }
}
