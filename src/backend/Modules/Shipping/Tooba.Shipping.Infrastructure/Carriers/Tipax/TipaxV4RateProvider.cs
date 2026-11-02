using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Tooba.Shipping.Application.Carriers;
using Tooba.Shipping.Application.Carriers.Ports;

namespace Tooba.Shipping.Infrastructure.Carriers.Tipax;

internal sealed class TipaxV4RateProvider : ICarrierRateProvider
{
    public const string Code = "tipax";
    private const string PricingPath = "api/OM/v4/Pricing";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<TipaxV4Options> _options;
    private readonly TipaxV4TokenSource _tokenSource;

    public TipaxV4RateProvider(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<TipaxV4Options> options,
        TipaxV4TokenSource tokenSource)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _tokenSource = tokenSource;
    }

    public string ProviderCode => Code;

    public async Task<CarrierRateResult> QuoteAsync(
        CarrierRateRequest request,
        CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;
        if (!options.Enabled)
        {
            return CarrierRateResult.Failure(CarrierRateOutcome.Disabled, "carrier.tipax.disabled");
        }

        if (!options.IsValid())
        {
            return CarrierRateResult.Failure(
                CarrierRateOutcome.ConfigurationInvalid,
                "carrier.tipax.configuration_invalid");
        }

        if (!TryBuildRequest(request, options, out var payload))
        {
            return CarrierRateResult.Failure(CarrierRateOutcome.RequestInvalid, "carrier.tipax.request_invalid");
        }

        var token = await _tokenSource.GetAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            return CarrierRateResult.Failure(CarrierRateOutcome.Unauthorized, "carrier.tipax.authentication_failed");
        }

        var first = await SendAsync(request.RequestId, payload!, token, options, cancellationToken);
        if (first.Outcome != CarrierRateOutcome.Unauthorized)
        {
            return first;
        }

        // Pricing is read-only. One bounded re-authentication retry is safe; no recursive retry exists.
        _tokenSource.Invalidate();
        token = await _tokenSource.GetAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(token)
            ? first
            : await SendAsync(request.RequestId, payload!, token, options, cancellationToken);
    }

    private async Task<CarrierRateResult> SendAsync(
        string requestId,
        TipaxPriceRequest payload,
        string token,
        TipaxV4Options options,
        CancellationToken cancellationToken)
    {
        var url = new Uri(new Uri(EnsureTrailingSlash(options.ApiBaseUrl)), PricingPath);
        using var message = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload, options: TipaxJsonContext.Default.Options)
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        message.Headers.TryAddWithoutValidation("X-Correlation-ID", requestId);

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(options.Timeout);
        try
        {
            using var response = await _httpClientFactory.CreateClient("Shipping.Tipax.Api")
                .SendAsync(message, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return CarrierRateResult.Failure(CarrierRateOutcome.Unauthorized, "carrier.tipax.unauthorized");
            }

            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity)
            {
                return CarrierRateResult.Failure(CarrierRateOutcome.ProviderRejected, "carrier.tipax.rejected");
            }

            if (!response.IsSuccessStatusCode)
            {
                return CarrierRateResult.Failure(
                    CarrierRateOutcome.TemporarilyUnavailable,
                    "carrier.tipax.unavailable");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
            var rows = await JsonSerializer.DeserializeAsync(
                stream,
                TipaxJsonContext.Default.TipaxPriceResponseArray,
                deadline.Token);
            var normalized = Normalize(requestId, rows, options.CurrencyCode);
            return normalized.Count == 0
                ? CarrierRateResult.Failure(CarrierRateOutcome.NoService, "carrier.tipax.no_service")
                : new CarrierRateResult(CarrierRateOutcome.Quoted, normalized);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return CarrierRateResult.Failure(CarrierRateOutcome.TemporarilyUnavailable, "carrier.tipax.timeout");
        }
        catch (HttpRequestException)
        {
            return CarrierRateResult.Failure(CarrierRateOutcome.TemporarilyUnavailable, "carrier.tipax.unavailable");
        }
        catch (JsonException)
        {
            return CarrierRateResult.Failure(
                CarrierRateOutcome.ProviderRejected,
                "carrier.tipax.response_invalid");
        }
    }

    private static bool TryBuildRequest(
        CarrierRateRequest request,
        TipaxV4Options options,
        out TipaxPriceRequest? payload)
    {
        payload = null;
        if (string.IsNullOrWhiteSpace(request.RequestId)
            || !int.TryParse(request.OriginLocationCode, NumberStyles.None, CultureInfo.InvariantCulture, out var origin)
            || !int.TryParse(request.DestinationLocationCode, NumberStyles.None, CultureInfo.InvariantCulture, out var destination)
            || !int.TryParse(request.ServiceCode, NumberStyles.None, CultureInfo.InvariantCulture, out var service)
            || origin <= 0
            || destination <= 0
            || service <= 0
            || request.WeightGrams <= 0
            || request.DeclaredValue < 0
            || request.LengthCentimeters <= 0
            || request.WidthCentimeters <= 0
            || request.HeightCentimeters <= 0)
        {
            return false;
        }

        var paymentType = request.PaymentKind switch
        {
            CarrierPaymentKind.Prepaid => options.PrepaidPaymentTypeId,
            CarrierPaymentKind.CollectOnDelivery => options.CollectPaymentTypeId,
            CarrierPaymentKind.CashOnDelivery => options.CashOnDeliveryPaymentTypeId,
            _ => 0
        };
        var pickupType = request.PickupKind switch
        {
            CarrierPickupKind.DoorPickup => options.DoorPickupTypeId,
            CarrierPickupKind.DropOff => options.DropOffPickupTypeId,
            _ => 0
        };
        var distributionType = request.DeliveryKind switch
        {
            CarrierDeliveryKind.DoorDelivery => options.DoorDistributionTypeId,
            CarrierDeliveryKind.BranchPickup => options.BranchDistributionTypeId,
            _ => 0
        };
        if (paymentType <= 0 || pickupType <= 0 || distributionType <= 0)
        {
            return false;
        }

        payload = new TipaxPriceRequest(
            [new TipaxPackageInput(
                new TipaxCity(origin),
                new TipaxCity(destination),
                request.WeightGrams,
                request.DeclaredValue,
                request.LengthCentimeters,
                request.WidthCentimeters,
                request.HeightCentimeters,
                options.PackingId,
                options.PackageContentId,
                options.PackType,
                paymentType,
                pickupType,
                distributionType,
                service,
                null,
                0)],
            string.IsNullOrWhiteSpace(request.DiscountCode) ? null : request.DiscountCode.Trim());
        return true;
    }

    private static IReadOnlyList<CarrierRateOption> Normalize(
        string requestId,
        TipaxPriceResponse[]? rows,
        string currencyCode)
    {
        if (rows is null || rows.Length == 0)
        {
            return Array.Empty<CarrierRateOption>();
        }

        var result = new List<CarrierRateOption>();
        foreach (var row in rows)
        {
            Add(row.PackNum, row.RegularRate);
            Add(row.PackNum, row.RegularPlusRate);
            Add(row.PackNum, row.ExpressRate);
            Add(row.PackNum, row.SameDayExpressRate);
            Add(row.PackNum, row.AirExpressRate);
        }

        return result;

        void Add(int packNumber, TipaxRate? rate)
        {
            if (rate is null || rate.ServiceId <= 0 || rate.FinalPrice < 0)
            {
                return;
            }

            result.Add(new CarrierRateOption(
                Code,
                rate.ServiceId.ToString(CultureInfo.InvariantCulture),
                rate.ServiceTitle?.Trim() ?? string.Empty,
                currencyCode.Trim().ToUpperInvariant(),
                rate.FinalPrice,
                rate.TransportationCost,
                rate.Tax,
                rate.Packing,
                rate.PickupPrice,
                rate.DeliveryPrice,
                $"tipax:{requestId}:{packNumber}:{rate.ServiceId}"));
        }
    }

    private static string EnsureTrailingSlash(string value)
        => value.EndsWith("/", StringComparison.Ordinal) ? value : value + "/";
}
