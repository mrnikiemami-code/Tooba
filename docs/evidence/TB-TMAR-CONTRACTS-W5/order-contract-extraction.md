# Order Pricing contract extraction — TB-TMAR-CONTRACTS-W5

## Created
`Tooba.Pricing.Contracts` (references Offer.Contracts for SalesChannel)

## Moved
- `IPriceLookupGateway`
- `PriceQuote`
- `PriceResolutionQuery`

## Retained in Pricing.Application
`IPriceDirectory`, `IPricingUseCaseGuard`, `ICampaignCartPriceAuthority`

## Direction
Order.Application → Pricing.Contracts (not Pricing.Application)
Consumers updated to `using Tooba.Pricing.Contracts` for lookup types.
