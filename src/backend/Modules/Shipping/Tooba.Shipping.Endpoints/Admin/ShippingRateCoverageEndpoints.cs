using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Shipping.Application.Rates.Commands;
using Tooba.Shipping.Contracts.Admin;

namespace Tooba.Shipping.Endpoints.Admin;

/// <summary>
/// مجوزدهی ادمین برای نویسندگی نرخ/پوشش Shipping. درز انتزاعی است تا Host همان مجوزدهی موجود
/// پنل ادمین را تزریق کند (پیاده‌سازی مرجع: HostShippingAdminAuthorizer).
/// </summary>
public interface IShippingAdminAuthorizer
{
    /// <summary>دسترسی را بررسی می‌کند و شناسهٔ عامل را برمی‌گرداند.</summary>
    /// <param name="context">زمینهٔ HTTP.</param>
    /// <param name="permission">شناسهٔ مجوز موجود پلتفرم.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<Guid> RequireAuthorizedAsync(HttpContext context, string permission, CancellationToken cancellationToken);
}

/// <summary>
/// شناسهٔ مجوزهای مصرفی این مسیرها. این ماژول هیچ مجوز تازه‌ای در کاتالوگ پلتفرم نمی‌سازد:
/// نرخ و پوشش داخل همان سطح ادمین «خدمات ارسال» مدیریت می‌شوند که از قبل با مجوزهای
/// Fulfillment محافظت شده است، بنابراین همان شناسه‌های موجود دوباره استفاده می‌شوند و هیچ
/// مسیر بی‌مجوز یا ناشناخته‌ای ایجاد نمی‌شود.
/// </summary>
public static class ShippingAdminPermissions
{
    /// <summary>مجوز خواندن (موجود در کاتالوگ پلتفرم).</summary>
    public const string View = "fulfillment.view";

    /// <summary>مجوز نوشتن (موجود در کاتالوگ پلتفرم).</summary>
    public const string Manage = "fulfillment.manage";
}

/// <summary>مارس‌کنندهٔ نرخ و پوشش ارسال؛ فقط شکل HTTP را به پیام می‌نگارد.</summary>
public static class ShippingRateCoverageEndpoints
{
    /// <summary>مسیرهای نویسندگی نرخ/پوشش را زیر گروه ادمین می‌نگارد.</summary>
    /// <param name="group">گروه مسیر ادمین.</param>
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/shipping-services/{serviceCode}/rates", ListAsync);
        group.MapPost("/shipping-services/{serviceCode}/rates", CreateRateAsync);
        group.MapPut("/shipping-services/{serviceCode}/rates/{ratePolicyId:guid}", UpdateRateAsync);
        group.MapPatch("/shipping-services/{serviceCode}/rates/{ratePolicyId:guid}/active", SetRateActiveAsync);
        group.MapPost("/shipping-services/{serviceCode}/coverage-grants", CreateGrantAsync);
        group.MapPatch(
            "/shipping-services/{serviceCode}/coverage-grants/{coverageGrantId:guid}/active",
            SetGrantActiveAsync);
    }

    private static async Task<IResult> ListAsync(
        string serviceCode,
        MediatR.ISender sender,
        IShippingAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, ShippingAdminPermissions.View, cancellationToken);
        return api.From(await sender.Send(new GetShippingRatesAndCoverageQuery(serviceCode), cancellationToken));
    }

    private static async Task<IResult> CreateRateAsync(
        string serviceCode,
        ShippingRatePolicyWriteRequest body,
        MediatR.ISender sender,
        IShippingAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
        return api.From(await sender.Send(
            new CreateShippingRatePolicyCommand(serviceCode, body),
            cancellationToken));
    }

    private static async Task<IResult> UpdateRateAsync(
        string serviceCode,
        Guid ratePolicyId,
        ShippingRatePolicyWriteRequest body,
        MediatR.ISender sender,
        IShippingAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
        return api.From(await sender.Send(
            new UpdateShippingRatePolicyCommand(ratePolicyId, serviceCode, body),
            cancellationToken));
    }

    private static async Task<IResult> SetRateActiveAsync(
        string serviceCode,
        Guid ratePolicyId,
        ShippingRatePolicyActiveRequest body,
        MediatR.ISender sender,
        IShippingAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
        return api.From(await sender.Send(
            new SetShippingRatePolicyActiveCommand(ratePolicyId, serviceCode, body.IsActive),
            cancellationToken));
    }

    private static async Task<IResult> CreateGrantAsync(
        string serviceCode,
        ShippingCoverageGrantWriteRequest body,
        MediatR.ISender sender,
        IShippingAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
        return api.From(await sender.Send(
            new CreateShippingCoverageGrantCommand(serviceCode, body),
            cancellationToken));
    }

    private static async Task<IResult> SetGrantActiveAsync(
        string serviceCode,
        Guid coverageGrantId,
        ShippingCoverageGrantActiveRequest body,
        MediatR.ISender sender,
        IShippingAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
        return api.From(await sender.Send(
            new SetShippingCoverageGrantActiveCommand(coverageGrantId, serviceCode, body.IsActive),
            cancellationToken));
    }
}

/// <summary>ترکیب نازک HTTP برای Shipping: نگاشت مسیر و ثبت وابستگی‌های لایهٔ ارائه.</summary>
public static class ShippingEndpointModule
{
    /// <summary>مسیرهای ادمین Shipping را زیر <c>/v1/admin</c> می‌نگارد.</summary>
    /// <param name="app">سازندهٔ مسیر.</param>
    public static IEndpointRouteBuilder MapShippingEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var admin = app.MapGroup("/v1/admin");
        ShippingRateCoverageEndpoints.Map(admin);
        return app;
    }
}
