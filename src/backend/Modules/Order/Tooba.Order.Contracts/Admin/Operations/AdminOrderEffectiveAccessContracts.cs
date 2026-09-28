namespace Tooba.Order.Contracts.Admin.Operations;

/// <summary>One effective permission grant for admin order operations.</summary>
public sealed record OrderAdminPermissionGrant(string PermissionId, bool DeniedByCeiling);

/// <summary>Effective access for the actor performing admin order operations.</summary>
public sealed record OrderAdminEffectiveAccess(IReadOnlyList<OrderAdminPermissionGrant> Permissions);

/// <summary>Host/thin adapter over AccessControl — no AccessControl Application in Order handlers.</summary>
public interface IOrderAdminEffectiveAccessReader
{
    /// <summary>Loads platform-scoped effective permissions for the actor.</summary>
    Task<OrderAdminEffectiveAccess> GetAsync(Guid actorUserId, CancellationToken cancellationToken);
}
