using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.UserPreference.Application.UiPreferences.Commands;
using Tooba.UserPreference.Application.UiPreferences.Queries;
using Tooba.UserPreference.Contracts.Errors;

namespace Tooba.UserPreference.Endpoints.Admin;

/// <summary>مرز HTTP ترجیح UI Admin.</summary>
public static class UiPreferenceAdminEndpoints
{
    /// <summary>مسیرهای GET/PUT UI preference را ثبت می‌کند.</summary>
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/{key}", GetAsync);
        group.MapPut("/{key}", PutAsync);
    }

    private static async Task<IResult> GetAsync(
        string key,
        HttpContext httpContext,
        IUserPreferenceAdminAuthorizer adminAuthorizer,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var actor = await adminAuthorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
        var result = await sender.Send(new GetUiPreferenceQuery(actor, key), cancellationToken);
        return api.From(result);
    }

    private static async Task<IResult> PutAsync(
        string key,
        UiPreferenceWriteRequest body,
        HttpContext httpContext,
        IUserPreferenceAdminAuthorizer adminAuthorizer,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var actor = await adminAuthorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
        if (body.Json.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return api.FromFailure(new SemanticError(UserPreferenceErrorCodes.UiPreferenceJsonRequired));
        }

        var result = await sender.Send(
            new UpsertUiPreferenceCommand(actor, key, body.Json.GetRawText()),
            cancellationToken);
        return api.From(result);
    }
}

/// <summary>بدنهٔ نوشتن ترجیح UI.</summary>
public sealed record UiPreferenceWriteRequest(JsonElement Json);
