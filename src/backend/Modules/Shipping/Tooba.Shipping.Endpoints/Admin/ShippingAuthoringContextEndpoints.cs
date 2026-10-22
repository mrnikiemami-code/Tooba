using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Shipping.Application.Authoring.Queries;

namespace Tooba.Shipping.Endpoints.Admin;

/// <summary>
/// مارس‌کنندهٔ زمینهٔ نویسندگی ادمین ارسال: یک مسیر فقط-خواندنی و literal زیر همان گروه ادمین و
/// همان مجوز موجود <c>fulfillment.view</c>.
/// </summary>
/// <remarks>
/// این مسیر هیچ پارامتر مسیر، هیچ query string و هیچ body نمی‌پذیرد؛ بنابراین هیچ override
/// مالک/حالت/tenant/زبان/ارز از سمت مرورگر وجود ندارد و حالت مؤثر، ارز و زبان‌ها فقط از زمینه و
/// Contracts authoritative سرور می‌آیند. مسیر literal است تا با مسیرهای پارامتری
/// <c>{serviceCode}</c> اشتباه نشود.
/// </remarks>
public static class ShippingAuthoringContextEndpoints
{
    /// <summary>مسیر زمینهٔ نویسندگی را زیر گروه ادمین می‌نگارد.</summary>
    /// <param name="group">گروه مسیر ادمین.</param>
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/shipping-services/authoring-context", GetAsync);
    }

    private static async Task<IResult> GetAsync(
        MediatR.ISender sender,
        IShippingAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, ShippingAdminPermissions.View, cancellationToken);

        // درخواست هیچ ورودی فراخوان‌کننده‌ای ندارد؛ هیچ binding از route/query/body در این مسیر نیست.
        return api.From(await sender.Send(new GetShippingAuthoringContextQuery(), cancellationToken));
    }
}
