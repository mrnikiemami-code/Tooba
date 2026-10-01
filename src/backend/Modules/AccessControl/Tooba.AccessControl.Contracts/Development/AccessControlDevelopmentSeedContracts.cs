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
}

/// <summary>
/// Stable actor identifiers needed by Support Development seed after AccessControl prelude.
/// </summary>
/// <param name="SellerPartyId">Seller Party id for ActorA.</param>
/// <param name="SellerActorUserId">Actor user id for ActorA.</param>
public sealed record AccessControlDevelopmentSeedActors(
    Guid SellerPartyId,
    Guid SellerActorUserId);
