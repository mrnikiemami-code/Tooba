using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.UserPreference.Application;
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
        try
        {
            var actor = await adminAuthorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
            var snapshot = await sender.Send(new GetUiPreferenceQuery(actor, key), cancellationToken);
            if (snapshot is null)
            {
                var normalized = UserPreferenceShapes.NormalizeUiKey(key);
                return Results.Json(new { key = normalized, json = (object?)null, updatedAt = (DateTimeOffset?)null });
            }

            using var document = JsonDocument.Parse(snapshot.JsonPayload);
            return Results.Json(new
            {
                key = snapshot.Key,
                json = document.RootElement.Clone(),
                updatedAt = snapshot.UpdatedAt,
            });
        }
        catch (PlatformHttpException ex)
        {
            return api.FromPlatformException(ex);
        }
        catch (SemanticException ex)
        {
            return api.FromSemanticException(ex);
        }
        catch (JsonException)
        {
            return api.FromFailure(new SemanticError(UserPreferenceErrorCodes.UiPreferenceInvalidJson));
        }
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
        try
        {
            var actor = await adminAuthorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
            if (body.Json.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            {
                return api.FromFailure(new SemanticError(UserPreferenceErrorCodes.UiPreferenceJsonRequired));
            }

            var updated = await sender.Send(
                new UpsertUiPreferenceCommand(actor, key, body.Json.GetRawText()),
                cancellationToken);
            using var document = JsonDocument.Parse(updated.JsonPayload);
            return Results.Json(new
            {
                key = updated.Key,
                json = document.RootElement.Clone(),
                updatedAt = updated.UpdatedAt,
            });
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

/// <summary>بدنهٔ نوشتن ترجیح UI.</summary>
public sealed record UiPreferenceWriteRequest(JsonElement Json);
