using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Localization.Application.Languages.Commands;
using Tooba.Localization.Application.Languages.Queries;

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
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        await adminAuthorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
        return api.From(await sender.Send(new ListLanguagesAdminQuery(), cancellationToken));
    }

    private static async Task<IResult> CreateAsync(
        LanguageWriteRequest body,
        HttpContext httpContext,
        ILocalizationAdminAuthorizer adminAuthorizer,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        await adminAuthorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
        return api.From(await sender.Send(
            new CreateLanguageCommand(
                body.Code ?? "",
                body.UrlPrefix ?? "",
                body.DisplayName ?? "",
                body.NativeName ?? "",
                body.Direction ?? "rtl",
                body.Culture ?? body.Code ?? "",
                body.CalendarDisplay ?? "Jalali",
                body.Active ?? true,
                body.IsDefault ?? false,
                body.SortOrder ?? 0),
            cancellationToken));
    }

    private static async Task<IResult> UpdateAsync(
        string code,
        LanguageWriteRequest body,
        HttpContext httpContext,
        ILocalizationAdminAuthorizer adminAuthorizer,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        await adminAuthorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
        return api.From(await sender.Send(
            new UpdateLanguageCommand(
                code,
                body.Code,
                body.UrlPrefix,
                body.DisplayName ?? "",
                body.NativeName ?? "",
                body.Direction ?? "rtl",
                body.Culture ?? code,
                body.CalendarDisplay ?? "Jalali",
                body.Active ?? true,
                body.IsDefault ?? false,
                body.SortOrder ?? 0),
            cancellationToken));
    }

    private static async Task<IResult> PatchAsync(
        string code,
        LocalePatchRequest body,
        HttpContext httpContext,
        ILocalizationAdminAuthorizer adminAuthorizer,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        await adminAuthorizer.RequireAuthorizedAsync(httpContext, cancellationToken);
        return api.From(await sender.Send(
            new PatchLanguageCommand(code, body.Active, body.IsDefault, body.SortOrder),
            cancellationToken));
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
