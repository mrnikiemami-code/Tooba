using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Results;
using Tooba.Localization.Application;

namespace Tooba.Localization.Endpoints.Admin;

/// <summary>مرز HTTP رجیستری زبان Admin — Host/Localization HOST_ZERO.</summary>
public static class LocaleAdminEndpoints
{
    /// <summary>مسیرهای زبان Admin را ثبت می‌کند.</summary>
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/", ListAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{code}", UpdateAsync);
        group.MapPatch("/{code}", PatchAsync);
    }

    private static async Task<IResult> ListAsync(
        HttpContext httpContext,
        ILocalizationAdminAuthorizer adminAuthorizer,
        ILanguageDirectory directory,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        try
        {
            await adminAuthorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
            var rows = await directory.ListAdminAsync(cancellationToken);
            return Results.Json(rows.Select(ToApiModel));
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

    private static async Task<IResult> CreateAsync(
        LanguageWriteRequest body,
        HttpContext httpContext,
        ILocalizationAdminAuthorizer adminAuthorizer,
        ILanguageDirectory directory,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        try
        {
            await adminAuthorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
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
            return api.FromPlatformException(ex);
        }
        catch (SemanticException ex)
        {
            return api.FromSemanticException(ex);
        }
        catch (Exception ex) when (TryMapLanguageFault(ex, out var semantic))
        {
            return api.FromSemanticException(semantic);
        }
    }

    private static async Task<IResult> UpdateAsync(
        string code,
        LanguageWriteRequest body,
        HttpContext httpContext,
        ILocalizationAdminAuthorizer adminAuthorizer,
        ILanguageDirectory directory,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        try
        {
            await adminAuthorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
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
            return api.FromPlatformException(ex);
        }
        catch (SemanticException ex)
        {
            return api.FromSemanticException(ex);
        }
        catch (Exception ex) when (TryMapLanguageFault(ex, out var semantic))
        {
            return api.FromSemanticException(semantic);
        }
    }

    private static async Task<IResult> PatchAsync(
        string code,
        LocalePatchRequest body,
        HttpContext httpContext,
        ILocalizationAdminAuthorizer adminAuthorizer,
        ILanguageDirectory directory,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        try
        {
            await adminAuthorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
            var updated = await directory.PatchAsync(code, new PatchLanguageCommand(body.Active, body.IsDefault, body.SortOrder), cancellationToken);
            var admin = await directory.GetAdminByCodeAsync(updated.Code, cancellationToken);
            return Results.Json(admin is null ? ToApiModel(updated, false) : ToApiModel(admin));
        }
        catch (PlatformHttpException ex)
        {
            return api.FromPlatformException(ex);
        }
        catch (SemanticException ex)
        {
            return api.FromSemanticException(ex);
        }
        catch (Exception ex) when (TryMapLanguageFault(ex, out var semantic))
        {
            return api.FromSemanticException(semantic);
        }
    }

    private static object ToApiModel(LanguageAdminSnapshot row) =>
        ToApiModel(row.Snapshot, row.IsReferenced, row.CanEditCode, row.CanEditUrlPrefix);

    private static object ToApiModel(
        LanguageSnapshot row,
        bool isReferenced,
        bool? canEditCode = null,
        bool? canEditUrlPrefix = null) => new
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

    private static bool TryMapLanguageFault(Exception ex, out SemanticException semantic)
    {
        semantic = null!;
        var code = ex switch
        {
            InvalidOperationException ioe => ioe.Message,
            ContractOperationException coe => coe.Code,
            _ => null,
        };
        if (string.IsNullOrWhiteSpace(code)
            || !code.StartsWith("localization.language.", StringComparison.Ordinal))
        {
            return false;
        }

        semantic = new SemanticException(new SemanticError(code));
        return true;
    }
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
