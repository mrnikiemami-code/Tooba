# Tooba — Pricing Foundation

Status:

```text
IN_PROGRESS — TB-P03-T003 awaiting Architect ACCEPT
```

Task:

```text
TB-P03-T003
```

## Purpose

Pricing owns authored commercial money for an Offer in a Market, SalesChannel, and Currency. It is not a column on Product or Offer, not inventory, and not a tax engine.

## Locked separations

```text
Product.Price is forbidden
Offer.Price is forbidden
Locale != Market != Currency != Tax Jurisdiction
Authored Price != FX-derived display price
Price exists != purchasable
```

Base authored amount is **tax exclusive**. VAT is not stored inside Amount. Iranian display Toman is not a stored currency; authored IRR stays IRR.

Rounding uses `MidpointRounding.AwayFromZero` at the currency scale (IRR: 0 fraction digits). IEEE float is not used.

## Selection

`ResolvePriceAsync` matches:

- OfferId
- Market
- canonical `SalesChannel` from Offer
- Currency
- Active status
- ValidFrom/ValidTo window at the requested Instant (UTC)

Optional customer, organization, and quantity sit on the query as **seams** and are not required in persistence yet. QualifierKind=Base is the only implemented book row.

Two Active Base prices for the same Offer+Market+Channel+Currency with overlapping windows are rejected.

## Persistence

`PricingDbContext` owns schema `pricing`. Offer existence is validated through `IOfferLookupGateway`. No FK to offer/catalog schemas. Marketplace data lives on the marketplace database; Single-Store data lives on the tenant database.

## Events

- `pricing.price_created.v1`
- `pricing.price_activated.v1`
- `pricing.price_changed.v1`
- `pricing.price_expired.v1`

## Out of scope

Tax calculation, promotions, FX conversion service, full B2B tiers/contracts, cart, order, payment, Buy Box, commercial UI, TB-P03-T004, P03 Gate.
