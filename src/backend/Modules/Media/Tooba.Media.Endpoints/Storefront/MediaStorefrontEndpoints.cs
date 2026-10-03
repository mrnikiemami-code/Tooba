using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.Media.Application.Ports;
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

    private static Task<IResult> ServeStorefrontMediaAsync(
        Guid assetId,
        ISender sender,
        IMediaObjectStore store,
        CancellationToken cancellationToken) =>
        MediaAssetServing.ServeAsync(assetId, sender, store, cancellationToken);
}
