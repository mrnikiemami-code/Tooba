using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Shipping.Application.Validation;

namespace Tooba.Shipping.Endpoints.Admin;

/// <summary>
/// خواندن بدنهٔ نوشتن برای مسیرهای نویسندگی پیش‌نویس: یک بدنهٔ ناخوانا/غایب به همان پوشش
/// اعتبارسنجی موجود (<c>shipping.validation.body_invalid</c>) تبدیل می‌شود، نه به استثنای خام.
/// </summary>
internal static class ShippingEndpointBody
{
    private const string BodyField = "request";

    /// <summary>بدنه را می‌خواند یا یک نتیجهٔ شکست آمادهٔ ارائه برمی‌گرداند.</summary>
    /// <typeparam name="T">نوع بدنه.</typeparam>
    /// <param name="context">زمینهٔ HTTP.</param>
    /// <param name="api">سازندهٔ پاسخ.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    public static async Task<(T? Body, IResult? Failure)> ReadAsync<T>(
        HttpContext context,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(api);

        if (!context.Request.HasJsonContentType())
        {
            return (null, api.FromException(InvalidBody()));
        }

        try
        {
            var body = await context.Request.ReadFromJsonAsync<T>(cancellationToken);
            return body is null ? (null, api.FromException(InvalidBody())) : (body, null);
        }
        catch (JsonException)
        {
            return (null, api.FromException(InvalidBody()));
        }
        catch (BadHttpRequestException)
        {
            return (null, api.FromException(InvalidBody()));
        }
    }

    private static ValidationException InvalidBody() =>
        new([new ValidationFailure(BodyField, ShippingValidationCodes.BodyInvalid)
        {
            ErrorCode = ShippingValidationCodes.BodyInvalid,
        }]);
}
