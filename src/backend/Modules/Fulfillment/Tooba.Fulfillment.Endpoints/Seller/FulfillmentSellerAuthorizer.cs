using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Security;
using Tooba.Fulfillment.Application.Commands.SellerMutateFulfillment;

namespace Tooba.Fulfillment.Endpoints.Seller;

/// <summary>
/// مجوز فروشنده Fulfillment و پروجکشن <c>order.handle</c>. سیاست Fulfillment اینجاست؛
/// مکانیک عمومی پنل و مجوز مؤثر از درزهای خنثی پلتفرم می‌آید و این ماژول به Host،
/// AccessControl.Application و AccessControl.Domain وابسته نیست.
/// </summary>
public sealed class FulfillmentSellerAuthorizer(
    ISellerPanelAccess sellerAccess,
    IPlatformEffectiveAccessReader effectiveAccess) : IFulfillmentSellerAuthorizer
{
    /// <inheritdoc />
    public Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
        HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return sellerAccess.RequireAuthorizedAsync(httpContext.Request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SellerHandlePermissionInput> GetHandlePermissionAsync(
        Guid actorUserId, Guid sellerPartyId, CancellationToken cancellationToken)
    {
        var effective = await effectiveAccess.GetEffectivePermissionsAsync(
            actorUserId,
            PlatformAccessOwnerKind.Seller,
            sellerPartyId,
            cancellationToken);
        var handles = effective
            .Where(p => p.PermissionId == "order.handle" && !p.DeniedByCeiling)
            .ToList();
        var hasGlobal = handles.Any(p => p.ScopeKind == PlatformAccessScopeKind.GlobalWithinOwner);
        var allowed = handles
            .Where(p => p.ScopeKind == PlatformAccessScopeKind.Category && p.ScopeResourceId is not null)
            .Select(p => p.ScopeResourceId!.Value)
            .Distinct()
            .ToArray();
        return new SellerHandlePermissionInput(hasGlobal, allowed);
    }
}
