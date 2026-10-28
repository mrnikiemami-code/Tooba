namespace Tooba.AccessControl.Contracts.Development;

/// <summary>
/// Neutral development prelude for Host composition seed binders.
/// Owns seller-dev-context ensure, AccessControl bootstrap, and seller capability tuple sync
/// without exposing Application/Domain/Infrastructure types to Host.
/// </summary>
public interface IAccessControlDevelopmentSeedPrelude
{
    /// <summary>
    /// Ensures seller demo contexts, AccessControl bootstrap for the admin actor and seller parties,
    /// and syncs seller ActorA capability tuples. Returns ActorA ids, or <see langword="null"/> when
    /// seller context is unavailable. Must run on a scope with CommerceContext already assigned.
    /// </summary>
    /// <param name="adminActorUserId">Platform admin demo actor user id.</param>
    /// <param name="tenantId">Active tenant id string.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    Task<AccessControlDevelopmentSeedActors?> EnsureSupportSeedPrerequisitesAsync(
        Guid adminActorUserId,
        string tenantId,
        CancellationToken cancellationToken);

    /// <summary>
    /// TB240-R2 Marketplace commissioning: binds BOTH real Development seller identities to their OWN
    /// seller-owner scope through the existing AccessControl authority, in a Tenant-NULL Marketplace
    /// deployment where no store tenant exists.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The existing <c>EnsureBootstrapAsync</c> already creates the seller-owner role, its delegable catalog
    /// permissions and the platform seller ceilings for every supplied seller id, but it never assigns that
    /// role to the seller's real identity user. This method performs exactly that missing step for the two
    /// ACTUAL seller actors resolved from Identity/Party membership — never from a fixed default actor id and
    /// never with a fabricated business tenant.
    /// </para>
    /// <para>
    /// Idempotent: an existing assignment is left untouched, no role/permission/ceiling is duplicated, no
    /// Platform grant is given to a seller, and the real Actor↔Seller pair is re-published LAST so a legacy
    /// synthetic Marketplace pair cannot remain published afterwards.
    /// </para>
    /// </remarks>
    /// <param name="platformAdminUserId">Optional real platform administrator actor for the audit trail.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The commissioned real pairs, or <see langword="null"/> when seller context is unavailable.</returns>
    Task<AccessControlMarketplaceSellerAccess?> EnsureMarketplaceSellerAccessAsync(
        Guid? platformAdminUserId,
        CancellationToken cancellationToken);
}

/// <summary>
/// Stable actor identifiers needed by Support Development seed after AccessControl prelude.
/// </summary>
/// <param name="SellerPartyId">Seller Party id for ActorA.</param>
/// <param name="SellerActorUserId">Actor user id for ActorA.</param>
public sealed record AccessControlDevelopmentSeedActors(
    Guid SellerPartyId,
    Guid SellerActorUserId);

/// <summary>One commissioned real seller identity and its own owner scope.</summary>
/// <param name="SellerPartyId">The seller's real business Party id.</param>
/// <param name="ActorUserId">The seller's real Identity user.</param>
/// <param name="SellerOwnerRoleId">The existing seller-owner role of that own scope.</param>
/// <param name="AssignmentCreated">True when this call created the assignment.</param>
public sealed record AccessControlMarketplaceSellerScope(
    Guid SellerPartyId,
    Guid ActorUserId,
    Guid SellerOwnerRoleId,
    bool AssignmentCreated);

/// <summary>Result of Marketplace seller-access commissioning for both real sellers.</summary>
/// <param name="Sellers">The two commissioned seller scopes, in stable order.</param>
public sealed record AccessControlMarketplaceSellerAccess(
    IReadOnlyList<AccessControlMarketplaceSellerScope> Sellers);
