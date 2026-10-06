using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Results;
using Tooba.BuildingBlocks.Security;
using Tooba.Media.Application.Assets.Commands;
using Tooba.Media.Application.Assets.Models;
using Tooba.Media.Application.Assets.Queries;
using Tooba.Media.Contracts.Errors;

namespace Tooba.Media.Endpoints.Admin;

/// <summary>
/// مرزهای HTTP مدیریتی Media DAM.
/// Stable machine codes preserved for clients: <see cref="MediaErrorCodes.UploadFailed"/>,
/// <see cref="MediaErrorCodes.Missing"/>.
/// </summary>
public static class MediaAdminEndpoints
{
    /// <summary>مسیرهای Admin Media را ثبت می‌کند.</summary>
    public static void Map(IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/v1/admin/media");
        admin.MapPost("/upload", UploadAsync).DisableAntiforgery();
        admin.MapGet("/", QueryAsync);
        admin.MapGet("/{id:guid}", GetAsync);
    }

    private static async Task<IResult> UploadAsync(
        HttpRequest request,
        ISender sender,
        ApiResponseFactory api,
        IAdminPanelAccess adminAccess,
        IErrorMessageLocalizer errorLocalizer,
        IRequestLocaleResolver localeResolver,
        CancellationToken cancellationToken)
    {
        var actorUserId = await adminAccess.RequireAuthorizedAsync(request, cancellationToken);

        if (!request.HasFormContentType)
        {
            return api.FromFailure(new SemanticError(MediaErrorCodes.UploadFailed));
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var files = form.Files.GetFiles("files");
        if (files.Count == 0)
            files = form.Files.Count > 0 ? form.Files : Array.Empty<IFormFile>();

        if (files.Count == 0)
        {
            return api.FromFailure(new SemanticError(MediaErrorCodes.UploadFailed));
        }

        var culture = localeResolver.Resolve(request.Headers.AcceptLanguage.ToString());
        var results = new List<object>(files.Count);
        foreach (var file in files)
        {
            await using var stream = file.OpenReadStream();
            var outcome = await sender.Send(
                new UploadMediaAssetCommand(
                    stream,
                    file.FileName,
                    file.ContentType ?? string.Empty,
                    actorUserId),
                cancellationToken);

            if (outcome.IsSuccess)
            {
                results.Add(new { ok = true, asset = outcome.Value });
                continue;
            }

            var errorCode = outcome.Errors[0].Code;
            results.Add(new
            {
                ok = false,
                fileName = file.FileName,
                title = ResolveTitle(errorLocalizer, errorCode, culture),
                errorCode,
            });
        }

        return api.From(Result.Success(new MediaUploadBatchResponse(results)));
    }

    private static async Task<IResult> QueryAsync(
        ISender sender,
        ApiResponseFactory api,
        HttpRequest request,
        IAdminPanelAccess adminAccess,
        string? search = null,
        string? contentTypePrefix = null,
        string? kind = null,
        int page = 1,
        int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        await adminAccess.RequireAuthorizedAsync(request, cancellationToken);
        var prefix = ResolveContentTypePrefix(contentTypePrefix, kind);
        return api.From(await sender.Send(
            new QueryMediaAssetsQuery(search, prefix, page, pageSize),
            cancellationToken));
    }

    /// <summary>نگاشت contentTypePrefix یا kind=image|video|file به پیشوند ContentType.</summary>
    private static string? ResolveContentTypePrefix(string? contentTypePrefix, string? kind)
    {
        if (!string.IsNullOrWhiteSpace(contentTypePrefix))
            return contentTypePrefix.Trim();
        return kind?.Trim().ToLowerInvariant() switch
        {
            "image" => "image/",
            "video" => "video/",
            "file" => "application/pdf",
            _ => null,
        };
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        ISender sender,
        ApiResponseFactory api,
        HttpRequest request,
        IAdminPanelAccess adminAccess,
        CancellationToken cancellationToken)
    {
        await adminAccess.RequireAuthorizedAsync(request, cancellationToken);
        return api.From(await sender.Send(new GetMediaAssetQuery(id), cancellationToken));
    }

    /// <summary>
    /// Per-item batch title resolved through the canonical error localizer and request locale —
    /// never by reading the module .resx with a hard-coded culture.
    /// </summary>
    private static string ResolveTitle(
        IErrorMessageLocalizer errorLocalizer,
        string errorCode,
        System.Globalization.CultureInfo culture) =>
        errorLocalizer.Localize(
            errorCode,
            culture,
            new Dictionary<string, string?>(),
            errorCode);
}
