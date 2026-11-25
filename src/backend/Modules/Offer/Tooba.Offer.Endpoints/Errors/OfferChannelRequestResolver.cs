using Tooba.BuildingBlocks;
using Tooba.Offer.Application.Offers.Policies;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Errors;

namespace Tooba.Offer.Endpoints.Errors;

/// <summary>
/// Resolves a transport-supplied channel name against the Offer-owned edition policy.
/// Enum names are accepted case-insensitively; a missing value uses the edition default
/// (never a hardcoded Marketplace) and an unpermitted value becomes a stable semantic error.
/// </summary>
public static class OfferChannelRequestResolver
{
    /// <summary>Resolves a concrete allowed channel for creation, defaulting from the edition when absent.</summary>
    /// <param name="raw">Raw transport value; may be null.</param>
    /// <param name="edition">Trusted installation edition.</param>
    /// <exception cref="SemanticException">No channel is allowed, or the requested channel is not a permitted name.</exception>
    public static SalesChannel ResolveRequired(string? raw, string? configuredChannel)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return OfferChannelPolicy.Default(configuredChannel)
                ?? throw new SemanticException(new SemanticError(OfferErrorCodes.ChannelRequired));
        }

        if (!TryParseName(raw, out var channel) || !OfferChannelPolicy.IsAllowed(configuredChannel, channel))
        {
            throw new SemanticException(new SemanticError(OfferErrorCodes.ChannelNotAllowed));
        }

        return channel;
    }

    /// <summary>
    /// Parses an optional channel for duplicate lookup. A missing value returns <c>null</c> so the
    /// query applies the same edition default; an invalid or unpermitted value fails closed.
    /// </summary>
    /// <param name="raw">Raw transport value; may be null.</param>
    /// <param name="edition">Trusted installation edition.</param>
    /// <exception cref="SemanticException">The requested channel is not a permitted name.</exception>
    public static SalesChannel? ResolveOptional(string? raw, string? configuredChannel)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (!TryParseName(raw, out var channel) || !OfferChannelPolicy.IsAllowed(configuredChannel, channel))
        {
            throw new SemanticException(new SemanticError(OfferErrorCodes.ChannelNotAllowed));
        }

        return channel;
    }

    private static bool TryParseName(string raw, out SalesChannel channel)
    {
        var candidate = raw.Trim();
        foreach (var value in Enum.GetValues<SalesChannel>())
        {
            if (string.Equals(value.ToString(), candidate, StringComparison.OrdinalIgnoreCase))
            {
                channel = value;
                return true;
            }
        }

        channel = default;
        return false;
    }
}
