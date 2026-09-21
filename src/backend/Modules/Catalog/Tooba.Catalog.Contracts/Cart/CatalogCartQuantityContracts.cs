using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Contracts;

/// <summary>Cart-facing Catalog quantity policy lookup without Catalog.Application leakage.</summary>
public interface ICatalogCartQuantityPolicyGateway
{
    /// <summary>Effective quantity policy for a Catalog variant, or null when unknown.</summary>
    Task<EffectiveQuantityPolicy?> GetEffectiveQuantityPolicyForVariantAsync(
        Guid variantId,
        CancellationToken cancellationToken);
}
