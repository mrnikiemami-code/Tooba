using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.Media.Application;
using Tooba.Media.Endpoints.Admin;

namespace Tooba.Media.Endpoints.Storefront;

/// <summary>Storefront-facing Media binary serving under /v1/storefront/media.</summary>
public static class MediaStorefrontEndpoints
{
    /// <summary>Maps GET /v1/storefront/media/{assetId} (parity with Host residual).</summary>
    public static void MapMediaStorefrontEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapGet("/v1/storefront/media/{assetId:guid}", ServeStorefrontMediaAsync);
    }

    private static async Task<IResult> ServeStorefrontMediaAsync(
        Guid assetId,
        IMediaDirectory directory,
        IMediaObjectStore store,
        CancellationToken cancellationToken)
    {
        var served = await MediaAssetServing.TryServeStoredMediaAsync(
            assetId, directory, store, cancellationToken);
        return served ?? MediaAssetServing.PlaceholderSvg(assetId);
    }
}
