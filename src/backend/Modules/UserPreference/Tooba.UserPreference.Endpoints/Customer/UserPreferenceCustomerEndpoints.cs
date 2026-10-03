using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.UserPreference.Application.LocalePreferences.Commands;
using Tooba.UserPreference.Application.LocalePreferences.Queries;
using Tooba.UserPreference.Application.Models;
using Tooba.UserPreference.Contracts.Errors;
using Tooba.UserPreference.Endpoints.Admin;

namespace Tooba.UserPreference.Endpoints.Customer;

/// <summary>مرز HTTP ترجیح locale مشتری.</summary>
public static class UserPreferenceCustomerEndpoints
{
    /// <summary>مسیرهای GET/PUT locale مشتری را ثبت می‌کند.</summary>
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/", GetAsync);
        group.MapPut("/", PutAsync);
    }

    private static async Task<IResult> GetAsync(
        HttpContext httpContext,
        IUserPreferenceCustomerActorResolver actorResolver,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var actor = actorResolver.ResolveActor(httpContext);
        if (actor is null)
        {
            return api.FromFailure(new SemanticError(UserPreferenceErrorCodes.SessionRequired));
        }

        var snapshot = await sender.Send(new GetUserPreferenceQuery(actor.Value), cancellationToken);
        return Results.Json(snapshot is null
            ? new { locale = UserPreferenceShapes.DefaultLocale, createdAt = (DateTimeOffset?)null, updatedAt = (DateTimeOffset?)null }
            : new { locale = snapshot.Locale, createdAt = snapshot.CreatedAt, updatedAt = snapshot.UpdatedAt });
    }

    private static async Task<IResult> PutAsync(
        UserPreferenceWriteRequest body,
        HttpContext httpContext,
        IUserPreferenceCustomerActorResolver actorResolver,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var actor = actorResolver.ResolveActor(httpContext);
        if (actor is null)
        {
            return api.FromFailure(new SemanticError(UserPreferenceErrorCodes.SessionRequired));
        }

        try
        {
            var updated = await sender.Send(
                new UpsertUserPreferenceCommand(actor.Value, body.Locale),
                cancellationToken);
            return Results.Json(new { locale = updated.Locale, createdAt = updated.CreatedAt, updatedAt = updated.UpdatedAt });
        }
        catch (SemanticException ex)
        {
            return api.FromSemanticException(ex);
        }
    }
}

/// <summary>بدنهٔ نوشتن locale.</summary>
public sealed record UserPreferenceWriteRequest(string Locale);
