using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Shipping.Application.Rates.Commands;
using Tooba.Shipping.Application.Validation;
using Tooba.Shipping.Contracts.Admin;
using Tooba.Shipping.Endpoints.Errors;
using Tooba.Shipping.Endpoints.Resources;

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
    /// <summary>
    /// کلید فیلد بدنه در پوشش اعتبارسنجی. یک بدنهٔ خوانانشدنی به هیچ خصیصهٔ خاصی نسبت داده
    /// نمی‌شود، پس با همان کلید عمومی <c>request</c> گزارش می‌شود.
    /// </summary>
    private const string BodyField = "request";

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
        group.MapPut("/shipping-services/{serviceCode}/method-coverage", SetMethodCoverageAsync);
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
        MediatR.ISender sender,
        IShippingAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
        var (body, failure) = await ReadBodyAsync<ShippingRatePolicyWriteRequest>(context, api, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        return api.From(await sender.Send(
            new CreateShippingRatePolicyCommand(serviceCode, body),
            cancellationToken));
    }

    private static async Task<IResult> UpdateRateAsync(
        string serviceCode,
        Guid ratePolicyId,
        MediatR.ISender sender,
        IShippingAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
        var (body, failure) = await ReadBodyAsync<ShippingRatePolicyWriteRequest>(context, api, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        return api.From(await sender.Send(
            new UpdateShippingRatePolicyCommand(ratePolicyId, serviceCode, body),
            cancellationToken));
    }

    private static async Task<IResult> SetRateActiveAsync(
        string serviceCode,
        Guid ratePolicyId,
        MediatR.ISender sender,
        IShippingAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
        var (body, failure) = await ReadBodyAsync<ShippingRatePolicyActiveRequest>(context, api, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        return api.From(await sender.Send(
            new SetShippingRatePolicyActiveCommand(ratePolicyId, serviceCode, body.IsActive),
            cancellationToken));
    }

    private static async Task<IResult> CreateGrantAsync(
        string serviceCode,
        MediatR.ISender sender,
        IShippingAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
        var (body, failure) = await ReadBodyAsync<ShippingCoverageGrantWriteRequest>(context, api, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        return api.From(await sender.Send(
            new CreateShippingCoverageGrantCommand(serviceCode, body),
            cancellationToken));
    }

    private static async Task<IResult> SetGrantActiveAsync(
        string serviceCode,
        Guid coverageGrantId,
        MediatR.ISender sender,
        IShippingAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
        var (body, failure) = await ReadBodyAsync<ShippingCoverageGrantActiveRequest>(context, api, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        return api.From(await sender.Send(
            new SetShippingCoverageGrantActiveCommand(coverageGrantId, serviceCode, body.IsActive),
            cancellationToken));
    }

    /// <summary>
    /// حالت تحدید جغرافیایی یک روش را ثبت می‌کند. این مسیر هیچ نرخ یا دروازهٔ مشتری‌محور را فعال
    /// نمی‌کند و پذیرش آن فقط یک تصمیم پوشش/اهلیت است.
    /// </summary>
    private static async Task<IResult> SetMethodCoverageAsync(
        string serviceCode,
        MediatR.ISender sender,
        IShippingAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
        var (body, failure) = await ReadBodyAsync<ShippingMethodCoverageWriteRequest>(context, api, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        return api.From(await sender.Send(
            new SetShippingMethodCoverageCommand(serviceCode, body),
            cancellationToken));
    }

    /// <summary>
    /// بدنه را می‌خواند و اگر بدنهٔ ارسالی خوانا نباشد (JSON نامعتبر، contentType غیر-JSON،
    /// شناسهٔ نامعتبر/تهی یا بدنهٔ کاملاً غایب) شکست اعتبارسنجی اعلام‌شده می‌سازد.
    /// </summary>
    /// <remarks>
    /// مرز اتصال پیش‌فرض بدنه را پیش از اجرای handler تفسیر می‌کند؛ شکست تفسیر به‌صورت
    /// <c>BadHttpRequestException</c> از کنترل خارج می‌شد و در handler سراسری به
    /// <c>platform.unexpected</c> ۵۰۰ نگاشت می‌گشت. ورودی بدشکل، خطای سطح کلاینت است: اینجا
    /// به همان پوشش اعلام‌شدهٔ <c>validation.failed</c> (۴۰۰) با کد فرزند پایدار تبدیل می‌شود و
    /// هیچ خطای واقعی زیرساخت/برنامه‌نویسی پوشانده نمی‌شود.
    /// </remarks>
    private static async Task<(T Body, IResult? Failure)> ReadBodyAsync<T>(
        HttpContext context,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
        where T : class
    {
        var body = await TryReadBodyAsync<T>(context, cancellationToken);
        return body is null
            ? (default!, api.FromException(InvalidBodyException()))
            : (body, null);
    }

    private static async Task<T?> TryReadBodyAsync<T>(HttpContext context, CancellationToken cancellationToken)
        where T : class
    {
        if (!context.Request.HasJsonContentType())
        {
            return null;
        }

        try
        {
            return await context.Request.ReadFromJsonAsync<T>(cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (BadHttpRequestException)
        {
            return null;
        }
    }

    private static ValidationException InvalidBodyException() =>
        new([new ValidationFailure(BodyField, ShippingValidationCodes.BodyInvalid)
        {
            ErrorCode = ShippingValidationCodes.BodyInvalid,
        }]);
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
        ShippingMethodFinancialStatusEndpoints.Map(admin);
        ShippingOperatingScheduleEndpoints.Map(admin);
        ShippingPackagingPolicyEndpoints.Map(admin);
        ShippingOperationalQuoteEndpoints.Map(admin);
        return app;
    }

    /// <summary>
    /// ثبت ارائهٔ Shipping: کاتالوگ خطای صریح <c>shipping.*</c> و مجموعهٔ منبع آن.
    /// یک‌بار در مسیر ترکیب Host فراخوانی می‌شود؛ هیچ ثبت تکراری مجاز نیست.
    /// </summary>
    /// <param name="services">مجموعهٔ سرویس.</param>
    public static IServiceCollection AddShippingEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IErrorCatalogContributor, ShippingErrorCatalogContributor>();
        services.AddSingleton<IErrorResourceSet, ShippingErrorResourceSet>();
        return services;
    }
}
