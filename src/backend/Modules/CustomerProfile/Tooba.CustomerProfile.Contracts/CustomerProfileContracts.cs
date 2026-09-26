namespace Tooba.CustomerProfile.Contracts;

/// <summary>Private descriptive profile snapshot without owner/credential projection in API payloads.</summary>
public sealed record CustomerProfileSnapshot(
    string FirstName,
    string LastName,
    string DisplayName,
    string? BirthDate,
    string? Bio,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Descriptive profile write input. It carries no email/mobile/password/national code authority.
/// </summary>
public sealed record CustomerProfileWrite(
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? BirthDate,
    string? Bio);

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
