#pragma warning disable CS1591
using Tooba.BuildingBlocks.Security;
using Tooba.Settlement.Endpoints.Admin;

namespace Tooba.Host.Admin;

/// <summary>
/// اتصال Host به درز احراز Settlement admin Endpoints.
/// تصمیم tenant/platform فقط از درز عمومی <see cref="IAdminPanelAccess"/> می‌آید.
/// </summary>
public sealed class HostSettlementAdminAuthorizer(IAdminPanelAccess adminAccess) : ISettlementAdminAuthorizer
{
    /// <inheritdoc />
    public Task<Guid> RequireAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return adminAccess.RequireAuthorizedAsync(httpContext.Request, cancellationToken);
    }
}
