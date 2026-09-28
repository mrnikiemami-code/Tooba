using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Security;
using Tooba.Media.Application;

namespace Tooba.Media.Endpoints.Admin;

/// <summary>مرزهای HTTP مدیریتی Media DAM.</summary>
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

    private static IResult ToError(PlatformHttpException ex) =>
        Results.Json(new { title = ex.Title, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);

    private static async Task<IResult> UploadAsync(
        HttpRequest request,
        IMediaDirectory directory,
        IAdminPanelAccess adminAccess,
        CancellationToken cancellationToken)
    {
        try
        {
            var actorUserId = await adminAccess.RequireAuthorizedAsync(request, cancellationToken);

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
        IAdminPanelAccess adminAccess,
        string? search = null,
        string? contentTypePrefix = null,
        string? kind = null,
        int page = 1,
        int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await adminAccess.RequireAuthorizedAsync(request, cancellationToken);
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
        IAdminPanelAccess adminAccess,
        CancellationToken cancellationToken)
    {
        try
        {
            await adminAccess.RequireAuthorizedAsync(request, cancellationToken);
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
}
