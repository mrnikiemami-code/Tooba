using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Tooba.Shipping.Application.Carriers;

/// <summary>
/// LOCAL freshness for carrier quotes when the provider does not return expiry.
/// This is not a Tipax guarantee; it is a documented Tooba policy.
/// </summary>
public static class CarrierQuoteLocalFreshness
{
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
}

/// <summary>Authoritative identity of the inputs that produced a carrier quote.</summary>
public static class CarrierQuoteInputIdentity
{
    public static string Compute(
        Guid accountId,
        int configurationVersion,
        string providerCode,
        string originLocationCode,
        string destinationLocationCode,
        string serviceCode,
        decimal weightGrams,
        decimal declaredValue,
        decimal lengthCentimeters,
        decimal widthCentimeters,
        decimal heightCentimeters,
        CarrierPaymentKind paymentKind)
    {
        var canonical = string.Join(
            "|",
            accountId.ToString("N"),
            configurationVersion.ToString(CultureInfo.InvariantCulture),
            (providerCode ?? string.Empty).Trim().ToLowerInvariant(),
            (originLocationCode ?? string.Empty).Trim(),
            (destinationLocationCode ?? string.Empty).Trim(),
            (serviceCode ?? string.Empty).Trim(),
            weightGrams.ToString("0.###", CultureInfo.InvariantCulture),
            declaredValue.ToString("0.##", CultureInfo.InvariantCulture),
            lengthCentimeters.ToString("0.###", CultureInfo.InvariantCulture),
            widthCentimeters.ToString("0.###", CultureInfo.InvariantCulture),
            heightCentimeters.ToString("0.###", CultureInfo.InvariantCulture),
            ((int)paymentKind).ToString(CultureInfo.InvariantCulture));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

/// <summary>
/// Neutral quote evidence persisted through captured checkout into Order.
/// <see cref="ProviderReference"/> is only set when the provider actually returned one.
/// </summary>
public sealed record CarrierQuoteEvidence(
    string ProviderCode,
    Guid AccountId,
    int ConfigurationVersion,
    string ServiceCode,
    string QuoteInputIdentity,
    string CurrencyCode,
    decimal FinalAmount,
    decimal TransportationAmount,
    decimal TaxAmount,
    decimal PackagingAmount,
    decimal PickupAmount,
    decimal DeliveryAmount,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset FreshUntilUtc,
    string? ProviderReference);
