using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Security;

namespace Tooba.Host.Seller;

/// <summary>
/// درز عمومی Host برای دسترسی پنل فروشنده: Actor از نشست/Development و مجوز از موتور مجوز.
/// هیچ سیاست ماژولی اینجا نیست.
/// </summary>
internal sealed class HostSellerPanelAccess(
    CurrentAuthenticatedSession session,
    IAuthorizationGuard guard,
    IHostEnvironment environment) : ISellerPanelAccess
{
    /// <inheritdoc />
    public Task<(Guid ActorUserId, Guid SellerPartyId)> RequireAuthorizedAsync(
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return SellerPanelAccess.RequireAuthorizedAsync(
            request, session, guard, environment, cancellationToken);
    }
}
