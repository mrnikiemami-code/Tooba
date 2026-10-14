using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.Shipping.Application.Rates.Commands;
using Tooba.Shipping.Contracts.Admin;

namespace Tooba.Shipping.Endpoints.Admin;

/// <summary>
/// مجوزدهی ادمین برای نویسندگی نرخ/پوشش Shipping. درز انتزاعی است تا Host بتواند همان
/// مجوزدهی موجود پنل ادمین را تزریق کند.
/// </summary>
public interface IShippingAdminAuthorizer
{
    /// <summary>دسترسی را بررسی می‌کند و شناسهٔ عامل را برمی‌گرداند.</summary>
    /// <param name="context">زمینهٔ HTTP.</param>
    /// <param name="permission">نام مجوز.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<Guid> RequireAuthorizedAsync(HttpContext context, string permission, CancellationToken cancellationToken);
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
        group.MapPatch("/shipping-services/{serviceCode}/coverage-grants/{coverageGrantId:guid}/active", SetGrantActiveAsync);
    }

    private static async Task<IResult> ListAsync(
        string serviceCode,
        MediatR.ISender sender,
        IShippingAdminAuthorizer authorizer,
        Tooba.BuildingBlocks.Presentation.ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, "shipping.rates.view", cancellationToken);
        return api.From(await sender.Send(new GetShippingRatesAndCoverageQuery(serviceCode), cancellationToken));
    }

    private static async Task<IResult> CreateRateAsync(
        string serviceCode,
        ShippingRatePolicyWriteRequest body,
        MediatR.ISender sender,
        IShippingAdminAuthorizer authorizer,
        Tooba.BuildingBlocks.Presentation.ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, "shipping.rates.manage", cancellationToken);
        return api.From(await sender.Send(
            new CreateShippingRatePolicyCommand(body with { ServiceCode = serviceCode }),
            cancellationToken));
    }

    private static async Task<IResult> UpdateRateAsync(
        string serviceCode,
        Guid ratePolicyId,
        ShippingRatePolicyWriteRequest body,
        MediatR.ISender sender,
        IShippingAdminAuthorizer authorizer,
        Tooba.BuildingBlocks.Presentation.ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, "shipping.rates.manage", cancellationToken);
        return api.From(await sender.Send(
            new UpdateShippingRatePolicyCommand(ratePolicyId, body with { ServiceCode = serviceCode }),
            cancellationToken));
    }

    private static async Task<IResult> SetRateActiveAsync(
        string serviceCode,
        Guid ratePolicyId,
        ShippingRatePolicyActiveRequest body,
        MediatR.ISender sender,
        IShippingAdminAuthorizer authorizer,
        Tooba.BuildingBlocks.Presentation.ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, "shipping.rates.manage", cancellationToken);
        return api.From(await sender.Send(
            new SetShippingRatePolicyActiveCommand(ratePolicyId, body.IsActive),
            cancellationToken));
    }

    private static async Task<IResult> CreateGrantAsync(
        string serviceCode,
        ShippingCoverageGrantWriteRequest body,
        MediatR.ISender sender,
        IShippingAdminAuthorizer authorizer,
        Tooba.BuildingBlocks.Presentation.ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, "shipping.coverage.manage", cancellationToken);
        return api.From(await sender.Send(
            new CreateShippingCoverageGrantCommand(body with { ServiceCode = serviceCode }),
            cancellationToken));
    }

    private static async Task<IResult> SetGrantActiveAsync(
        string serviceCode,
        Guid coverageGrantId,
        ShippingCoverageGrantActiveRequest body,
        MediatR.ISender sender,
        IShippingAdminAuthorizer authorizer,
        Tooba.BuildingBlocks.Presentation.ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, "shipping.coverage.manage", cancellationToken);
        return api.From(await sender.Send(
            new SetShippingCoverageGrantActiveCommand(coverageGrantId, body.IsActive),
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
