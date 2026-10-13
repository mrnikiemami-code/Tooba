using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Results;
using Tooba.Order.Application.Storefront;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Contracts.Payments;
using Tooba.Order.Contracts.Storefront;
using Tooba.Payment.Contracts.Errors;

namespace Tooba.Order.Endpoints.Storefront;

/// <summary>
/// تنها مسیر عمومی «کلیک نهایی پرداخت» فروشگاهی به‌مالکیت Order:
/// <c>POST /v1/storefront/cart/{cartId:guid}/start-payment</c>.
///
/// این لایه فقط نازک است: هویت از مرز اعتماد (gate + actor) می‌آید، گواه مهمان از هدر/کوکی متعارف، و کل
/// ترکیب commit متعارف سفارش + آماده‌سازی مهلت ۰۱۸ + شروع پرداخت ۰۱۲R به
/// <see cref="StorefrontStartPaymentComposer"/> واگذار می‌شود. هیچ قاعدهٔ موجودی/تراکنش/سهمیه/تبدیل سبد/
/// سیاست مهلت/Catalog اینجا بازپیاده نمی‌شود و هیچ کد خطایی ساخته نمی‌شود: نگاشت شکست از کاتالوگ مرکزی خطا
/// (<see cref="ApiResponseFactory"/>) می‌آید و خطاهای تایپ‌شدهٔ Order/Payment دست‌نخورده propagate می‌شوند.
/// </summary>
internal static class StorefrontStartPaymentEndpoints
{
    /// <summary>
    /// این outcome یک نقض ناوردای سمت سرور است (اجرای مسیر داخل <c>TransactionScope</c> محیطی) که از این
    /// مسیر HTTP قابل وقوع نیست، چون این لایه هیچ تراکنش محیطی نمی‌سازد. عمداً به یک توصیف‌گر <b>ثبت‌شدهٔ</b>
    /// Foundation (Platform، ۵۰۰) نگاشت می‌شود تا یک نقص سمت سرور به‌عنوان خطای ۴xx مشتری جا زده نشود؛
    /// هیچ خطای کسب‌وکاری به ۵۰۰ عمومی تبدیل نمی‌شود.
    /// </summary>
    private const string ServerInvariantViolationCode = "platform.error";

