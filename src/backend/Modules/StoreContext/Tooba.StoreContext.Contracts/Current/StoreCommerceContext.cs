using Tooba.BuildingBlocks;

namespace Tooba.StoreContext.Contracts.Current;

/// <summary>
/// Effective storefront/store commerce context for the current request or worker cycle.
/// The platform control plane owns it; consumer modules only read. Null means unresolved and the
/// consumer must fail closed instead of inventing Market/Currency/SalesChannel.
/// </summary>
/// <param name="Market">Effective market reference.</param>
/// <param name="Currency">Effective ISO currency code.</param>
/// <param name="SalesChannel">Stable sales-channel name; still a string at this boundary.</param>
public sealed record StoreCommerceContext(
    string? Market,
    string? Currency,
    string? SalesChannel);

/// <summary>
/// Read access to the current effective store commerce context. Absent until the platform boundary
/// assigns it for a resolved request or background worker cycle.
/// </summary>
public interface ICurrentStoreCommerceContext
{
    /// <summary>
    /// The effective store commerce context for this scope, or null when unresolved/skipped.
    /// </summary>
    StoreCommerceContext? Current { get; }
}

/// <summary>
/// Assignment seam for the effective store commerce context. Host/control-plane composition and
/// background workers assign it; consumer modules never assign their own authority.
/// </summary>
public interface IStoreCommerceContextAssigner
{
    /// <summary>
    /// Assigns the effective store commerce context for the current scope.
    /// </summary>
    /// <param name="context">Effective context resolved by the platform boundary.</param>
    void Assign(StoreCommerceContext context);
}

/// <summary>
/// Generic platform seam that rebuilds the effective store commerce context for a background
/// worker target (edition + optional tenant) without reading HTTP headers.
/// </summary>
public interface IWorkerStoreCommerceContextFactory
{
    /// <summary>
    /// Selects the effective store commerce context for the given edition/tenant target.
    /// </summary>
    /// <param name="edition">Process edition resolved from the control plane.</param>
    /// <param name="tenantId">Single-Store tenant id; null for Marketplace.</param>
    StoreCommerceContext FromTarget(ToobaEdition edition, string? tenantId);
}
