# Test Data Plan (next implementation phase — do not insert now)

Idempotent seed (Host Development seed or dedicated demo reset endpoint), using **existing** seeded products/offers/prices/stock.

## Campaigns

1. Active AMAZING (`StartAt` past, `EndAt` future)
2. Future/teasing AMAZING (`StartAt` tomorrow)
3. Expired AMAZING (`EndAt` past)

## Memberships (active campaign)

- ≥4 real Offers from existing catalog with Active status + Base prices + stock > 0
- Different discount depths via different `PromoAmount`
- One nearly-out stock Offer
- One out-of-stock / Suspended Offer that **must be filtered**
- One with explicit high SortOrder priority
- If Marketplace demo data has multiple sellers, include ≥2 sellers; else Single-Store still valid
- One membership exercising Offer max order qty if present

## i18n

- Campaign translations FA + EN for title/badge

## Proof expectations

- Active source returns only marketable active-window members
- Teasing not in default active storefront source
- Expired absent
- Deterministic order
- No frontend-only fake cards for final integration proof
