using System.Text.Json.Serialization;

namespace Tooba.Shipping.Infrastructure.Carriers.Tipax;

internal sealed record TipaxPriceRequest(
    [property: JsonPropertyName("packageInputs")] TipaxPackageInput[] PackageInputs,
    [property: JsonPropertyName("discountCode")] string? DiscountCode);

internal sealed record TipaxPackageInput(
    [property: JsonPropertyName("origin")] TipaxCity Origin,
    [property: JsonPropertyName("destination")] TipaxCity Destination,
    [property: JsonPropertyName("weight")] decimal Weight,
    [property: JsonPropertyName("packageValue")] decimal PackageValue,
    [property: JsonPropertyName("length")] decimal Length,
    [property: JsonPropertyName("width")] decimal Width,
    [property: JsonPropertyName("height")] decimal Height,
    [property: JsonPropertyName("packingId")] int PackingId,
    [property: JsonPropertyName("packageContentId")] int PackageContentId,
    [property: JsonPropertyName("packType")] int PackType,
    [property: JsonPropertyName("paymentType")] int PaymentType,
    [property: JsonPropertyName("pickupType")] int PickupType,
    [property: JsonPropertyName("distributionType")] int DistributionType,
    [property: JsonPropertyName("serviceId")] int ServiceId,
    [property: JsonPropertyName("parcelBookId")] long? ParcelBookId,
    [property: JsonPropertyName("integrationCost")] decimal IntegrationCost);

internal sealed record TipaxCity([property: JsonPropertyName("cityId")] int CityId);

internal sealed record TipaxPriceResponse(
    [property: JsonPropertyName("packNum")] int PackNum,
    [property: JsonPropertyName("regularRate")] TipaxRate? RegularRate,
    [property: JsonPropertyName("regularPlusRate")] TipaxRate? RegularPlusRate,
    [property: JsonPropertyName("expressRate")] TipaxRate? ExpressRate,
    [property: JsonPropertyName("sameDayExpressRate")] TipaxRate? SameDayExpressRate,
    [property: JsonPropertyName("airExpressRate")] TipaxRate? AirExpressRate);

internal sealed record TipaxRate(
    [property: JsonPropertyName("serviceId")] int ServiceId,
    [property: JsonPropertyName("serviceTitle")] string? ServiceTitle,
    [property: JsonPropertyName("transportationCost")] decimal TransportationCost,
    [property: JsonPropertyName("packing")] decimal Packing,
    [property: JsonPropertyName("tax")] decimal Tax,
    [property: JsonPropertyName("finalPrice")] decimal FinalPrice,
    [property: JsonPropertyName("pickupPrice")] decimal PickupPrice,
    [property: JsonPropertyName("deliveryPrice")] decimal DeliveryPrice);

[JsonSerializable(typeof(TipaxPriceRequest))]
[JsonSerializable(typeof(TipaxPriceResponse[]))]
internal sealed partial class TipaxJsonContext : JsonSerializerContext;
