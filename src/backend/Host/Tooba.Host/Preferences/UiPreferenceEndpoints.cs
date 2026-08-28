using System.Text.Json;
using Tooba.BuildingBlocks;
using Tooba.Host.Admin;
using Tooba.UserPreference.Application;
using Tooba.UserPreference.Domain;

namespace Tooba.Host.Preferences;

/// <summary>
/// مرز HTTP ترجیح کلیددار UI برای اپراتور Admin؛ مالکیت فقط از Actor حل‌شده می‌آید.
/// </summary>
public static class UiPreferenceEndpoints
{
    /// <summary>مسیرهای ترجیح UI Admin را ثبت می‌کند.</summary>
    public static void MapUiPreferenceEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/v1/admin/ui-preferences");
        admin.MapGet("/{key}", GetAdminUiPreferenceAsync);
        admin.MapPut("/{key}", PutAdminUiPreferenceAsync);
    }

    private static async Task<IResult> GetAdminUiPreferenceAsync(
        string key,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        IUiPreferenceDirectory directory,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            var normalized = UiPreference.NormalizeKey(key);
            var snapshot = await directory.GetAsync(actor, normalized, cancellationToken);
            if (snapshot is null)
            {
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
            return Results.Json(new { title = ex.Message, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
        catch (InvalidOperationException)
        {
            return Results.Json(
                new { title = "Rejected", errorCode = "ui_preference.rejected" },
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (JsonException)
        {
            return Results.Json(
                new { title = "Rejected", errorCode = "ui_preference.invalid_json" },
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> PutAdminUiPreferenceAsync(
        string key,
        UiPreferenceWriteRequest body,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        IUiPreferenceDirectory directory,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            var normalized = UiPreference.NormalizeKey(key);
            if (body.Json.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            {
                return Results.Json(
                    new { title = "Rejected", errorCode = "ui_preference.json_required" },
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var payload = body.Json.GetRawText();
            var updated = await directory.UpsertAsync(actor, normalized, new UiPreferenceWrite(payload), cancellationToken);
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
            return Results.Json(new { title = ex.Message, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
        catch (InvalidOperationException)
        {
            return Results.Json(
                new { title = "Rejected", errorCode = "ui_preference.rejected" },
                statusCode: StatusCodes.Status400BadRequest);
        }
    }
}

/// <summary>بدنهٔ نوشتن ترجیح UI؛ مالک از Actor می‌آید.</summary>
public sealed record UiPreferenceWriteRequest(JsonElement Json);
