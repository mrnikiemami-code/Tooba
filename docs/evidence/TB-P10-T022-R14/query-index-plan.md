# Query / Index Plan (proposal only — do not create yet)

## Target query

Active AMAZING offers for tenant store DB, locale Y, timestamp `now`, ordered by campaign priority + membership sort, only marketable offers.

## Shape

```
Campaign (type, status, window)
  ⋈ CampaignOffer (sort)
  ⋈ Offer (Active)
  ⋈ Price (current Base)
  ⋈ Stock (Available > 0)
  ⋈ Product/Variant/Media for card
  ⋈ CampaignTranslation (locale Y + fallback)
```

Paging: keyset or OFFSET on (priority, sort, offer_id). Cap take via Landing `take` (≤48).

## Proposed indexes (architecture only)

1. `campaigns (type_code, lifecycle_status, start_at, end_at)` or partial index where Published
2. `campaign_offers (campaign_id, sort_order)` INCLUDE offer_id
3. `campaign_offers (offer_id)` for reverse lookup / overlap checks
4. Reuse existing `prices (offer_id, market, channel, currency, qualifier_kind, valid_from)`
5. Reuse `stock_positions (offer_id, location_id)`

## Performance rules

- Avoid N+1: batch price + stock by OfferId lists
- Avoid full product scan: never start from `products` for Amazing
- Cache: short TTL page fragment / source result keyed by (tenant, locale, source, campaignId|active, take); invalidate on campaign membership write, price change, stock availability change
- SSR: resolve on Host publish/public composer path same as other ProductCollection sources

## Overlap policy (recommend)

Allow same Offer in multiple campaigns historically; **at most one active AMAZING membership display** — define deterministic winner (higher campaign priority, then sort, then offer id) in resolver.
