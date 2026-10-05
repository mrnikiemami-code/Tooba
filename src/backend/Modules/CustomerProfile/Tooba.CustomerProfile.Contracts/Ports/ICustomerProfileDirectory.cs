using Tooba.CustomerProfile.Contracts.Dtos;

namespace Tooba.CustomerProfile.Contracts.Ports;

/// <summary>
/// Stable descriptive-profile contract. All operations are scoped to the acting user id supplied
/// by the Host boundary; callers never see CustomerProfile Application/Domain/persistence types.
/// </summary>
public interface ICustomerProfileDirectory
{
    /// <summary>Returns the actor profile, or null when no row exists.</summary>
    Task<CustomerProfileSnapshot?> GetAsync(Guid actorUserId, CancellationToken cancellationToken);

    /// <summary>Creates or updates the actor profile.</summary>
    Task<CustomerProfileSnapshot> UpsertAsync(
        Guid actorUserId,
        CustomerProfileWrite input,
        CancellationToken cancellationToken);
}
