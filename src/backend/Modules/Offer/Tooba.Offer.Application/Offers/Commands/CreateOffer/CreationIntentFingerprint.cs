using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using DomainChannel = Tooba.Offer.Domain.ValueObjects.SalesChannel;

namespace Tooba.Offer.Application.Offers.Commands.CreateOffer;

/// <summary>
/// TB-287: deterministic fingerprint of the immutable original creation intent.
/// It is computed only from the normalized create request — never from mutable listing state —
/// so a legitimate later edit cannot change the idempotency identity of the original create.
/// </summary>
internal static class CreationIntentFingerprint
{
    /// <summary>Version tag for the canonicalization scheme; bump if the field set or semantics change.</summary>
    private const string Version = "ci1";

    /// <summary>Computes the stable lowercase SHA-256 hex fingerprint for the normalized create request.</summary>
    public static string Compute(CreateOfferCommand request, DomainChannel channel, string requestId)
    {
        var sku = string.IsNullOrWhiteSpace(request.SellerSku) ? null : request.SellerSku.Trim();

        // Effective initial status after the create handler's normalization (only Draft/Active survive).
        var effectiveStatus = !string.IsNullOrWhiteSpace(request.Status)
            && string.Equals(request.Status.Trim(), "Active", StringComparison.OrdinalIgnoreCase)
            ? "Active"
            : "Draft";

        // Effective initial return policy exactly as SellerOffer.SetReturnPolicy would store it.
        var effectivePolicy = "Default";
        int? effectiveCustomWindow = null;
        if (request.ReturnPolicyChoice is not null || request.CustomReturnWindowDays is not null)
        {
            var raw = string.IsNullOrWhiteSpace(request.ReturnPolicyChoice)
                ? "Default"
                : request.ReturnPolicyChoice.Trim();
            effectivePolicy = string.Equals(raw, "Custom", StringComparison.OrdinalIgnoreCase)
                ? "Custom"
                : string.Equals(raw, "NonReturnable", StringComparison.OrdinalIgnoreCase)
                    ? "NonReturnable"
                    : "Default";
            effectiveCustomWindow = effectivePolicy == "Custom" ? request.CustomReturnWindowDays : null;
        }

        var canonical = new StringBuilder()
            .Append(Version)
            .Append('|').Append(request.SellerPartyId.ToString("N"))
            .Append('|').Append(request.CatalogVariantId.ToString("N"))
            .Append('|').Append(((int)channel).ToString(CultureInfo.InvariantCulture))
            .Append('|').Append(TextField(sku))
            .Append('|').Append(effectiveStatus)
            .Append('|').Append(effectivePolicy)
            .Append('|').Append(effectiveCustomWindow?.ToString(CultureInfo.InvariantCulture) ?? "N")
            .Append('|').Append(DecimalField(request.MinimumOrderQuantity))
            .Append('|').Append(DecimalField(request.MaximumOrderQuantity))
            .Append('|').Append(TextField(requestId.Trim()))
            .ToString();

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    // Length-prefixed so no separator can be smuggled into a value and collide two different intents.
    private static string TextField(string? value) =>
        value is null ? "N" : $"S{value.Length.ToString(CultureInfo.InvariantCulture)}:{value}";

    // Canonical decimal: strip trailing zeros so 10, 10.0 and 10.00 hash identically.
    private static string DecimalField(decimal? value) =>
        value is null
            ? "N"
            : "D" + value.Value.ToString("0.############################", CultureInfo.InvariantCulture);
}
