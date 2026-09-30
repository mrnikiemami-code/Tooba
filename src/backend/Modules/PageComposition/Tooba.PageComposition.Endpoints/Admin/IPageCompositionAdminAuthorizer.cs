using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Security;

namespace Tooba.PageComposition.Endpoints.Admin;

/// <summary>درز مجوز Admin برای PageComposition.</summary>
public interface IPageCompositionAdminAuthorizer
{
    Task RequireAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken);
}

/// <summary>پیاده‌سازی روی IAdminPanelAccess بدون وابستگی به Host.</summary>
public sealed class PageCompositionAdminAuthorizer(IAdminPanelAccess adminAccess) : IPageCompositionAdminAuthorizer
{
    /// <inheritdoc />
    public Task RequireAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return adminAccess.RequireAuthorizedAsync(httpContext.Request, cancellationToken);
    }
}
