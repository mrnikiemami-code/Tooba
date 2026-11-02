#pragma warning disable CS1591
using Tooba.Shipping.Domain.Authoring;

namespace Tooba.Shipping.Domain.Carriers;

/// <summary>Shipping-owned durable sender completion for one committed group (never recipient postal defaults).</summary>
public sealed class ShippingCarrierShipmentSenderCompletion
{
    public const int MaxNameLength = 256;
    public const int MaxMobileLength = 32;
    public const int MaxAddressLength = 512;
    public const int MaxPostalLength = 32;
    public const int MaxLocationLength = 64;

    public Guid CompletionId { get; private set; }
    public ShippingDraftOwnerKind OwnerKind { get; private set; }
    public string? OwnerTenantId { get; private set; }
    public Guid? OwnerSellerPartyId { get; private set; }
    public Guid CheckoutId { get; private set; }
    public Guid ShippingGroupId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Mobile { get; private set; } = string.Empty;
    public string AddressLine { get; private set; } = string.Empty;
    public string? PostalCode { get; private set; }
    public string LocationCode { get; private set; } = string.Empty;
    public DateTimeOffset CompletedAt { get; private set; }

    public static ShippingCarrierShipmentSenderCompletion Create(
        Guid completionId,
        ShippingDraftOwnerKind ownerKind,
        string? ownerTenantId,
        Guid? ownerSellerPartyId,
        Guid checkoutId,
        Guid shippingGroupId,
        string fullName,
        string mobile,
        string addressLine,
        string? postalCode,
        string locationCode,
        DateTimeOffset completedAt)
        => new()
        {
            CompletionId = completionId == Guid.Empty ? Guid.NewGuid() : completionId,
            OwnerKind = ownerKind,
            OwnerTenantId = ownerTenantId,
            OwnerSellerPartyId = ownerSellerPartyId,
            CheckoutId = checkoutId,
            ShippingGroupId = shippingGroupId,
            FullName = TrimRequired(fullName, MaxNameLength),
            Mobile = TrimRequired(mobile, MaxMobileLength),
            AddressLine = TrimRequired(addressLine, MaxAddressLength),
            PostalCode = string.IsNullOrWhiteSpace(postalCode) ? null : postalCode.Trim(),
            LocationCode = TrimRequired(locationCode, MaxLocationLength),
            CompletedAt = completedAt,
        };

    private static string TrimRequired(string value, int max)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0 || trimmed.Length > max)
        {
            throw new ArgumentException("Value length is invalid.", nameof(value));
        }

        return trimmed;
    }
}

/// <summary>Operational package facts for legacy accepted quotes without mutating Order evidence.</summary>
public sealed class ShippingCarrierShipmentPackageCompletion
{
    public Guid CompletionId { get; private set; }
    public ShippingDraftOwnerKind OwnerKind { get; private set; }
    public string? OwnerTenantId { get; private set; }
    public Guid? OwnerSellerPartyId { get; private set; }
    public Guid CheckoutId { get; private set; }
    public Guid ShippingGroupId { get; private set; }
    public decimal WeightGrams { get; private set; }
    public decimal DeclaredValue { get; private set; }
    public decimal LengthCentimeters { get; private set; }
    public decimal WidthCentimeters { get; private set; }
    public decimal HeightCentimeters { get; private set; }
    public DateTimeOffset CompletedAt { get; private set; }

    public static ShippingCarrierShipmentPackageCompletion Create(
        Guid completionId,
        ShippingDraftOwnerKind ownerKind,
        string? ownerTenantId,
        Guid? ownerSellerPartyId,
        Guid checkoutId,
        Guid shippingGroupId,
        decimal weightGrams,
        decimal declaredValue,
        decimal lengthCentimeters,
        decimal widthCentimeters,
        decimal heightCentimeters,
        DateTimeOffset completedAt)
    {
        if (weightGrams <= 0 || lengthCentimeters <= 0 || widthCentimeters <= 0 || heightCentimeters <= 0)
        {
            throw new ArgumentException("Package dimensions must be positive.");
        }

        return new ShippingCarrierShipmentPackageCompletion
        {
            CompletionId = completionId == Guid.Empty ? Guid.NewGuid() : completionId,
            OwnerKind = ownerKind,
            OwnerTenantId = ownerTenantId,
            OwnerSellerPartyId = ownerSellerPartyId,
            CheckoutId = checkoutId,
            ShippingGroupId = shippingGroupId,
            WeightGrams = weightGrams,
            DeclaredValue = declaredValue,
            LengthCentimeters = lengthCentimeters,
            WidthCentimeters = widthCentimeters,
            HeightCentimeters = heightCentimeters,
            CompletedAt = completedAt,
        };
    }
}
#pragma warning restore CS1591
