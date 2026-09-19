# TB-P10-T022-R18 — Pricing Discovery

## AuthoredPrice
- Entity: `Tooba.Pricing.Domain.AuthoredPrice`
- Table: `pricing.prices`
- Dimensions: OfferId, Market, Channel (SalesChannel), Currency, ValidFrom/ValidTo, Status, QualifierKind, QualifierKey

## Qualifier model (before R18)
- `PriceQualifierKind.Base` only; QualifierKey null
- Resolve/ResolveBatch filter Base + Active + effective window

## Precedence
- Exactly one effective Base per (Offer, Market, Channel, Currency) window
- Overlap on activate rejected

## Bulk resolver
- `IPriceLookupGateway.ResolvePricesBatchAsync` used by MerchandisingCampaignQuery / Product Showcase

## Effective selling (R17)
- Campaign members projected Base amount only; ProductCard promo fields unused for campaigns (LOCK-SF-411)

## Safe extension
- Add `PriceQualifierKind.MerchandisingCampaign` with QualifierKey = CampaignId ("D")
- Keep Base resolver unchanged; add `ResolveCampaignPricesBatchAsync`
