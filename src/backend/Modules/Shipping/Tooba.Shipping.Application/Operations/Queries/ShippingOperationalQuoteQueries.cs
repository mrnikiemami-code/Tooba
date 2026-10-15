using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Geography.Contracts.Places;
using Tooba.Shipping.Application.Rates.Ports;
using Tooba.Shipping.Contracts.Operations;
using Tooba.Shipping.Contracts.Rates;

namespace Tooba.Shipping.Application.Operations.Queries;

/// <summary>
/// ارزیابی عملیاتی تشخیصی یک خدمت ارسال: قیمت قطعی، تفکیک پرداخت، وضعیت بازهٔ حامل و وضعیت
/// صادقانهٔ ظرفیت. فقط-خواندنی است و هیچ چیزی نمی‌نویسد.
/// </summary>
/// <remarks>
/// <b>چرا یک query و نه command.</b> این عملیات هیچ حالتی را تغییر نمی‌دهد: نه سیاستی می‌سازد، نه
/// رزروی ثبت می‌کند و نه مبلغی روی سفارش می‌نویسد. مدل‌سازی آن به‌عنوان query همین مرز را در سطح
/// نوع بیان می‌کند، نه فقط در مستندات.
/// </remarks>
/// <param name="ServiceCode">کد خدمت مسیر؛ مرجع قطعی، نه مقدار بدنه.</param>
/// <param name="Request">ورودی صریح ارزیابی.</param>
public sealed record EvaluateShippingOperationalQuoteQuery(
    string ServiceCode,
    ShippingOperationalQuoteEvaluationRequest Request)
    : IRequest<Result<ShippingOperationalQuote>>;

/// <summary>هندلر ارزیابی عملیاتی.</summary>
public sealed class EvaluateShippingOperationalQuoteHandler(
    IShippingOperationalQuoteLookup lookup,
    IShippingMethodOptionValidator optionValidator,
    IShippingDestinationScopeValidator scopeValidator)
    : IRequestHandler<EvaluateShippingOperationalQuoteQuery, Result<ShippingOperationalQuote>>
{
    /// <inheritdoc />
    public async Task<Result<ShippingOperationalQuote>> Handle(
        EvaluateShippingOperationalQuoteQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Identity is validated against the real Fulfillment catalog first: an invented service or option
        // must be a declared client error, not a "not priced" answer that hides the real defect.
        var methodValid = await optionValidator.ValidateAsync(
            request.ServiceCode,
            request.Request.OptionCode,
            cancellationToken);
        if (methodValid.IsFailure)
        {
            return Result.Failure<ShippingOperationalQuote>(methodValid.Errors);
        }

        // Destination identity stays Geography-owned: the submitted ids are checked against the reference
        // list through the read-only Contracts seam, exactly like the write paths.
        var scopeValid = await scopeValidator.ValidateAsync(request.Request.Scope, cancellationToken);
        if (scopeValid.IsFailure)
        {
            return Result.Failure<ShippingOperationalQuote>(scopeValid.Errors);
        }

        var scope = request.Request.Scope;
        var destination = new DestinationIdentifiers(
            scope.CountryId,
            scope.AdministrativeAreaId,
            scope.CityId,
            scope.ServiceAreaId);

        var parcel = request.Request.WeightGrams is null && request.Request.ItemCount is null
            ? null
            : new ShippingMeasuredParcel(request.Request.WeightGrams, request.Request.ItemCount);

        // The evaluation instant is passed through explicitly; the seam never reads the system clock, so
        // the same request always produces the same answer.
        var quote = await lookup.QuoteAsync(
            new ShippingOperationalQuoteRequest(
                request.ServiceCode,
                destination,
                request.Request.CartSubtotalExclusive,
                request.Request.CurrencyCode,
                request.Request.OptionCode,
                parcel,
                request.Request.RequestedPickupAtUtc),
            cancellationToken);

        return Result.Success(quote);
    }
}
