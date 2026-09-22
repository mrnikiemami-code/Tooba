#pragma warning disable CS1591
using Tooba.AccessControl.Application;
using Tooba.AccessControl.Domain;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application.Commands.SellerMutateFulfillment;
using Tooba.Fulfillment.Endpoints.Seller;

namespace Tooba.Host.Seller;

/// <summary>Host transport adapter for Fulfillment seller Endpoints auth + permission snapshot.</summary>
public sealed class HostFulfillmentSellerAuthorizer : IFulfillmentSellerAuthorizer
{
    private readonly IAccessControlDirectory _access;

    public HostFulfillmentSellerAuthorizer(IAccessControlDirectory access) => _access = access;

    public Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
        HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        var session = httpContext.RequestServices.GetRequiredService<CurrentAuthenticatedSession>();
        var guard = httpContext.RequestServices.GetRequiredService<IAuthorizationGuard>();
        var environment = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();
        return SellerPanelAccess.RequireAuthorizedAsync(
            httpContext.Request, session, guard, environment, cancellationToken);
    }

    public async Task<SellerHandlePermissionInput> GetHandlePermissionAsync(
        Guid actorUserId, Guid sellerPartyId, CancellationToken cancellationToken)
    {
        var effective = await _access.GetEffectiveAccessAsync(
            actorUserId,
            new AccessOwnerScope(AccessOwnerScopeKind.Seller, sellerPartyId),
            cancellationToken);
        var handles = effective.Permissions
            .Where(p => p.PermissionId == "order.handle" && !p.DeniedByCeiling)
            .ToList();
        var hasGlobal = handles.Any(p => p.ScopeKind == AccessScopeKind.GlobalWithinOwner);
        var allowed = handles
            .Where(p => p.ScopeKind == AccessScopeKind.Category && p.ScopeResourceId is not null)
            .Select(p => p.ScopeResourceId!.Value)
            .Distinct()
            .ToArray();
        return new SellerHandlePermissionInput(hasGlobal, allowed);
    }
}
