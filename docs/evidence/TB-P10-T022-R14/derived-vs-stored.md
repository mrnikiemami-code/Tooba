# Derived vs Stored

| Field | Ownership | Notes |
|---|---|---|
| Countdown / timer | **Derived** | `EndAt - UtcNow`; never store ticking value |
| Discount percent | **Derived** | From list vs promo selling when both known |
| Active state | **Derived preferred** | Time predicates + Published; optional materialized status for admin lists later |
| Sold percentage | **Derived later** | From allocation sold / quota when allocation exists; omit until then |
| Marketable stock | **Reuse Inventory** | Sum/filter `Available` by Offer |
| Badge text | **Stored translated** | Campaign translation / presentation |
| List / original price | **Reuse Pricing** | Active `AuthoredPrice.Amount` |
| Promotional selling price | **Stored on membership (recommended)** | `PromoAmount` during campaign; do not mutate canonical price history silently |
| Min/max order qty | **Reuse Offer limits** | Membership override = LATER |
| IsAmazing | **Not stored** | Membership in AMAZING campaign implies it |
| Teasing | **Derived (+ optional flag)** | `StartAt > now` and published/teasing-visible |

Do not duplicate canonical pricing or inventory tables.
