using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Tooba.AccessControl.Application.Development.Seller;
using Tooba.AccessControl.Contracts.Errors;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Results;

namespace Tooba.AccessControl.Endpoints.Seller.Development;

/// <summary>
/// مسیر Development فروشنده که توسط AccessControl مالکیت می‌شود: <c>GET /v1/seller/dev-contexts</c>.
/// مجوز از Actor احرازشده و موتور مجوز می‌آید؛ هدر Seller فقط زمینه است. این سطح Development-only است
/// و خارج از Development با کد پایدار <c>seller.dev.unavailable</c> پاسخ 404 می‌دهد.
/// </summary>
public static class SellerDevContextEndpoints
{
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
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return api.FromFailure(new SemanticError(AccessControlErrorCodes.SellerDevUnavailable));
        }

        return api.From(await sender.Send(new GetSellerDevContextsQuery(), cancellationToken));
    }
}
