using MediatR;
using Microsoft.AspNetCore.Http;
using Tooba.Media.Application.Assets.Queries;
using Tooba.Media.Application.Ports;

namespace Tooba.Media.Endpoints.Admin;

/// <summary>ارائهٔ باینری دارایی Media و fallback نمایشی.</summary>
public static class MediaAssetServing
{
    /// <summary>باینری دارایی را از طریق CQRS ارائه می‌کند و در نبود SVG نمایشی برمی‌گرداند.</summary>
    public static async Task<IResult> ServeAsync(
        Guid id,
        ISender sender,
        IMediaObjectStore store,
        CancellationToken cancellationToken)
    {
        var asset = await sender.Send(new GetMediaAssetQuery(id), cancellationToken);
        if (asset.IsFailure)
            return PlaceholderSvg(id);

        var keyResult = await sender.Send(new GetMediaStorageKeyQuery(id), cancellationToken);
        if (keyResult.IsFailure || string.IsNullOrWhiteSpace(keyResult.Value))
            return PlaceholderSvg(id);

        var stream = await store.OpenReadAsync(keyResult.Value!, cancellationToken);
        if (stream is null)
            return PlaceholderSvg(id);

        return Results.File(stream, asset.Value.ContentType, enableRangeProcessing: true);
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
