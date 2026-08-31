using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Grid;

namespace Tooba.Host.Admin;

/// <summary>الگوی مشترک endpointهای POST .../query برای گریدهای Admin.</summary>
internal static class AdminGridQueryEndpoint
{
    /// <summary>GridQuery را با مجوز Admin اجرا می‌کند.</summary>
    public static async Task<IResult> ExecuteAsync<T>(
        GridQueryRequest body,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        Func<GridQueryRequest, CancellationToken, Task<GridPageResponse<T>>> query,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            return Results.Json(await query(body, cancellationToken));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }
}
