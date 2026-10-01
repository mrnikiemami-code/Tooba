using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.UserPreference.Application;
using Tooba.UserPreference.Application.LocalePreferences.Commands;
using Tooba.UserPreference.Application.LocalePreferences.Queries;

namespace Tooba.UserPreference.Endpoints.Admin;

/// <summary>مرز HTTP ترجیح locale اپراتور Admin.</summary>
public static class UserPreferenceAdminEndpoints
{
    /// <summary>مسیرهای GET/PUT locale ادمین را ثبت می‌کند.</summary>
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/", GetAsync);
        group.MapPut("/", PutAsync);
    }

    private static async Task<IResult> GetAsync(
        HttpContext httpContext,
        IUserPreferenceAdminAuthorizer adminAuthorizer,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = await adminAuthorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
            var snapshot = await sender.Send(new GetUserPreferenceQuery(actor), cancellationToken);
            return Results.Json(snapshot is null
                ? new { locale = UserPreferenceShapes.DefaultLocale, createdAt = (DateTimeOffset?)null, updatedAt = (DateTimeOffset?)null }
                : new { locale = snapshot.Locale, createdAt = snapshot.CreatedAt, updatedAt = snapshot.UpdatedAt });
        }
        catch (PlatformHttpException ex)
        {
            return api.FromPlatformException(ex);
        }
        catch (SemanticException ex)
        {
            return api.FromSemanticException(ex);
        }
    }

    private static async Task<IResult> PutAsync(
        Customer.UserPreferenceWriteRequest body,
        HttpContext httpContext,
        IUserPreferenceAdminAuthorizer adminAuthorizer,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = await adminAuthorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
            var updated = await sender.Send(
                new UpsertUserPreferenceCommand(actor, body.Locale),
                cancellationToken);
            return Results.Json(new { locale = updated.Locale, createdAt = updated.CreatedAt, updatedAt = updated.UpdatedAt });
        }
        catch (PlatformHttpException ex)
        {
            return api.FromPlatformException(ex);
        }
        catch (SemanticException ex)
        {
            return api.FromSemanticException(ex);
        }
    }
}
