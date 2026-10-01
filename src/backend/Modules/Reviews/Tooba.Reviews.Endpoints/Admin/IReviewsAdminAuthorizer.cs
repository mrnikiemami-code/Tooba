using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Security;

namespace Tooba.Reviews.Endpoints.Admin;

/// <summary>درز مجوز Admin برای Reviews.</summary>
public interface IReviewsAdminAuthorizer
{
    Task<Guid> RequireAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken);
}

/// <summary>پیاده‌سازی روی IAdminPanelAccess بدون وابستگی به Host.</summary>
public sealed class ReviewsAdminAuthorizer(IAdminPanelAccess adminAccess) : IReviewsAdminAuthorizer
{
    /// <inheritdoc />
    public Task<Guid> RequireAuthorizedAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return adminAccess.RequireAuthorizedAsync(httpContext.Request, cancellationToken);
    }
}
