using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Text.Json;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Shipping.Application.Rates.Commands;
using Tooba.Shipping.Application.Scheduling.Commands;
using Tooba.Shipping.Application.Validation;
using Tooba.Shipping.Contracts.Scheduling;

namespace Tooba.Shipping.Endpoints.Admin;

/// <summary>
/// مارس‌کنندهٔ برنامهٔ عملیاتی هفتگی Shipping. همان مرز مجوز و همان شکل بدنهٔ نرخ/پوشش را
/// بازاستفاده می‌کند؛ هیچ مسیر بی‌مجوز یا مجوز تازه‌ای ساخته نمی‌شود.
/// </summary>
public static class ShippingOperatingScheduleEndpoints
{
    /// <summary>
    /// کلید فیلد بدنه در پوشش اعتبارسنجی. یک بدنهٔ خوانانشدنی به هیچ خصیصهٔ خاصی نسبت داده
    /// نمی‌شود، پس با همان کلید عمومی <c>request</c> گزارش می‌شود.
    /// </summary>
    private const string BodyField = "request";

    /// <summary>مسیرهای برنامهٔ عملیاتی را زیر گروه ادمین می‌نگارد.</summary>
    /// <param name="group">گروه مسیر ادمین.</param>
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/shipping-services/{serviceCode}/operating-schedules", ListAsync);
        group.MapPut("/shipping-services/{serviceCode}/operating-schedules", UpsertAsync);
        group.MapPatch(
            "/shipping-services/{serviceCode}/operating-schedules/{scheduleId:guid}/active",
            SetActiveAsync);
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
        return api.From(await sender.Send(new GetShippingOperatingSchedulesQuery(serviceCode), cancellationToken));
    }

    private static async Task<IResult> UpsertAsync(
        string serviceCode,
        MediatR.ISender sender,
        IShippingAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
        var (body, failure) = await ReadBodyAsync<ShippingOperatingScheduleWriteRequest>(
            context,
            api,
            cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        return api.From(await sender.Send(
            new UpsertShippingOperatingScheduleCommand(serviceCode, body),
            cancellationToken));
    }

    private static async Task<IResult> SetActiveAsync(
        string serviceCode,
        Guid scheduleId,
        MediatR.ISender sender,
        IShippingAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, ShippingAdminPermissions.Manage, cancellationToken);
        var (body, failure) = await ReadBodyAsync<ShippingOperatingScheduleActiveRequest>(
            context,
            api,
            cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        return api.From(await sender.Send(
            new SetShippingOperatingScheduleActiveCommand(scheduleId, serviceCode, body.IsActive),
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
