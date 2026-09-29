using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Tooba.AccessControl.Application.Development.Seller;

namespace Tooba.AccessControl.Endpoints.Seller.Development;

/// <summary>
/// مسیر Development فروشنده که توسط AccessControl مالکیت می‌شود: <c>GET /v1/seller/dev-contexts</c>.
/// مجوز از Actor احرازشده و موتور مجوز می‌آید؛ هدر Seller فقط زمینه است. این سطح Development-only است
/// و خارج از Development با کد پایدار <c>seller.dev.unavailable</c> پاسخ 404 می‌دهد.
/// </summary>
public static class SellerDevContextEndpoints
{
    /// <summary>کد پایدار نبودِ مسیر Development.</summary>
    public const string UnavailableCode = "seller.dev.unavailable";

    /// <summary>کد پایدار آماده‌نبودن snapshot Development.</summary>
    public const string NotReadyCode = "seller.dev.not-ready";

    /// <summary>
    /// مسیر dev-contexts فروشنده را روی گروه <c>/v1/seller</c> ثبت می‌کند.
    /// </summary>
    /// <param name="group">گروه مسیر <c>/v1/seller</c>.</param>
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/dev-contexts", GetDevContextsAsync);
    }

    private static async Task<IResult> GetDevContextsAsync(
        IHostEnvironment environment,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return Results.Json(
                new { title = "Not Found", errorCode = UnavailableCode },
                statusCode: StatusCodes.Status404NotFound);
        }

        var view = await sender.Send(new GetSellerDevContextsQuery(), cancellationToken);
        if (view is null)
        {
            return Results.Json(
                new { title = "در دسترس نیست", errorCode = NotReadyCode },
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Results.Json(view);
    }
}
