# Cart → Pricing Contracts — TB-TMAR-CONTRACTS-W6

## Before
Cart.Application → Pricing.Application

## After
Cart.Application → Pricing.Contracts

## Extended Pricing.Contracts
- `ICampaignCartPriceAuthority` (moved from Pricing.Application)
- `CurrencyCode` (Contracts assembly; Domain TypeForwardedTo)

Cart.Infrastructure continues using `IPriceLookupGateway` + `ICampaignCartPriceAuthority` from Contracts; AuthoredPrice ownership remains in Pricing.
