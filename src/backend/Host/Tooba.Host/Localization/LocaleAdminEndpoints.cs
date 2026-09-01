using Tooba.BuildingBlocks;
using Tooba.Host.Admin;
using Tooba.Localization.Application;
using Tooba.Localization.Domain;

namespace Tooba.Host.Localization;

/// <summary>API Admin برای رجیستری زبان پایدار DB-backed.</summary>
public static class LocaleAdminEndpoints
{
    /// <summary>مسیرهای زبان Admin را ثبت می‌کند.</summary>
    public static void MapLocaleAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/admin/languages");
        group.MapGet("/", ListAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{code}", UpdateAsync);
        group.MapPatch("/{code}", PatchAsync);
    }

    private static async Task<IResult> ListAsync(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        ILanguageDirectory directory,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            var rows = await directory.ListAdminAsync(cancellationToken);
            return Results.Json(rows.Select(ToApiModel));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> CreateAsync(
        LanguageWriteRequest body,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        ILanguageDirectory directory,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            var created = await directory.CreateAsync(new CreateLanguageCommand(
                body.Code ?? "",
                body.UrlPrefix ?? "",
                body.DisplayName ?? "",
                body.NativeName ?? "",
                body.Direction ?? "rtl",
                body.Culture ?? body.Code ?? "",
                body.CalendarDisplay ?? "Jalali",
                body.Active ?? true,
                body.IsDefault ?? false,
                body.SortOrder ?? 0), cancellationToken);
            return Results.Json(ToApiModel(created, isReferenced: false));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
        catch (InvalidOperationException ex)
        {
            return LanguageError(ex);
        }
    }

    private static async Task<IResult> UpdateAsync(
        string code,
        LanguageWriteRequest body,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        ILanguageDirectory directory,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            var updated = await directory.UpdateAsync(code, new UpdateLanguageCommand(
                body.Code,
                body.UrlPrefix,
                body.DisplayName ?? "",
                body.NativeName ?? "",
                body.Direction ?? "rtl",
                body.Culture ?? code,
                body.CalendarDisplay ?? "Jalali",
                body.Active ?? true,
                body.IsDefault ?? false,
                body.SortOrder ?? 0), cancellationToken);
            var admin = await directory.GetAdminByCodeAsync(updated.Code, cancellationToken);
            return Results.Json(admin is null ? ToApiModel(updated, false) : ToApiModel(admin));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
        catch (InvalidOperationException ex)
        {
            return LanguageError(ex);
        }
    }

    private static async Task<IResult> PatchAsync(
        string code,
        LocalePatchRequest body,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        ILanguageDirectory directory,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            var updated = await directory.PatchAsync(code, new PatchLanguageCommand(body.Active, body.IsDefault, body.SortOrder), cancellationToken);
            var admin = await directory.GetAdminByCodeAsync(updated.Code, cancellationToken);
            return Results.Json(admin is null ? ToApiModel(updated, false) : ToApiModel(admin));
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
        catch (InvalidOperationException ex)
        {
            return LanguageError(ex);
        }
    }

    private static object ToApiModel(LanguageAdminSnapshot row) => ToApiModel(row.Snapshot, row.IsReferenced, row.CanEditCode, row.CanEditUrlPrefix);

    private static object ToApiModel(LanguageSnapshot row, bool isReferenced, bool? canEditCode = null, bool? canEditUrlPrefix = null) => new
    {
        languageId = row.LanguageId,
        code = row.Code,
        urlPrefix = row.UrlPrefix,
        displayName = row.DisplayName,
        nativeName = row.NativeName,
        direction = row.Direction,
        culture = row.Culture,
        calendarDisplay = row.CalendarDisplay,
        active = row.IsActive,
        isDefault = row.IsDefault,
        sortOrder = row.SortOrder,
        createdAt = row.CreatedAt,
        updatedAt = row.UpdatedAt,
        isReferenced,
        canEditCode = canEditCode ?? !isReferenced,
        canEditUrlPrefix = canEditUrlPrefix ?? !isReferenced,
    };

    private static IResult LanguageError(InvalidOperationException ex) =>
        Results.Json(new { title = ex.Message, errorCode = ex.Message }, statusCode: StatusCodes.Status400BadRequest);
}

/// <summary>بدنهٔ PATCH زبان.</summary>
public sealed record LocalePatchRequest(bool? Active, bool? IsDefault, int? SortOrder);

/// <summary>بدنهٔ ایجاد/ویرایش زبان.</summary>
public sealed record LanguageWriteRequest(
    string? Code,
    string? UrlPrefix,
    string? DisplayName,
    string? NativeName,
    string? Direction,
    string? Culture,
    string? CalendarDisplay,
    bool? Active,
    bool? IsDefault,
    int? SortOrder);
