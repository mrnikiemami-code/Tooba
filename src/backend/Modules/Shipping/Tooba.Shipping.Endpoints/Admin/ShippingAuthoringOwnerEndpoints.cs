using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Shipping.Application.Authoring.Queries;

namespace Tooba.Shipping.Endpoints.Admin;

/// <summary>
/// مسیر فقط-خواندنی «مالک مجازشدهٔ نویسندگی» برای ادمین. مسیر literal و بدون پارامتر است و همان
/// مجوز موجود <c>fulfillment.view</c> را می‌خواهد.
/// </summary>
/// <remarks>
/// هیچ body، query string یا پارامتر مسیری برای مالک/حالت/Tenant/فروشنده bind نمی‌شود؛ scope پس از
/// مجوزدهی سمت سرور ساخته می‌شود و همین یک use case با مسیر فروشنده مشترک است.
/// </remarks>
public static class ShippingAuthoringOwnerEndpoints
{
    /// <summary>مسیر مالک نویسندگی ادمین را زیر گروه ادمین می‌نگارد.</summary>
    /// <param name="group">گروه مسیر ادمین.</param>
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/shipping-services/authoring-owner", GetAsync);
    }

    private static async Task<IResult> GetAsync(
        MediatR.ISender sender,
        IShippingAuthoringOwnerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var scope = await authorizer.RequireAdminOwnerAsync(
            context,
            ShippingAdminPermissions.View,
            cancellationToken);

        // درخواست هیچ فیلد bind‌شده از transport ندارد؛ تنها ورودی، scope سرورساخته است.
        return api.From(await sender.Send(new GetShippingAuthoringOwnerQuery(scope), cancellationToken));
    }
}
