using System.Globalization;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation.Errors;

namespace Tooba.Offer.Application.Offers.Ports;

/// <summary>
/// Compares a caller's viewed Offer version with the persisted version before an owner write.
/// The version is the Offer aggregate's UpdatedAt instant; an omitted version keeps legacy clients
/// compatible, while a supplied blank/stale/malformed value fails closed with a 409 conflict.
/// </summary>
public static class OfferVersionGuard
{
    /// <summary>Returns a conflict error when the supplied version is not exactly current.</summary>
    public static SemanticError? Check(DateTimeOffset actual, string? expectedVersion)
    {
        if (expectedVersion is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(expectedVersion) || !DateTimeOffset.TryParse(
                expectedVersion,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var expected))
        {
            return new SemanticError(FoundationErrorCodes.ConcurrencyConflict);
        }

        return expected.UtcTicks / 10 == actual.UtcTicks / 10
            ? null
            : new SemanticError(FoundationErrorCodes.ConcurrencyConflict);
    }
}
