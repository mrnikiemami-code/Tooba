namespace Tooba.Fulfillment.Contracts.Shipping;

/// <summary>Definition of an enabled storefront shipping method.</summary>
public sealed record ShippingMethodDefinition(
    string Code,
    string LabelFa,
    string ProviderKind);

/// <summary>Rate and lead time for a storefront shipping method code.</summary>
public sealed class ShippingMethodRateOptions
{
    public string Code { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public int LeadDays { get; set; } = 1;
    public decimal? FreeAboveSubtotal { get; set; }
    public string[] AllowedProvinces { get; set; } = [];
}

/// <summary>Store-enabled shipping method configuration.</summary>
public sealed class ShippingMethodsOptions
{
    public const string SectionName = "Tooba:ShippingMethods";

    public string[] EnabledCodes { get; set; } =
    [
        "post",
        "tipax",
        "snapp_courier",
        "store_courier",
        "in_person",
    ];

    public int DefaultSellerPreparationDays { get; set; } = 1;
    public int DeliveryHorizonDays { get; set; } = 7;

    public ShippingMethodRateOptions[] Rates { get; set; } =
    [
        new() { Code = "post", BasePrice = 150_000m, LeadDays = 3 },
        new() { Code = "post:express", BasePrice = 200_000m, LeadDays = 2 },
        new() { Code = "post:standard", BasePrice = 120_000m, LeadDays = 4 },
        new() { Code = "tipax", BasePrice = 220_000m, LeadDays = 2 },
        new() { Code = "tipax:express", BasePrice = 260_000m, LeadDays = 1 },
        new() { Code = "tipax:standard", BasePrice = 180_000m, LeadDays = 3 },
        new() { Code = "snapp_courier", BasePrice = 90_000m, LeadDays = 0 },
        new() { Code = "store_courier", BasePrice = 75_000m, LeadDays = 1 },
        new() { Code = "in_person", BasePrice = 0m, LeadDays = 0 },
    ];

    public Dictionary<string, int> SellerPreparationDaysByPartyId { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Registry of shipping methods without secret/account leakage.</summary>
public static class ShippingMethodRegistry
{
    public static IReadOnlyList<ShippingMethodDefinition> All { get; } =
    [
        new("post", "پست", "post"),
        new("tipax", "تیپاکس", "tipax"),
        new("snapp_courier", "اسنپ / پیک آنلاین", "courier"),
        new("store_courier", "پیک فروشگاه", "store_courier"),
        new("in_person", "تحویل حضوری", "in_person"),
    ];

    public static IReadOnlyList<ShippingMethodDefinition> Enabled(ShippingMethodsOptions? options)
    {
        var codes = options?.EnabledCodes is { Length: > 0 } enabled
            ? enabled.Select(x => x.Trim().ToLowerInvariant()).ToHashSet(StringComparer.Ordinal)
            : All.Select(x => x.Code).ToHashSet(StringComparer.Ordinal);
        return All.Where(x => codes.Contains(x.Code)).ToArray();
    }

    public static ShippingMethodDefinition? Find(string? code) =>
        All.FirstOrDefault(x => string.Equals(x.Code, code?.Trim(), StringComparison.OrdinalIgnoreCase));

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
