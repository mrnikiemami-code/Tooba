namespace Tooba.Media.Domain.Aggregates;

/// <summary>Media-owned positive proof that a Ready asset belongs to one tenant+shop.</summary>
public sealed class MediaAssetOwnerBinding
{
    private MediaAssetOwnerBinding()
    {
    }

    /// <summary>Bound media asset.</summary>
    public Guid MediaAssetId { get; init; }

    /// <summary>Owning tenant.</summary>
    public Guid TenantId { get; init; }

    /// <summary>Owning seller/shop party.</summary>
    public Guid SellerPartyId { get; init; }

    /// <summary>Actor that uploaded the asset.</summary>
    public Guid CreatedByUserId { get; init; }

    /// <summary>Binding instant.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Creates a durable owner binding for a newly uploaded seller asset.</summary>
    public static MediaAssetOwnerBinding Create(
        Guid mediaAssetId,
        Guid tenantId,
        Guid sellerPartyId,
        Guid createdByUserId,
        DateTimeOffset now)
    {
        if (mediaAssetId == Guid.Empty || tenantId == Guid.Empty || sellerPartyId == Guid.Empty || createdByUserId == Guid.Empty)
            throw new ArgumentException("Media owner binding identities are required.");

        return new MediaAssetOwnerBinding
        {
            MediaAssetId = mediaAssetId,
            TenantId = tenantId,
            SellerPartyId = sellerPartyId,
            CreatedByUserId = createdByUserId,
            CreatedAt = now,
        };
    }
}
