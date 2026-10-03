using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.UserPreference.Application.LocalePreferences.Commands;
using Tooba.UserPreference.Application.LocalePreferences.Queries;
using Tooba.UserPreference.Contracts.Errors;

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

        var result = await sender.Send(new GetUserPreferenceQuery(actor.Value), cancellationToken);
        return api.From(result);
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

        var result = await sender.Send(
            new UpsertUserPreferenceCommand(actor.Value, body.Locale),
            cancellationToken);
        return api.From(result);
    }
}

/// <summary>بدنهٔ نوشتن locale.</summary>
public sealed record UserPreferenceWriteRequest(string Locale);
