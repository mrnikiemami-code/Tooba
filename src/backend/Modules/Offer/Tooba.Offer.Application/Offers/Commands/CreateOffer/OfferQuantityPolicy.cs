using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Contracts.Ports;
using Tooba.Offer.Contracts.Errors;

namespace Tooba.Offer.Application.Offers.Commands.CreateOffer;

/// <summary>Validates offer quantity limits against Catalog unit precision and step.</summary>
internal static class OfferQuantityPolicy
{
    public static SemanticError? Validate(decimal? minimum, decimal? maximum, CatalogVariantLookupResult? variant)
    {
        if (variant is null) return null;
        var decimalPlaces = Math.Max(0, variant.QuantityDecimalPlaces);
        var step = variant.QuantityStep is > 0 ? variant.QuantityStep : null;
        if (minimum is decimal min && !Aligned(min, decimalPlaces, step))
            return new SemanticError(OfferErrorCodes.MinQuantityInvalid);
        if (maximum is decimal max && !Aligned(max, decimalPlaces, step))
            return new SemanticError(OfferErrorCodes.MaxQuantityInvalid);
        return null;
    }

    private static bool Aligned(decimal value, int decimalPlaces, decimal? step)
    {
        if (value <= 0) return false;
        var rounded = decimal.Round(value, decimalPlaces, MidpointRounding.AwayFromZero);
        if (rounded != value) return false;
        if (step is decimal unit && unit > 0)
        {
            var quotient = value / unit;
            if (quotient != decimal.Truncate(quotient)) return false;
        }
        return true;
    }
}
