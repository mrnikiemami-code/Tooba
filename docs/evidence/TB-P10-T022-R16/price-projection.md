# TB-P10-T022-R16 — Price Projection

## Path

`IPriceLookupGateway.ResolvePricesBatchAsync(offerIds, market, channel, currency, at)`

Defaults: Market=`IR`, Channel=`Marketplace`, Currency=`IRR` (`MerchandisingPriceScope.Default`).

Selects Active Base `AuthoredPrice` effective at `now` — same rules as `ResolvePriceAsync`.

## Forbidden (not implemented)

- PromoAmount scalar
- Fabricated discount %
- Compare-at invention
- Campaign-scoped price override (still deferred via `IMerchandisingCampaignPromoPrice`)
