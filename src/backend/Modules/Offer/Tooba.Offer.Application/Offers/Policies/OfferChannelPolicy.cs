using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Offer.Application.Offers.Policies;

/// <summary>
/// Offer-owned authority for the sales channel of an installation/store.
/// The configured StoreCommerce.SalesChannel is the only authority; the enum is vocabulary,
/// never a client-supplied grant.
/// </summary>
public static class OfferChannelPolicy
{
    /// <summary>Returns the channel allowed by the resolved StoreCommerce context, if valid.</summary>
    public static IReadOnlyList<SalesChannel> Allowed(string? configuredChannel)
    {
        if (string.IsNullOrWhiteSpace(configuredChannel)) return [];
        var value = configuredChannel.Trim();
        if (!Enum.GetNames<SalesChannel>().Any(name => string.Equals(name, value, StringComparison.OrdinalIgnoreCase)))
            return [];
        return Enum.Parse<SalesChannel>(value, ignoreCase: true) is var channel ? [channel] : [];
    }

    /// <summary>True only when the configured store channel equals the requested channel.</summary>
    public static bool IsAllowed(string? configuredChannel, SalesChannel channel)
        => Allowed(configuredChannel).Contains(channel);

    /// <summary>The configured channel, or null when absent/invalid.</summary>
    public static SalesChannel? Default(string? configuredChannel)
    {
        var allowed = Allowed(configuredChannel);
        return allowed.Count == 0 ? null : allowed[0];
    }
}
