using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Text.Json;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Shipping.Application.Operations.Queries;
using Tooba.Shipping.Application.Validation;
using Tooba.Shipping.Contracts.Operations;

namespace Tooba.Shipping.Endpoints.Admin;

/// <summary>
/// مارس‌کنندهٔ ارزیابی عملیاتی Shipping. یک مسیر فقط-خواندنی زیر همان گروه ادمین و همان مجوز
/// موجود <c>fulfillment.view</c>؛ هیچ مسیر نوشتنی، هیچ مجوز تازه و هیچ تغییر قیمت مشتری ساخته
/// نمی‌شود.
/// </summary>
public static class ShippingOperationalQuoteEndpoints
{
    /// <summary>
    /// کلید فیلد بدنه در پوشش اعتبارسنجی. یک بدنهٔ خوانانشدنی به هیچ خصیصهٔ خاصی نسبت داده
    /// نمی‌شود، پس با همان کلید عمومی <c>request</c> گزارش می‌شود.
    /// </summary>
    private const string BodyField = "request";

    /// <summary>مسیر ارزیابی عملیاتی را زیر گروه ادمین می‌نگارد.</summary>
    /// <param name="group">گروه مسیر ادمین.</param>
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapPost("/shipping-services/{serviceCode}/operational-quote", EvaluateAsync);
    }

    private static async Task<IResult> EvaluateAsync(
        string serviceCode,
        MediatR.ISender sender,
        IShippingAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireAuthorizedAsync(context, ShippingAdminPermissions.View, cancellationToken);
        var (body, failure) = await ReadBodyAsync<ShippingOperationalQuoteEvaluationRequest>(
            context,
            api,
            cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        return api.From(await sender.Send(
            new EvaluateShippingOperationalQuoteQuery(serviceCode, body),
            cancellationToken));
    }

    /// <summary>
    /// بدنه را می‌خواند و اگر بدنهٔ ارسالی خوانا نباشد (JSON نامعتبر، contentType غیر-JSON،
    /// شناسهٔ نامعتبر/تهی یا بدنهٔ کاملاً غایب) شکست اعتبارسنجی اعلام‌شده می‌سازد.
    /// </summary>
    /// <remarks>
    /// این مسیر عمداً <c>POST</c> است چون یک ارزیابی با ورودی صریح است، نه یک منبع؛ بدنهٔ خوانا
    /// نبودن یک ورودی سطح کلاینت است و باید ۴۰۰ اعلام‌شده بدهد، نه خطای بستر.
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
