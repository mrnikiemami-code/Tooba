namespace Tooba.AccessControl.Contracts.Development;

/// <summary>
/// Rebuilds the process-local InMemory authorization projection from durable AccessControl
/// role assignments already present for a tenant. Development + InMemory engine only.
/// Never calls EnsureBootstrap, never registers users, never mutates durable AccessControl rows.
/// </summary>
public interface IAccessControlInMemoryProjectionRecovery
{
    /// <summary>
    /// Projects existing assignments for <paramref name="tenantId"/> into the local authorization engine.
    /// </summary>
    /// <param name="tenantId">Trusted current tenant id (e.g. store-alpha).</param>
    /// <param name="cancellationToken">Cancellation.</param>
    Task<AccessControlInMemoryProjectionRecoveryResult> RecoverAsync(
        string tenantId,
        CancellationToken cancellationToken);
}

/// <summary>Outcome of a local InMemory projection recovery attempt.</summary>
/// <param name="EngineEligible">True when Development and Mode=InMemory; otherwise recovery was a no-op.</param>
/// <param name="UsersProjected">Distinct users for which capability sync ran.</param>
/// <param name="TenantMembershipsProjected">tenant#member tuples written from platform-scope assignments.</param>
/// <param name="PartyMembershipsProjected">party#member tuples written from seller-scope assignments.</param>
public sealed record AccessControlInMemoryProjectionRecoveryResult(
    bool EngineEligible,
    int UsersProjected,
    int TenantMembershipsProjected,
    int PartyMembershipsProjected);
