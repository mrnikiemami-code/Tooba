using Tooba.BuildingBlocks;
using Tooba.Host.Admin;
using Tooba.OperatorProfile.Application;

namespace Tooba.Host.OperatorProfile;

/// <summary>
/// مرز HTTP پروفایل شخصی اپراتور Admin؛ تنظیمات سراسری platform اینجا نیست.
/// </summary>
public static class OperatorProfileEndpoints
{
    /// <summary>مسیرهای پروفایل اپراتور را ثبت می‌کند.</summary>
    public static void MapOperatorProfileEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/admin/operator/profile");
        group.MapGet("/", GetProfileAsync);
        group.MapPut("/", UpdateProfileAsync);
    }

    private static async Task<IResult> GetProfileAsync(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        IOperatorProfileDirectory directory,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            var snapshot = await directory.GetAsync(actor, cancellationToken);
            return Results.Json(snapshot is null
                ? new
                {
                    firstName = "",
                    lastName = "",
                    displayName = "",
                    bio = (string?)null,
                    createdAt = (DateTimeOffset?)null,
                    updatedAt = (DateTimeOffset?)null,
                }
                : new
                {
                    firstName = snapshot.FirstName,
                    lastName = snapshot.LastName,
                    displayName = snapshot.DisplayName,
                    bio = snapshot.Bio,
                    createdAt = snapshot.CreatedAt,
                    updatedAt = snapshot.UpdatedAt,
                });
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Message, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> UpdateProfileAsync(
        OperatorProfileWriteRequest body,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        IOperatorProfileDirectory directory,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            var updated = await directory.UpsertAsync(
                actor,
                new OperatorProfileWrite(body.DisplayName, body.FirstName, body.LastName, body.Bio),
                cancellationToken);
            return Results.Json(new
            {
                firstName = updated.FirstName,
                lastName = updated.LastName,
                displayName = updated.DisplayName,
                bio = updated.Bio,
                createdAt = updated.CreatedAt,
                updatedAt = updated.UpdatedAt,
            });
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Message, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
        catch (InvalidOperationException)
        {
            return Results.Json(
                new { title = "Rejected", errorCode = "operator.profile.rejected" },
                statusCode: StatusCodes.Status400BadRequest);
        }
    }
}

/// <summary>بدنهٔ ویرایش پروفایل اپراتور.</summary>
public sealed record OperatorProfileWriteRequest(
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? Bio);
