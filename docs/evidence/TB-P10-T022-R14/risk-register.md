# Risk Register

| Risk | Mitigation |
|---|---|
| Duplicating pricing truth | Membership promo amount only; list from AuthoredPrice |
| Duplicating inventory truth | Read Available only |
| Campaign/Offer circular ownership | Campaign owns membership; Offer unaware of Amazing |
| Stale cache | Invalidate on campaign/price/stock |
| Timezone / UTC ambiguity | Store UTC; derive UI in local tz at edges |
| Overlapping campaigns / multi active AMAZING | Deterministic winner rule |
| Same Offer in multiple active campaigns | Resolver picks one; admin warning later |
| Seller eligibility | Filter Active Offer + channel |
| Store scope leakage | Tenant DB boundary |
| Single-Store vs Marketplace leakage | Same schema; UX gating by edition |
| Translation fallback | Page locale → default locale |
| Expired/teasing leak | Time predicates on every public query |
| N+1 / full scans | Batch joins; start from membership |
| Hardcoded AMAZING / enum explosion | TypeCode registry; one source key |
| Boolean explosion IsAmazing | Forbidden |
| Campaign delete/history loss | Soft archive; keep membership history |
| Source config breaking pages | Additive ProductSources; validate unknown |
| Overloading checkout Promotion | Separate merchandising aggregate |
