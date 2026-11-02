using System.Text.Json.Serialization;

namespace Tooba.Shipping.Infrastructure.Carriers.Tipax;

internal sealed record TipaxBeneficiary(
    [property: JsonPropertyName("fullName")] string FullName,
    [property: JsonPropertyName("mobile")] string Mobile);

internal sealed record TipaxClientAddress(
    [property: JsonPropertyName("cityId")] int CityId,
    [property: JsonPropertyName("fullAddress")] string FullAddress,
    [property: JsonPropertyName("latitude")] string Latitude,
    [property: JsonPropertyName("longitude")] string Longitude,
    [property: JsonPropertyName("postalCode")] string? PostalCode,
    [property: JsonPropertyName("beneficiary")] TipaxBeneficiary Beneficiary);

internal sealed record TipaxOmPackage(
    [property: JsonPropertyName("origin")] TipaxClientAddress Origin,
    [property: JsonPropertyName("destination")] TipaxClientAddress Destination,
    [property: JsonPropertyName("weight")] decimal Weight,
    [property: JsonPropertyName("packageValue")] long PackageValue,
    [property: JsonPropertyName("length")] decimal Length,
    [property: JsonPropertyName("width")] decimal Width,
    [property: JsonPropertyName("height")] decimal Height,
    [property: JsonPropertyName("packingId")] int PackingId,
    [property: JsonPropertyName("packageContentId")] int PackageContentId,
    [property: JsonPropertyName("packType")] int PackType,
    [property: JsonPropertyName("serviceId")] int ServiceId,
    [property: JsonPropertyName("paymentType")] int PaymentType,
    [property: JsonPropertyName("pickupType")] int PickupType,
    [property: JsonPropertyName("distributionType")] int DistributionType,
    [property: JsonPropertyName("enableLabelPrivacy")] bool EnableLabelPrivacy,
    [property: JsonPropertyName("cod")] decimal Cod,
    [property: JsonPropertyName("cashAmount")] decimal CashAmount,
    [property: JsonPropertyName("payInPlaceAmount")] decimal PayInPlaceAmount);

internal sealed record TipaxOmOrderRegister(
    [property: JsonPropertyName("packages")] TipaxOmPackage[] Packages,
    [property: JsonPropertyName("successCallbackUrl")] string SuccessCallbackUrl,
    [property: JsonPropertyName("failureCallbackUrl")] string FailureCallbackUrl,
    [property: JsonPropertyName("secondaryTraceCode")] string SecondaryTraceCode);

internal sealed record TipaxTrackingTitle(
    [property: JsonPropertyName("trackingCode")] string? TrackingCode,
    [property: JsonPropertyName("customTitle")] string? CustomTitle);

internal sealed record TipaxOmOrderList(
    [property: JsonPropertyName("trackingCodesWithTitles")] TipaxTrackingTitle[]? TrackingCodesWithTitles,
    [property: JsonPropertyName("correlationId")] string? CorrelationId);

internal sealed record TipaxTrackingList(
    [property: JsonPropertyName("orderId")] string? OrderId,
    [property: JsonPropertyName("trackingCode")] string? TrackingCode,
    [property: JsonPropertyName("traceCode")] string? TraceCode,
    [property: JsonPropertyName("secondaryTraceCode")] string? SecondaryTraceCode,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("statusId")] int StatusId);

[JsonSerializable(typeof(TipaxOmOrderRegister))]
[JsonSerializable(typeof(TipaxOmOrderList))]
[JsonSerializable(typeof(TipaxTrackingList))]
[JsonSerializable(typeof(TipaxTrackingList[]))]
[JsonSerializable(typeof(string[]))]
internal sealed partial class TipaxShipmentJsonContext : JsonSerializerContext;
