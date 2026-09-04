using Tooba.BuildingBlocks;
using Tooba.Host.Admin;
using Tooba.Media.Application;

namespace Tooba.Host.Media;

/// <summary>مرزهای HTTP مدیریتی و ارائهٔ باینری Media DAM.</summary>
public static class MediaEndpoints
{
    /// <summary>مسیرهای Admin Media و ارائهٔ عمومی را ثبت می‌کند.</summary>
    public static void MapMediaEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/v1/admin/media");
        admin.MapPost("/upload", UploadAsync).DisableAntiforgery();
        admin.MapGet("/", QueryAsync);
        admin.MapGet("/{id:guid}", GetAsync);

        app.MapGet("/v1/media/{id:guid}", ServeAsync);
    }

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);

    private static async Task<IResult> UploadAsync(
        HttpRequest request,
        IMediaDirectory directory,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            var actorUserId = await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);

            if (!request.HasFormContentType)
            {
                return Results.Json(
                    new { title = "درخواست multipart لازم است.", errorCode = "media.upload.failed" },
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var form = await request.ReadFormAsync(cancellationToken);
            var files = form.Files.GetFiles("files");
            if (files.Count == 0)
                files = form.Files.Count > 0 ? form.Files : Array.Empty<IFormFile>();

            if (files.Count == 0)
            {
                return Results.Json(
                    new { title = "هیچ فایلی برای آپلود ارسال نشده است.", errorCode = "media.upload.failed" },
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var results = new List<object>(files.Count);
            foreach (var file in files)
            {
                try
                {
                    await using var stream = file.OpenReadStream();
                    var asset = await directory.UploadAsync(
                        stream,
                        file.FileName,
                        file.ContentType ?? string.Empty,
                        actorUserId,
                        cancellationToken);
                    results.Add(new { ok = true, asset });
                }
                catch (PlatformHttpException ex)
                {
                    results.Add(new
                    {
                        ok = false,
                        fileName = file.FileName,
                        title = ex.Title,
                        errorCode = ex.ErrorCode,
                    });
                }
            }

            return Results.Json(new { items = results }, statusCode: StatusCodes.Status200OK);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    private static async Task<IResult> QueryAsync(
        IMediaDirectory directory,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        string? search = null,
        string? contentTypePrefix = null,
        string? kind = null,
        int page = 1,
        int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            var prefix = ResolveContentTypePrefix(contentTypePrefix, kind);
            return Results.Json(await directory.QueryAsync(search, page, pageSize, cancellationToken, prefix));
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
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
        IMediaDirectory directory,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            var asset = await directory.GetAsync(id, cancellationToken);
            return asset is null
                ? Results.Json(new { title = "رسانه یافت نشد.", errorCode = "media.missing" }, statusCode: StatusCodes.Status404NotFound)
                : Results.Json(asset);
        }
        catch (PlatformHttpException ex)
        {
            return ToError(ex);
        }
    }

    /// <summary>باینری دارایی Ready را برمی‌گرداند؛ در نبود null برای fallback SVG.</summary>
    internal static async Task<IResult?> TryServeStoredMediaAsync(
        Guid assetId,
        IMediaDirectory directory,
        IMediaObjectStore store,
        CancellationToken cancellationToken)
    {
        var info = await directory.GetAsync(assetId, cancellationToken);
        if (info is null)
            return null;

        var key = await directory.GetStorageKeyAsync(assetId, cancellationToken);
        if (string.IsNullOrWhiteSpace(key))
            return null;

        var stream = await store.OpenReadAsync(key, cancellationToken);
        if (stream is null)
            return null;

        return Results.File(stream, info.ContentType, enableRangeProcessing: true);
    }

    private static async Task<IResult> ServeAsync(
        Guid id,
        IMediaDirectory directory,
        IMediaObjectStore store,
        CancellationToken cancellationToken)
    {
        var served = await TryServeStoredMediaAsync(id, directory, store, cancellationToken);
        if (served is not null)
            return served;

        return PlaceholderSvg(id);
    }

    /// <summary>SVG نمایشی برای Guidهای legacy بدون دارایی واقعی.</summary>
    internal static IResult PlaceholderSvg(Guid assetId)
    {
        var hue = Math.Abs(assetId.GetHashCode()) % 40 + 200;
        var svg =
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 640 640\" role=\"img\" aria-label=\"نمایش موقت رسانه\">" +
            $"<defs><linearGradient id=\"g\" x1=\"0\" x2=\"1\"><stop offset=\"0\" stop-color=\"hsl({hue},70%,46%)\"/>" +
            $"<stop offset=\"1\" stop-color=\"hsl({hue + 20},62%,38%)\"/></linearGradient></defs>" +
            $"<rect width=\"640\" height=\"640\" rx=\"28\" fill=\"url(#g)\"/>" +
            $"<text x=\"320\" y=\"330\" text-anchor=\"middle\" fill=\"white\" font-size=\"36\" font-family=\"Tahoma\">Tooba</text>" +
            $"</svg>";
        return Results.Text(svg, "image/svg+xml; charset=utf-8");
    }
}
