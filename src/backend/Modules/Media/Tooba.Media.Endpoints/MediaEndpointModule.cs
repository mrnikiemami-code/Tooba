using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Tooba.Media.Endpoints.Admin;

namespace Tooba.Media.Endpoints;

/// <summary>ترکیب نازک مالکیت HTTP ماژول Media.</summary>
public static class MediaEndpointModule
{
    /// <summary>مسیرهای Admin Media و ارائهٔ عمومی باینری را ثبت می‌کند.</summary>
    public static IEndpointRouteBuilder MapMediaModuleEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        MediaAdminEndpoints.Map(app);
        app.MapGet("/v1/media/{id:guid}", MediaAssetServing.ServeAsync);
        return app;
    }
}
