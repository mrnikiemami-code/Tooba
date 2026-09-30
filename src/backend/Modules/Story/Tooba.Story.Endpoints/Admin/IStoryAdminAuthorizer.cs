using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Security;

namespace Tooba.Story.Endpoints.Admin;

/// <summary>درز مجوز Admin برای Story.</summary>
public interface IStoryAdminAuthorizer
{
    Task<Guid> RequireAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken);
}

/// <summary>پیاده‌سازی روی IAdminPanelAccess بدون وابستگی به Host.</summary>
public sealed class StoryAdminAuthorizer(IAdminPanelAccess adminAccess) : IStoryAdminAuthorizer
{
    /// <inheritdoc />
    public Task<Guid> RequireAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return adminAccess.RequireAuthorizedAsync(httpContext.Request, cancellationToken);
    }
}
