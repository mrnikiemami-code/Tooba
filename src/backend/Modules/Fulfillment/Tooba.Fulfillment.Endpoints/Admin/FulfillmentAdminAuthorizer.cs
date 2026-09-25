using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Security;

namespace Tooba.Fulfillment.Endpoints.Admin;

/// <summary>
/// مجوز پنل مدیر Fulfillment. سیاست Fulfillment اینجاست؛ مکانیک عمومی پلتفرم از درز
/// <see cref="IAdminPanelAccess"/> می‌آید و این ماژول به Host وابسته نیست.
/// </summary>
public sealed class FulfillmentAdminAuthorizer(IAdminPanelAccess adminAccess) : IFulfillmentAdminAuthorizer
{
    /// <inheritdoc />
    public Task<Guid> RequireAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return adminAccess.RequireAuthorizedAsync(httpContext.Request, cancellationToken);
    }
}
