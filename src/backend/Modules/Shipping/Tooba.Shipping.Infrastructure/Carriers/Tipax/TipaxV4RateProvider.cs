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
    private readonly TipaxV4AccountTokenSource _accountTokens;

    public TipaxV4RateProvider(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<TipaxV4Options> options,
        TipaxV4TokenSource tokenSource,
        TipaxV4AccountTokenSource accountTokens)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _tokenSource = tokenSource;
        _accountTokens = accountTokens;
    }

    public string ProviderCode => Code;

    public async Task<CarrierRateResult> QuoteAsync(
        CarrierRateRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Account is { } account)
        {
            return await QuoteForAccountAsync(request, account, cancellationToken);
        }

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

        var first = await SendAsync(request.RequestId, payload!, token, options.ApiBaseUrl, options.CurrencyCode, cancellationToken);
        if (first.Outcome != CarrierRateOutcome.Unauthorized)
        {
            return first;
        }

        // Pricing is read-only. One bounded re-authentication retry is safe; no recursive retry exists.
        _tokenSource.Invalidate();
        token = await _tokenSource.GetAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(token)
            ? first
            : await SendAsync(request.RequestId, payload!, token, options.ApiBaseUrl, options.CurrencyCode, cancellationToken);
    }

    private async Task<CarrierRateResult> QuoteForAccountAsync(
        CarrierRateRequest request,
        CarrierAccountExecution account,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(account.Username) || string.IsNullOrWhiteSpace(account.Password))
        {
            return CarrierRateResult.Failure(CarrierRateOutcome.ConfigurationInvalid, "carrier.tipax.configuration_invalid");
        }

        if (!TryBuildRequest(request, account, out var payload))
        {
            return CarrierRateResult.Failure(CarrierRateOutcome.RequestInvalid, "carrier.tipax.request_invalid");
        }

        var apiBase = string.IsNullOrWhiteSpace(account.ApiBaseUrl)
            ? "https://omtestapi.tipax.ir"
            : account.ApiBaseUrl!;
        var token = await _accountTokens.GetAsync(
            account.AccountId,
            account.ConfigurationVersion,
            apiBase,
            account.TokenUrl,
            account.Username,
            account.Password,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            return CarrierRateResult.Failure(CarrierRateOutcome.Unauthorized, "carrier.tipax.authentication_failed");
        }

        var first = await SendAsync(
            request.RequestId,
            payload!,
            token,
            apiBase,
            account.CurrencyCode,
            cancellationToken);
        if (first.Outcome != CarrierRateOutcome.Unauthorized)
        {
            return first;
        }

        _accountTokens.Invalidate(account.AccountId, account.ConfigurationVersion);
        token = await _accountTokens.GetAsync(
            account.AccountId,
            account.ConfigurationVersion,
            apiBase,
            account.TokenUrl,
            account.Username,
            account.Password,
            cancellationToken);
        return string.IsNullOrWhiteSpace(token)
            ? first
            : await SendAsync(request.RequestId, payload!, token, apiBase, account.CurrencyCode, cancellationToken);
    }

    private async Task<CarrierRateResult> SendAsync(
        string requestId,
        TipaxPriceRequest payload,
        string token,
        string apiBaseUrl,
        string currencyCode,
        CancellationToken cancellationToken)
    {
        var url = new Uri(new Uri(EnsureTrailingSlash(apiBaseUrl)), PricingPath);
        using var message = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload, options: TipaxJsonContext.Default.Options)
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        message.Headers.TryAddWithoutValidation("X-Correlation-ID", requestId);

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(8));
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
            var normalized = Normalize(requestId, rows, currencyCode);
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
        return TryBuildRequest(
            request,
            paymentType.ToString(CultureInfo.InvariantCulture),
            pickupType.ToString(CultureInfo.InvariantCulture),
            distributionType.ToString(CultureInfo.InvariantCulture),
            options.PackingId.ToString(CultureInfo.InvariantCulture),
            options.PackageContentId.ToString(CultureInfo.InvariantCulture),
            options.PackType.ToString(CultureInfo.InvariantCulture),
            out payload);
    }

    private static bool TryBuildRequest(
        CarrierRateRequest request,
        CarrierAccountExecution account,
        out TipaxPriceRequest? payload)
        => TryBuildRequest(
            request,
            account.PaymentTypeId,
            account.PickupTypeId,
            account.DistributionTypeId,
            account.PackingId,
            account.PackageContentId,
            account.PackTypeId,
            out payload);

    private static bool TryBuildRequest(
        CarrierRateRequest request,
        string paymentTypeId,
        string pickupTypeId,
        string distributionTypeId,
        string packingId,
        string packageContentId,
        string packTypeId,
        out TipaxPriceRequest? payload)
    {
        payload = null;
        if (string.IsNullOrWhiteSpace(request.RequestId)
            || !int.TryParse(request.OriginLocationCode, NumberStyles.None, CultureInfo.InvariantCulture, out var origin)
            || !int.TryParse(request.DestinationLocationCode, NumberStyles.None, CultureInfo.InvariantCulture, out var destination)
            || !int.TryParse(request.ServiceCode, NumberStyles.None, CultureInfo.InvariantCulture, out var service)
            || !int.TryParse(paymentTypeId, NumberStyles.None, CultureInfo.InvariantCulture, out var paymentType)
            || !int.TryParse(pickupTypeId, NumberStyles.None, CultureInfo.InvariantCulture, out var pickupType)
            || !int.TryParse(distributionTypeId, NumberStyles.None, CultureInfo.InvariantCulture, out var distributionType)
            || !int.TryParse(packingId, NumberStyles.None, CultureInfo.InvariantCulture, out var packing)
            || !int.TryParse(packageContentId, NumberStyles.None, CultureInfo.InvariantCulture, out var packageContent)
            || !int.TryParse(packTypeId, NumberStyles.None, CultureInfo.InvariantCulture, out var packType)
            || origin <= 0
            || destination <= 0
            || service <= 0
            || paymentType <= 0
            || pickupType <= 0
            || distributionType <= 0
            || packing < 0
            || packageContent < 0
            || packType < 0
            || request.WeightGrams <= 0
            || request.DeclaredValue < 0
            || request.LengthCentimeters <= 0
            || request.WidthCentimeters <= 0
            || request.HeightCentimeters <= 0)
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
                packing,
                packageContent,
                packType,
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
