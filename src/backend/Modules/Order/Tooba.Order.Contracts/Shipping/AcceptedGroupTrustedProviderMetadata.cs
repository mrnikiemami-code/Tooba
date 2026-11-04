using System.Text.Encodings.Web;
using System.Text.Json;

namespace Tooba.Order.Contracts.Shipping;

/// <summary>
/// TB-252-R2: trusted method execution metadata from committed Order shipping-group facts.
/// Callers must never pass client ProviderMetadataJson into Fulfillment create.
/// </summary>
public static class AcceptedGroupTrustedProviderMetadata
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Builds method-specific JSON for Fulfillment ValidateAndNormalize.
    /// Returns null when the registry method does not need metadata.
    /// Throws <see cref="AcceptedGroupParcelCreateException"/> when required recipient facts are missing.
    /// </summary>
    public static string? BuildFromFacts(
        string? fulfillmentRegistryMethodCode,
        OrderCommittedCarrierShipmentFacts facts)
    {
        if (string.IsNullOrWhiteSpace(fulfillmentRegistryMethodCode))
        {
            return null;
        }

        var code = fulfillmentRegistryMethodCode.Trim();
        var recipientName = Normalize(facts.RecipientName);
        var recipientPhone = Normalize(facts.RecipientMobile);
        var address = Normalize(facts.RecipientAddress);
        var postal = Normalize(facts.PostalCode);
        var province = Normalize(facts.ProvinceName);
        var city = Normalize(facts.CityName);

        return code switch
        {
            "post" => BuildPost(recipientName, recipientPhone, address, postal),
            "tipax" => BuildTipax(recipientName, recipientPhone, address, postal, province, city),
            "snapp_courier" => BuildCourier(address, recipientName, recipientPhone),
            "store_courier" => "{}",
            "in_person" => BuildInPerson(address, city),
            _ => "{}",
        };
    }

    /// <summary>
    /// Overlay optional fulfillment-unit recipient fields when Order facts leave a required field empty.
    /// </summary>
    public static string? BuildFromFactsWithFallback(
        string? fulfillmentRegistryMethodCode,
        OrderCommittedCarrierShipmentFacts facts,
        string? fulfillmentRecipientName,
        string? fulfillmentContactMobile,
        string? fulfillmentPostalAddress,
        string? fulfillmentPostalCode,
        string? fulfillmentProvinceName,
        string? fulfillmentCityName)
    {
        var merged = facts with
        {
            RecipientName = First(facts.RecipientName, fulfillmentRecipientName) ?? facts.RecipientName,
            RecipientMobile = First(facts.RecipientMobile, fulfillmentContactMobile) ?? facts.RecipientMobile,
            RecipientAddress = First(facts.RecipientAddress, fulfillmentPostalAddress) ?? facts.RecipientAddress,
            PostalCode = First(facts.PostalCode, fulfillmentPostalCode) ?? facts.PostalCode,
            ProvinceName = First(facts.ProvinceName, fulfillmentProvinceName) ?? facts.ProvinceName,
            CityName = First(facts.CityName, fulfillmentCityName) ?? facts.CityName,
        };
        return BuildFromFacts(fulfillmentRegistryMethodCode, merged);
    }

    private static string BuildPost(
        string? recipientName,
        string? recipientPhone,
        string? address,
        string? postal)
    {
        if (string.IsNullOrWhiteSpace(recipientName))
        {
            throw new AcceptedGroupParcelCreateException("fulfillment.shipping.post.recipient_required");
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            throw new AcceptedGroupParcelCreateException("fulfillment.shipping.post.address_required");
        }

        return JsonSerializer.Serialize(
            new
            {
                recipientName,
                recipientPhone,
                destinationAddress = address,
                postalCode = postal,
            },
            JsonOptions);
    }

    private static string BuildTipax(
        string? recipientName,
        string? recipientPhone,
        string? address,
        string? postal,
        string? province,
        string? city)
    {
        if (string.IsNullOrWhiteSpace(recipientName) || string.IsNullOrWhiteSpace(address))
        {
            throw new AcceptedGroupParcelCreateException("fulfillment.shipping.tipax.address_required");
        }

        return JsonSerializer.Serialize(
            new
            {
                recipientName,
                recipientPhone,
                fullAddress = address,
                postalCode = postal,
                province,
                city,
            },
            JsonOptions);
    }

    private static string BuildCourier(string? address, string? recipientName, string? recipientPhone)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new AcceptedGroupParcelCreateException("fulfillment.shipping.courier.address_required");
        }

        return JsonSerializer.Serialize(
            new
            {
                pickupAddress = address,
                destinationAddress = address,
                recipientName,
                recipientPhone,
            },
            JsonOptions);
    }

    private static string BuildInPerson(string? address, string? city)
    {
        var location = First(address, city);
        if (string.IsNullOrWhiteSpace(location))
        {
            throw new AcceptedGroupParcelCreateException("fulfillment.shipping.pickup.location_required");
        }

        return JsonSerializer.Serialize(new { pickupLocation = location }, JsonOptions);
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? First(string? a, string? b)
        => Normalize(a) ?? Normalize(b);
}
