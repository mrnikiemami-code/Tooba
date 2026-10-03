using Microsoft.AspNetCore.Http;
using Tooba.Media.Application.Models;
using Tooba.Media.Application.Ports;

namespace Tooba.Media.Endpoints.Admin;

/// <summary>ارائهٔ باینری دارایی Media و fallback نمایشی.</summary>
public static class MediaAssetServing
{
    /// <summary>باینری دارایی Ready را برمی‌گرداند؛ در نبود null برای fallback SVG.</summary>
    public static async Task<IResult?> TryServeStoredMediaAsync(
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

    /// <summary>باینری دارایی را ارائه می‌کند و در نبود آن SVG نمایشی برمی‌گرداند.</summary>
    public static async Task<IResult> ServeAsync(
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
    public static IResult PlaceholderSvg(Guid assetId)
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
