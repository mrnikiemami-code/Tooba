# known-residuals

## Architect-reported seam
OfferSellerEndpoints caught InvalidOperationException when ex.Message is "offer.not_found".

## Producers (pre-repair)
- PriceDirectory.SetPriceAsync threw InvalidOperationException("offer.not_found")
- InventoryDirectory.SetInventoryAsync threw InvalidOperationException("offer.not_found")
- amount/quantity also used string InvalidOperationException codes

## Repair
SemanticException + OfferErrorCodes.NotFound / PricingErrorCodes.AmountInvalid / InventoryErrorCodes.QuantityInvalid.
Removed string-matched catch from Offer endpoints.
