using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Tooba.Shipping.Application.Carriers;
using Tooba.Shipping.Application.Carriers.Ports;
using Tooba.Shipping.Contracts.Errors;

namespace Tooba.Shipping.Infrastructure.Carriers.Tipax;

/// <summary>
/// CONTROLLED Tipax V4 shipment adapter. Label download is unsupported: OpenAPI has no label/PDF route.
/// Create uses POST /api/OM/v4/OmOrder; track uses POST /api/OM/v4/Tracking/ByTrackingCode
/// and POST /api/OM/v4/Tracking/BySecondaryTraceCode/{code}; cancel uses PUT /api/OM/v4/Parcels/Cancel/{trackingCode}.
/// </summary>
internal sealed class TipaxV4ShipmentProvider(
    IHttpClientFactory httpClientFactory,
    TipaxV4AccountTokenSource accountTokens) : ICarrierShipmentProvider
{
    public const string Code = TipaxV4RateProvider.Code;
    private const string CreatePath = "api/OM/v4/OmOrder";
    private const string TrackByCodePath = "api/OM/v4/Tracking/ByTrackingCode";
    private const string CancelPathPrefix = "api/OM/v4/Parcels/Cancel/";
    private const string TrackBySecondaryPrefix = "api/OM/v4/Tracking/BySecondaryTraceCode/";

    public string ProviderCode => Code;

    public CarrierShipmentCapabilities Capabilities { get; } = new(
        Code,
        CanCreate: true,
        CanTrack: true,
        CanCancel: true,
        CanLabel: false);

    public Task<CarrierShipmentResult> CreateAsync(
        CarrierShipmentCreateRequest request,
        CancellationToken cancellationToken)
        => SendCreateAsync(request, cancellationToken);

    public Task<CarrierShipmentResult> TrackAsync(
        CarrierShipmentTrackRequest request,
        CancellationToken cancellationToken)
        => SendTrackAsync(request, cancellationToken);

    public Task<CarrierShipmentResult> CancelAsync(
        CarrierShipmentCancelRequest request,
        CancellationToken cancellationToken)
        => SendCancelAsync(request, cancellationToken);

    private async Task<CarrierShipmentResult> SendCreateAsync(
        CarrierShipmentCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryBuildRegister(request, out var payload) || payload is null)
        {
            return CarrierShipmentResult.Failure(
                CarrierShipmentOutcome.RequestInvalid,
                ShippingErrorCodes.CarrierShipmentRequestInvalid);
        }

        var token = await GetTokenAsync(request.Account, cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            return CarrierShipmentResult.Failure(
                CarrierShipmentOutcome.Unauthorized,
                "carrier.tipax.authentication_failed");
        }

        var url = Combine(request.Account.ApiBaseUrl, CreatePath);
        var first = await PostJsonAsync(
            url,
            token,
            request.OperationKey,
            payload,
            TipaxShipmentJsonContext.Default.TipaxOmOrderRegister,
            cancellationToken);
        if (first.Status == HttpStatusCode.Unauthorized)
        {
            accountTokens.Invalidate(request.Account.AccountId, request.Account.ConfigurationVersion);
            token = await GetTokenAsync(request.Account, cancellationToken);
            if (string.IsNullOrWhiteSpace(token))
            {
                return CarrierShipmentResult.Failure(
                    CarrierShipmentOutcome.Unauthorized,
                    "carrier.tipax.unauthorized");
            }

            first = await PostJsonAsync(
                url,
                token,
                request.OperationKey,
                payload,
                TipaxShipmentJsonContext.Default.TipaxOmOrderRegister,
                cancellationToken);
        }

        if (first.TimedOut)
        {
            var reconciled = await TrackBySecondaryAsync(request.Account, request.OperationKey, cancellationToken);
            return reconciled.Outcome == CarrierShipmentOutcome.Succeeded
                ? reconciled
                : CarrierShipmentResult.Failure(
                    CarrierShipmentOutcome.Unknown,
                    ShippingErrorCodes.CarrierShipmentUnknown);
        }

        return MapCreate(first);
    }

    private async Task<CarrierShipmentResult> SendTrackAsync(
        CarrierShipmentTrackRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.TrackingCode))
        {
            return await TrackByCodesAsync(request.Account, [request.TrackingCode.Trim()], cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(request.SecondaryTraceCode))
        {
            return await TrackBySecondaryAsync(request.Account, request.SecondaryTraceCode.Trim(), cancellationToken);
        }

        return CarrierShipmentResult.Failure(
            CarrierShipmentOutcome.RequestInvalid,
            ShippingErrorCodes.CarrierShipmentRequestInvalid);
    }

    private async Task<CarrierShipmentResult> SendCancelAsync(
        CarrierShipmentCancelRequest request,
        CancellationToken cancellationToken)
    {
        var tracking = request.TrackingCode?.Trim();
        if (string.IsNullOrWhiteSpace(tracking))
        {
            return CarrierShipmentResult.Failure(
                CarrierShipmentOutcome.RequestInvalid,
                ShippingErrorCodes.CarrierShipmentRequestInvalid);
        }

        var token = await GetTokenAsync(request.Account, cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            return CarrierShipmentResult.Failure(
                CarrierShipmentOutcome.Unauthorized,
                "carrier.tipax.authentication_failed");
        }

        var url = Combine(request.Account.ApiBaseUrl, CancelPathPrefix + Uri.EscapeDataString(tracking));
        using var message = new HttpRequestMessage(HttpMethod.Put, url);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        message.Headers.TryAddWithoutValidation("X-Correlation-ID", request.OperationKey);
        var raw = await SendAsync(message, cancellationToken);
        if (raw.TimedOut)
        {
            return CarrierShipmentResult.Failure(
                CarrierShipmentOutcome.Unknown,
                ShippingErrorCodes.CarrierShipmentUnknown);
        }

        if (raw.Status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return CarrierShipmentResult.Failure(CarrierShipmentOutcome.Unauthorized, "carrier.tipax.unauthorized");
        }

        if (raw.Status == HttpStatusCode.NotFound)
        {
            return CarrierShipmentResult.Failure(CarrierShipmentOutcome.NotFound, ShippingErrorCodes.CarrierShipmentNotFound);
        }

        if (!raw.IsSuccess)
        {
            return CarrierShipmentResult.Failure(
                CarrierShipmentOutcome.ProviderRejected,
                "carrier.tipax.rejected");
        }

        return new CarrierShipmentResult(
            CarrierShipmentOutcome.Succeeded,
            TrackingCode: tracking,
            ProviderStatus: "cancelled");
    }

    private async Task<CarrierShipmentResult> TrackByCodesAsync(
        CarrierAccountExecution account,
        string[] codes,
        CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync(account, cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            return CarrierShipmentResult.Failure(
                CarrierShipmentOutcome.Unauthorized,
                "carrier.tipax.authentication_failed");
        }

        var url = Combine(account.ApiBaseUrl, TrackByCodePath);
        var raw = await PostJsonAsync(
            url,
            token,
            codes[0],
            codes,
            TipaxShipmentJsonContext.Default.StringArray,
            cancellationToken);
        return MapTrack(raw);
    }

    private async Task<CarrierShipmentResult> TrackBySecondaryAsync(
        CarrierAccountExecution account,
        string secondary,
        CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync(account, cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
        {
            return CarrierShipmentResult.Failure(
                CarrierShipmentOutcome.Unauthorized,
                "carrier.tipax.authentication_failed");
        }

        var url = Combine(account.ApiBaseUrl, TrackBySecondaryPrefix + Uri.EscapeDataString(secondary));
        using var message = new HttpRequestMessage(HttpMethod.Post, url);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var raw = await SendAsync(message, cancellationToken);
        return MapTrack(raw);
    }

    private static CarrierShipmentResult MapCreate(RawResponse raw)
    {
        if (raw.Status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return CarrierShipmentResult.Failure(CarrierShipmentOutcome.Unauthorized, "carrier.tipax.unauthorized");
        }

        if (raw.Status is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity)
        {
            return CarrierShipmentResult.Failure(CarrierShipmentOutcome.ProviderRejected, "carrier.tipax.rejected");
        }

        if (!raw.IsSuccess || string.IsNullOrWhiteSpace(raw.Body))
        {
            return CarrierShipmentResult.Failure(
                CarrierShipmentOutcome.TemporarilyUnavailable,
                "carrier.tipax.unavailable");
        }

        try
        {
            var parsed = JsonSerializer.Deserialize(raw.Body, TipaxShipmentJsonContext.Default.TipaxOmOrderList);
            var tracking = parsed?.TrackingCodesWithTitles?.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.TrackingCode))
                ?.TrackingCode;
            if (string.IsNullOrWhiteSpace(tracking))
            {
                return CarrierShipmentResult.Failure(
                    CarrierShipmentOutcome.ProviderRejected,
                    "carrier.tipax.response_invalid");
            }

            return new CarrierShipmentResult(
                CarrierShipmentOutcome.Succeeded,
                TrackingCode: tracking.Trim(),
                ProviderOrderId: string.IsNullOrWhiteSpace(parsed?.CorrelationId) ? null : parsed!.CorrelationId.Trim(),
                ProviderReference: tracking.Trim());
        }
        catch (JsonException)
        {
            return CarrierShipmentResult.Failure(
                CarrierShipmentOutcome.ProviderRejected,
                "carrier.tipax.response_invalid");
        }
    }

    private static CarrierShipmentResult MapTrack(RawResponse raw)
    {
        if (raw.TimedOut)
        {
            return CarrierShipmentResult.Failure(
                CarrierShipmentOutcome.TemporarilyUnavailable,
                "carrier.tipax.timeout");
        }

        if (raw.Status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return CarrierShipmentResult.Failure(CarrierShipmentOutcome.Unauthorized, "carrier.tipax.unauthorized");
        }

        if (raw.Status == HttpStatusCode.NotFound)
        {
            return CarrierShipmentResult.Failure(CarrierShipmentOutcome.NotFound, ShippingErrorCodes.CarrierShipmentNotFound);
        }

        if (!raw.IsSuccess || string.IsNullOrWhiteSpace(raw.Body))
        {
            return CarrierShipmentResult.Failure(
                CarrierShipmentOutcome.TemporarilyUnavailable,
                "carrier.tipax.unavailable");
        }

        try
        {
            using var doc = JsonDocument.Parse(raw.Body);
            JsonElement element = doc.RootElement;
            if (element.ValueKind == JsonValueKind.Array)
            {
                if (element.GetArrayLength() == 0)
                {
                    return CarrierShipmentResult.Failure(
                        CarrierShipmentOutcome.NotFound,
                        ShippingErrorCodes.CarrierShipmentNotFound);
                }

                element = element[0];
            }

            var tracking = ReadString(element, "trackingCode");
            var status = ReadString(element, "status");
            if (string.IsNullOrWhiteSpace(status))
            {
                status = "unknown";
            }

            return new CarrierShipmentResult(
                CarrierShipmentOutcome.Succeeded,
                TrackingCode: tracking,
                ProviderOrderId: ReadString(element, "orderId"),
                ProviderStatus: status);
        }
        catch (JsonException)
        {
            return CarrierShipmentResult.Failure(
                CarrierShipmentOutcome.ProviderRejected,
                "carrier.tipax.response_invalid");
        }
    }

    private static string? ReadString(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool TryBuildRegister(CarrierShipmentCreateRequest request, out TipaxOmOrderRegister? payload)
    {
        payload = null;
        var account = request.Account;
        if (!int.TryParse(request.Sender.LocationCode, NumberStyles.None, CultureInfo.InvariantCulture, out var origin)
            || !int.TryParse(request.Recipient.LocationCode, NumberStyles.None, CultureInfo.InvariantCulture, out var destination)
            || !int.TryParse(request.ServiceCode, NumberStyles.None, CultureInfo.InvariantCulture, out var service)
            || !int.TryParse(account.PaymentTypeId, NumberStyles.None, CultureInfo.InvariantCulture, out var payment)
            || !int.TryParse(account.PickupTypeId, NumberStyles.None, CultureInfo.InvariantCulture, out var pickup)
            || !int.TryParse(account.DistributionTypeId, NumberStyles.None, CultureInfo.InvariantCulture, out var distribution)
            || !int.TryParse(account.PackingId, NumberStyles.None, CultureInfo.InvariantCulture, out var packing)
            || !int.TryParse(account.PackageContentId, NumberStyles.None, CultureInfo.InvariantCulture, out var content)
            || !int.TryParse(account.PackTypeId, NumberStyles.None, CultureInfo.InvariantCulture, out var packType)
            || origin <= 0
            || destination <= 0
            || service <= 0
            || request.Package.WeightGrams <= 0
            || request.Package.DeclaredValue < 0
            || request.Package.LengthCentimeters <= 0
            || request.Package.WidthCentimeters <= 0
            || request.Package.HeightCentimeters <= 0
            || string.IsNullOrWhiteSpace(request.Sender.FullName)
            || string.IsNullOrWhiteSpace(request.Sender.Mobile)
            || string.IsNullOrWhiteSpace(request.Sender.AddressLine)
            || string.IsNullOrWhiteSpace(request.Recipient.FullName)
            || string.IsNullOrWhiteSpace(request.Recipient.Mobile)
            || string.IsNullOrWhiteSpace(request.Recipient.AddressLine)
            || !Uri.TryCreate(request.SuccessCallbackUrl, UriKind.Absolute, out var success)
            || (success.Scheme != Uri.UriSchemeHttp && success.Scheme != Uri.UriSchemeHttps)
            || !Uri.TryCreate(request.FailureCallbackUrl, UriKind.Absolute, out var failure)
            || (failure.Scheme != Uri.UriSchemeHttp && failure.Scheme != Uri.UriSchemeHttps))
        {
            return false;
        }

        payload = new TipaxOmOrderRegister(
            [
                new TipaxOmPackage(
                    new TipaxClientAddress(
                        origin,
                        request.Sender.AddressLine.Trim(),
                        "0",
                        "0",
                        request.Sender.PostalCode,
                        new TipaxBeneficiary(request.Sender.FullName.Trim(), request.Sender.Mobile.Trim())),
                    new TipaxClientAddress(
                        destination,
                        request.Recipient.AddressLine.Trim(),
                        "0",
                        "0",
                        request.Recipient.PostalCode,
                        new TipaxBeneficiary(request.Recipient.FullName.Trim(), request.Recipient.Mobile.Trim())),
                    request.Package.WeightGrams,
                    decimal.ToInt64(decimal.Truncate(request.Package.DeclaredValue)),
                    request.Package.LengthCentimeters,
                    request.Package.WidthCentimeters,
                    request.Package.HeightCentimeters,
                    packing,
                    content,
                    packType,
                    service,
                    payment,
                    pickup,
                    distribution,
                    EnableLabelPrivacy: false,
                    request.Package.CollectAmount,
                    0m,
                    0m)
            ],
            request.SuccessCallbackUrl.Trim(),
            request.FailureCallbackUrl.Trim(),
            request.OperationKey);
        return true;
    }

    private async Task<string?> GetTokenAsync(CarrierAccountExecution account, CancellationToken cancellationToken)
    {
        var apiBase = string.IsNullOrWhiteSpace(account.ApiBaseUrl)
            ? "https://omtestapi.tipax.ir"
            : account.ApiBaseUrl!;
        return await accountTokens.GetAsync(
            account.AccountId,
            account.ConfigurationVersion,
            apiBase,
            account.TokenUrl,
            account.Username,
            account.Password,
            cancellationToken);
    }

    private async Task<RawResponse> PostJsonAsync<T>(
        Uri url,
        string token,
        string correlation,
        T body,
        System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo,
        CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body, typeInfo)
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        message.Headers.TryAddWithoutValidation("X-Correlation-ID", correlation);
        return await SendAsync(message, cancellationToken);
    }

    private async Task<RawResponse> SendAsync(HttpRequestMessage message, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(8));
        try
        {
            using var response = await httpClientFactory.CreateClient("Shipping.Tipax.Api")
                .SendAsync(message, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            var body = await response.Content.ReadAsStringAsync(deadline.Token);
            return new RawResponse(response.StatusCode, response.IsSuccessStatusCode, body, TimedOut: false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new RawResponse(0, false, null, TimedOut: true);
        }
        catch (HttpRequestException)
        {
            return new RawResponse(0, false, null, TimedOut: false);
        }
    }

    private static Uri Combine(string? apiBaseUrl, string path)
    {
        var baseUrl = string.IsNullOrWhiteSpace(apiBaseUrl) ? "https://omtestapi.tipax.ir/" : apiBaseUrl!;
        if (!baseUrl.EndsWith("/", StringComparison.Ordinal))
        {
            baseUrl += "/";
        }

        return new Uri(new Uri(baseUrl), path);
    }

    private readonly record struct RawResponse(
        HttpStatusCode Status,
        bool IsSuccess,
        string? Body,
        bool TimedOut);
}