    internal static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/storefront");
        group.MapPost("/cart/{cartId:guid}/start-payment", StartPaymentAsync);
    }

    private static async Task<IResult> StartPaymentAsync(
        Guid cartId,
        StorefrontStartPaymentBody? body,
        StorefrontStartPaymentComposer composer,
        IOrderStorefrontCheckoutIdentityGate gate,
        IOrderStorefrontActor actor,
        ApiResponseFactory api,
        HttpRequest request,
        CancellationToken ct)
    {
        // هویت پرداخت‌کننده فقط از مرز اعتماد می‌آید؛ هیچ شناسهٔ کاربری از بدنه خوانده نمی‌شود.
        await gate.EnsureCheckoutActorAsync(ct);

        // بدنهٔ غایب رد می‌شود (کلید idempotency الزامی است)، نه اینکه با مقدار پیش‌فرض پذیرفته شود.
        var wire = body ?? new StorefrontStartPaymentBody(ExpectedCartVersion: 0, IdempotencyKey: string.Empty);

        var result = await composer.StartPaymentAsync(
            new StorefrontStartPaymentRequest(
                cartId,
                ReadGuestSecret(request),
                wire.ExpectedCartVersion,
                ResolveIdempotencyKey(wire.IdempotencyKey, request),
                wire.UseWallet,
                wire.ProviderCode,
                actor.AuthenticatedUserId,
                wire.CouponCode),
            ct);

        if (result.Started)
        {
            return api.From(Result.Success(new StorefrontStartPaymentResponse(
                result.CheckoutId!.Value,
                result.CheckoutReplayed,
                result.PaymentId!.Value,
                result.AttemptId!.Value,
                result.PaymentStatus ?? string.Empty,
                result.ProviderCode ?? string.Empty,
                result.RedirectUrl ?? string.Empty,
                result.Amount,
                result.Currency,
                result.RequiresPspRedirect,
                result.ReservationCount,
                result.GuaranteedUntilUtc)));
        }

        return Failure(api, ResolveFailureCode(result), result.CheckoutId);
    }

    /// <summary>
    /// نگاشت هر شکست به کد <b>ثبت‌شده و محلی‌شده</b>ی مالک واقعی آن مورد. هیچ کد خام ساخته یا
    /// بازتعریف نمی‌شود: کد commit از تولیدکنندهٔ خودش، خطاهای روش پرداخت از Payment و شکست‌های
    /// مالکیت/رزرو از Order.
    /// </summary>
    private static string ResolveFailureCode(StorefrontStartPaymentResult result) =>
        result.Outcome switch
        {
            StorefrontStartPaymentOutcome.CheckoutCommitFailed =>
                result.FailureCode ?? StorefrontOrderErrors.CheckoutRejected,
            StorefrontStartPaymentOutcome.InvalidRequest => result.Fault switch
            {
                StorefrontStartPaymentFault.CartRequired => StorefrontOrderErrors.CheckoutCartMissing,
                StorefrontStartPaymentFault.ProviderUnavailable => PaymentErrorCodes.MethodUnavailable,
                _ => StorefrontOrderErrors.CheckoutRejected,
            },
            StorefrontStartPaymentOutcome.HoldPreparationFailed => result.PreparationOutcome switch
            {
                OrderPaymentHoldPreparationOutcome.AccessDenied => StorefrontOrderErrors.CheckoutAccessDenied,
                OrderPaymentHoldPreparationOutcome.ReservationSetIncomplete
                    or OrderPaymentHoldPreparationOutcome.HoldUnavailable
                    or OrderPaymentHoldPreparationOutcome.DeadlineRejected =>
                    StorefrontOrderErrors.CheckoutInventoryUnavailable,
                OrderPaymentHoldPreparationOutcome.CycleNotPayable
                    or OrderPaymentHoldPreparationOutcome.CycleClosedAfterExtension
                    or OrderPaymentHoldPreparationOutcome.PolicyUnavailable =>
                    PaymentErrorCodes.ReservationNotPayable,
                OrderPaymentHoldPreparationOutcome.AmbientTransactionUnsupported => ServerInvariantViolationCode,
                _ => StorefrontOrderErrors.CheckoutRejected,
            },
            _ => StorefrontOrderErrors.CheckoutRejected,
        };

    /// <summary>
    /// شکست از مسیر مرکزی خطا گزارش می‌شود. وقتی سفارش مجاز و پایدار شده باشد، ارجاع آن به‌عنوان
    /// افزونهٔ استاندارد ProblemDetails نگه داشته می‌شود تا سفارش موجود قابل‌بازیابی بماند
    /// (هیچ لغو خودکار و هیچ commit دوباره‌ای انجام نمی‌شود).
    /// </summary>
    private static IResult Failure(ApiResponseFactory api, string code, Guid? checkoutId)
    {
        if (checkoutId is not Guid id || id == Guid.Empty)
        {
            return api.FromFailure(new SemanticError(code));
        }

        var problem = api.CreateProblemDetails(new SemanticException(new SemanticError(code)));
        problem.Extensions["checkoutId"] = id;
        return Results.Json(problem, statusCode: problem.Status ?? StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// کلید idempotency از بدنه یا هدر متعارف <c>Idempotency-Key</c> خوانده می‌شود و <b>هرگز ساخته
    /// نمی‌شود</b>: کلیک دوم با همان کلید باید همان سفارش/پرداخت را replay کند.
    /// </summary>
    private static string ResolveIdempotencyKey(string? bodyKey, HttpRequest request)
    {
        if (!string.IsNullOrWhiteSpace(bodyKey))
        {
            return bodyKey.Trim();
        }

        return request.Headers.TryGetValue("Idempotency-Key", out var raw) && !string.IsNullOrWhiteSpace(raw)
            ? raw.ToString().Trim()
            : string.Empty;
    }

    /// <summary>گواه مهمان بر پایهٔ قرارداد متعارف Order: هدر <c>X-Tooba-Guest-Secret</c> یا کوکی.</summary>
    private static string? ReadGuestSecret(HttpRequest request)
    {
        if (request.Headers.TryGetValue("X-Tooba-Guest-Secret", out var header) && !string.IsNullOrWhiteSpace(header))
        {
            return header.ToString();
        }

        return request.Cookies.TryGetValue("tooba_guest_secret", out var cookie) ? cookie : null;
    }
}
