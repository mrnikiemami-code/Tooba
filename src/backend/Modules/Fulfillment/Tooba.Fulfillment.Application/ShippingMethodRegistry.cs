using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Tooba.Fulfillment.Application;

/// <summary>
/// تعریف روش ارسال فعال در رجیستری فروشگاه.
/// </summary>
public sealed record ShippingMethodDefinition(
    string Code,
    string LabelFa,
    string ProviderKind);

/// <summary>
/// گزینه‌های روش‌های ارسال فعال.
/// </summary>
public sealed class ShippingMethodsOptions
{
    /// <summary>نام بخش.</summary>
    public const string SectionName = "Tooba:ShippingMethods";

    /// <summary>کدهای فعال؛ خالی یعنی همهٔ شناخته‌شده.</summary>
    public string[] EnabledCodes { get; set; } =
    [
        "post",
        "tipax",
        "snapp_courier",
        "store_courier",
        "in_person",
    ];
}

/// <summary>
/// رجیستری روش‌های ارسال بدون secret/account.
/// </summary>
public static class ShippingMethodRegistry
{
    /// <summary>همهٔ روش‌های پشتیبانی‌شده.</summary>
    public static IReadOnlyList<ShippingMethodDefinition> All { get; } =
    [
        new("post", "پست", "post"),
        new("tipax", "تیپاکس", "tipax"),
        new("snapp_courier", "اسنپ / پیک آنلاین", "courier"),
        new("store_courier", "پیک فروشگاه", "store_courier"),
        new("in_person", "تحویل حضوری", "in_person"),
    ];

    /// <summary>روش‌های فعال بر اساس پیکربندی.</summary>
    public static IReadOnlyList<ShippingMethodDefinition> Enabled(ShippingMethodsOptions? options)
    {
        var codes = options?.EnabledCodes is { Length: > 0 } enabled
            ? enabled.Select(x => x.Trim().ToLowerInvariant()).ToHashSet(StringComparer.Ordinal)
            : All.Select(x => x.Code).ToHashSet(StringComparer.Ordinal);
        return All.Where(x => codes.Contains(x.Code)).ToArray();
    }

    /// <summary>پیدا کردن با کد.</summary>
    public static ShippingMethodDefinition? Find(string? code) =>
        All.FirstOrDefault(x => string.Equals(x.Code, code?.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>برچسب نمایشی یا خود ورودی.</summary>
    public static string ResolveLabel(string? code, string? fallbackDisplayName)
    {
        var found = Find(code);
        if (found is not null)
        {
            return found.LabelFa;
        }

        return string.IsNullOrWhiteSpace(fallbackDisplayName) ? "سایر" : fallbackDisplayName.Trim();
    }
}

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
            ?? throw new InvalidOperationException("روش ارسال پشتیبانی نمی‌شود یا غیرفعال است.");

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
            _ => throw new InvalidOperationException("روش ارسال پشتیبانی نمی‌شود."),
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
            throw new InvalidOperationException("نام گیرنده برای پست الزامی است.");
        }

        if (!string.IsNullOrWhiteSpace(recipientPhone) && !PhonePattern.IsMatch(recipientPhone))
        {
            throw new InvalidOperationException("شماره موبایل گیرنده نامعتبر است.");
        }

        if (!string.IsNullOrWhiteSpace(postalCode) && !PostalPattern.IsMatch(postalCode))
        {
            throw new InvalidOperationException("کدپستی نامعتبر است.");
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            throw new InvalidOperationException("آدرس مقصد برای پست الزامی است.");
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
            throw new InvalidOperationException("نام گیرنده و آدرس کامل برای تیپاکس الزامی است.");
        }

        if (!string.IsNullOrWhiteSpace(recipientPhone) && !PhonePattern.IsMatch(recipientPhone))
        {
            throw new InvalidOperationException("شماره موبایل گیرنده نامعتبر است.");
        }

        var postal = Read(root, "postalCode", "PostalCode");
        if (!string.IsNullOrWhiteSpace(postal) && !PostalPattern.IsMatch(postal))
        {
            throw new InvalidOperationException("کدپستی نامعتبر است.");
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
            throw new InvalidOperationException("آدرس مبدأ و مقصد برای پیک آنلاین الزامی است.");
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
            throw new InvalidOperationException("محل تحویل حضوری الزامی است.");
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
