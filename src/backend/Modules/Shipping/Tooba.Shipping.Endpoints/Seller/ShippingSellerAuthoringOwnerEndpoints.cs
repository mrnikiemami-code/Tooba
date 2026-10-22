using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Shipping.Application.Authoring.Queries;
using Tooba.Shipping.Endpoints.Admin;

namespace Tooba.Shipping.Endpoints.Seller;

/// <summary>
/// مسیر فقط-خواندنی «مالک مجازشدهٔ نویسندگی» برای فروشنده. مسیر literal و بدون پارامتر است و مالک
/// را فقط از مجوزدهی سرور می‌گیرد.
/// </summary>
/// <remarks>
/// این مسیر هیچ sellerId/ownerKind/mode/tenant از body/query نمی‌پذیرد و هدر فروشنده را هم خودش
/// parse نمی‌کند؛ همان درز canonical پلتفرم کاندید را resolve و actor را برای همان Party مجاز
/// می‌کند. در نصب SingleStore این قابلیت پیش از هر resolve فروشنده با 404 بسته می‌شود.
/// </remarks>
public static class ShippingSellerAuthoringOwnerEndpoints
{
    /// <summary>مسیر مالک نویسندگی فروشنده را زیر گروه فروشنده می‌نگارد.</summary>
    /// <param name="group">گروه مسیر فروشنده.</param>
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
        var scope = await authorizer.RequireSellerOwnerAsync(
            context,
            ShippingAdminPermissions.View,
            cancellationToken);

        // همان use case مسیر ادمین؛ فقط شکل scope (Seller) متفاوت است.
        return api.From(await sender.Send(new GetShippingAuthoringOwnerQuery(scope), cancellationToken));
    }
}
