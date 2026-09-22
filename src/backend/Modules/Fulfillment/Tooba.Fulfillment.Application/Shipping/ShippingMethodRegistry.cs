using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tooba.Fulfillment.Contracts.Shipping;

namespace Tooba.Fulfillment.Application.Shipping;

/// <summary>
/// فیلدهای نرمال‌شده پست (بدون booking زنده).
/// </summary>
public sealed record PostShipmentMetadata(
    string? ServiceType,
    int? PackageCount,
    decimal? WeightKg,
    string? Dimensions,
    decimal? DeclaredValue,
    string? RecipientName,
    string? RecipientPhone,
    string? PostalCode,
    string? DestinationAddress,
    string? Notes);

/// <summary>
/// فیلدهای تیپاکس.
/// </summary>
public sealed record TipaxShipmentMetadata(
    int? PackageCount,
    decimal? WeightKg,
    string? Dimensions,
    decimal? DeclaredValue,
    string? SenderContact,
    string? RecipientName,
    string? RecipientPhone,
    string? Province,
    string? City,
    string? PostalCode,
    string? FullAddress,
    string? ServiceType,
    string? Notes);

/// <summary>
/// فیلدهای پیک آنلاین / اسنپ.
/// </summary>
public sealed record CourierShipmentMetadata(
    string? PickupAddress,
    string? DestinationAddress,
    string? PickupContactName,
    string? PickupContactPhone,
    string? RecipientName,
    string? RecipientPhone,
    string? PickupWindow,
    string? PackageDescription,
    string? DriverNote,
    string? ExternalReference);

/// <summary>
/// پیک فروشگاه.
/// </summary>
public sealed record StoreCourierShipmentMetadata(
    string? CourierName,
    string? CourierPhone,
    string? Note,
    string? DeliveryReference);

/// <summary>
/// تحویل حضوری.
/// </summary>
public sealed record InPersonShipmentMetadata(
    string? PickupLocation,
    string? ReadyNote);

/// <summary>
/// اعتبارسنجی متادیتای provider برای ایجاد مرسوله.
/// </summary>
public static class ShippingProviderMetadataValidator
{
    private static readonly Regex PhonePattern = new(@"^[\d\s+\-()]{8,20}$", RegexOptions.Compiled);
    private static readonly Regex PostalPattern = new(@"^\d{5,10}$", RegexOptions.Compiled);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// متادیتا را برای روش مشخص اعتبارسنجی و نرمال می‌کند؛ بدون fake booking.
    /// </summary>
    public static string? ValidateAndNormalize(string methodCode, string? rawJson)
    {
        var method = ShippingMethodRegistry.Find(methodCode)
            ?? throw new InvalidOperationException("fulfillment.shipping_method.unsupported");

        rawJson = string.IsNullOrWhiteSpace(rawJson) ? "{}" : rawJson.Trim();
        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;

        return method.Code switch
        {
            "post" => NormalizePost(root),
            "tipax" => NormalizeTipax(root),
            "snapp_courier" => NormalizeCourier(root),
            "store_courier" => NormalizeStoreCourier(root),
            "in_person" => NormalizeInPerson(root),
            _ => throw new InvalidOperationException("fulfillment.shipping_method.unsupported"),
        };
    }

    private static string NormalizePost(JsonElement root)
    {
        var recipientName = Read(root, "recipientName", "RecipientName");
        var recipientPhone = Read(root, "recipientPhone", "RecipientPhone");
        var postalCode = Read(root, "postalCode", "PostalCode");
        var address = Read(root, "destinationAddress", "DestinationAddress");
        if (string.IsNullOrWhiteSpace(recipientName))
        {
            throw new InvalidOperationException("fulfillment.shipping.post.recipient_required");
        }

        if (!string.IsNullOrWhiteSpace(recipientPhone) && !PhonePattern.IsMatch(recipientPhone))
        {
            throw new InvalidOperationException("fulfillment.shipping.mobile_invalid");
        }

        if (!string.IsNullOrWhiteSpace(postalCode) && !PostalPattern.IsMatch(postalCode))
        {
            throw new InvalidOperationException("fulfillment.shipping.postal_invalid");
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            throw new InvalidOperationException("fulfillment.shipping.post.address_required");
        }

        var meta = new PostShipmentMetadata(
            Read(root, "serviceType", "ServiceType"),
            ReadInt(root, "packageCount", "PackageCount"),
            ReadDecimal(root, "weightKg", "WeightKg"),
            Read(root, "dimensions", "Dimensions"),
            ReadDecimal(root, "declaredValue", "DeclaredValue"),
            recipientName,
            recipientPhone,
            postalCode,
            address,
            Read(root, "notes", "Notes"));
        return JsonSerializer.Serialize(meta, JsonOptions);
    }

