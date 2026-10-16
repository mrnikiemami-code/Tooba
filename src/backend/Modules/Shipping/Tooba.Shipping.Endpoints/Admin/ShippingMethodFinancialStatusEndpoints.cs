using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Shipping.Application.Rates.Queries;

namespace Tooba.Shipping.Endpoints.Admin;

/// <summary>
/// وضعیت مالی اثبات‌شدهٔ یک روش canonical. مسیر افزودنی و فقط-خواندنی است: هیچ نرخ‌گذاری مشتری،
/// هیچ نوشتن و هیچ تغییر روی مسیرهای موجود انجام نمی‌دهد.
/// </summary>
/// <remarks>
/// هویت روش از پیوند authoritative داخل <c>serviceCode</c>/<c>optionCode</c> می‌آید که کلاینت ادمین
/// آن را از درز پیوند سرور گرفته است. مجوز همان مجوز موجود ادمین «خدمات ارسال» است؛ نبود مجوز
/// ۴۰۱/۴۰۳ می‌دهد و هیچ وضعیتی ساخته نمی‌شود (fail-closed).
/// </remarks>
public static class ShippingMethodFinancialStatusEndpoints
{
    /// <summary>مسیر خواندن وضعیت مالی روش را زیر گروه ادمین می‌نگارد.</summary>
    /// <param name="group">گروه مسیر ادمین.</param>
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/shipping-services/{serviceCode}/financial-status", ReadAsync);
    }

    private static async Task<IResult> ReadAsync(
        string serviceCode,
        string? optionCode,
        MediatR.ISender sender,
        IShippingAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, ShippingAdminPermissions.View, cancellationToken);
        return api.From(await sender.Send(
            new GetShippingMethodFinancialStatusQuery(serviceCode, optionCode),
            cancellationToken));
    }
}