    private static string NormalizeTipax(JsonElement root)
    {
        var recipientName = Read(root, "recipientName", "RecipientName");
        var recipientPhone = Read(root, "recipientPhone", "RecipientPhone");
        var address = Read(root, "fullAddress", "FullAddress");
        if (string.IsNullOrWhiteSpace(recipientName) || string.IsNullOrWhiteSpace(address))
        {
            throw new InvalidOperationException("fulfillment.shipping.tipax.address_required");
        }

        if (!string.IsNullOrWhiteSpace(recipientPhone) && !PhonePattern.IsMatch(recipientPhone))
        {
            throw new InvalidOperationException("fulfillment.shipping.mobile_invalid");
        }

        var postal = Read(root, "postalCode", "PostalCode");
        if (!string.IsNullOrWhiteSpace(postal) && !PostalPattern.IsMatch(postal))
        {
            throw new InvalidOperationException("fulfillment.shipping.postal_invalid");
        }

        var meta = new TipaxShipmentMetadata(
            ReadInt(root, "packageCount", "PackageCount"),
            ReadDecimal(root, "weightKg", "WeightKg"),
            Read(root, "dimensions", "Dimensions"),
            ReadDecimal(root, "declaredValue", "DeclaredValue"),
            Read(root, "senderContact", "SenderContact"),
            recipientName,
            recipientPhone,
            Read(root, "province", "Province"),
            Read(root, "city", "City"),
            postal,
            address,
            Read(root, "serviceType", "ServiceType"),
            Read(root, "notes", "Notes"));
        return JsonSerializer.Serialize(meta, JsonOptions);
    }

    private static string NormalizeCourier(JsonElement root)
    {
        var pickup = Read(root, "pickupAddress", "PickupAddress");
        var dest = Read(root, "destinationAddress", "DestinationAddress");
        if (string.IsNullOrWhiteSpace(pickup) || string.IsNullOrWhiteSpace(dest))
        {
            throw new InvalidOperationException("fulfillment.shipping.courier.address_required");
        }

        var meta = new CourierShipmentMetadata(
            pickup,
            dest,
            Read(root, "pickupContactName", "PickupContactName"),
            Read(root, "pickupContactPhone", "PickupContactPhone"),
            Read(root, "recipientName", "RecipientName"),
            Read(root, "recipientPhone", "RecipientPhone"),
            Read(root, "pickupWindow", "PickupWindow"),
            Read(root, "packageDescription", "PackageDescription"),
            Read(root, "driverNote", "DriverNote"),
            Read(root, "externalReference", "ExternalReference"));
        return JsonSerializer.Serialize(meta, JsonOptions);
    }

    private static string NormalizeStoreCourier(JsonElement root)
    {
        var meta = new StoreCourierShipmentMetadata(
            Read(root, "courierName", "CourierName"),
            Read(root, "courierPhone", "CourierPhone"),
            Read(root, "note", "Note"),
            Read(root, "deliveryReference", "DeliveryReference"));
        return JsonSerializer.Serialize(meta, JsonOptions);
    }

    private static string NormalizeInPerson(JsonElement root)
    {
        var location = Read(root, "pickupLocation", "PickupLocation");
        if (string.IsNullOrWhiteSpace(location))
        {
            throw new InvalidOperationException("fulfillment.shipping.pickup.location_required");
        }

        var meta = new InPersonShipmentMetadata(location, Read(root, "readyNote", "ReadyNote"));
        return JsonSerializer.Serialize(meta, JsonOptions);
    }

    private static string? Read(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String)
            {
                var v = prop.GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(v))
                {
                    return v;
                }
            }
        }

        return null;
    }

    private static int? ReadInt(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var prop) && prop.TryGetInt32(out var v))
            {
                return v;
            }
        }

        return null;
    }

    private static decimal? ReadDecimal(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var prop) && prop.TryGetDecimal(out var v))
            {
                return v;
            }
        }

        return null;
    }
}
