# 04 — Selected commercial gap plan (TB-P06-T018)

## Selection rule

Maximum safe set of **small/medium** presentation-critical gaps that can close without inventing Host modules or fake mutations.

Priority applied:

1. Visible Shopeiva routes that are not honestly live  
2. Backend capability with missing/honest UI (seller operational settings)  
3. Small foundations only if required — **none selected** (notifications / tickets deferred)  
4. Hide/defer large future features

## Selected for Wave 1

### Customer

| Gap | Approach | Out of scope |
|---|---|---|
| Primary nav advertises wallet/tickets/gift-cards/notifications | Hide from primary nav; keep deep-link capability shells | Implementing Host wallet/tickets/notifications/gift-cards |
| Settings honesty | Live profile bridge + locale cookie preference; security/notification prefs marked unavailable | Fake security save / notification preference persistence |
| Dashboard quick actions | Only live routes; settings live; wallet removed | Wallet tile |

### Seller

| Gap | Approach | Out of scope |
|---|---|---|
| Primary nav advertises customers/coupons/reviews/tickets/gift-cards | Hide from primary nav | Coupons CRUD, reviews, customers module, tickets, gift-cards |
| Settings stub / N/A | Live operational page from seller dashboard API | Business profile edit / fake save |
| Dashboard settings tile | Include live settings; remove stub N/A | Fake charts/counts |

### Admin

| Gap | Approach | Out of scope |
|---|---|---|
| Settings in primary nav without module | Hide settings from nav | Admin settings module implementation |

## Explicitly NOT selected

| Item | Why deferred |
|---|---|
| Notifications foundation | Large new Host domain (recipient/type/read-unread/tenant); not required to restore panel honesty |
| Support/Tickets foundation | Large new Host domain (thread/status/reply); no commercial Host owner yet |
| Customer wallet / gift-cards | No Host ledger / gift-card APIs |
| Seller coupons / reviews / customers | Need correct Promotion/Reviews owners + permissions; medium-large |
| Admin settings module | Infra/tenant settings not commercially required for Wave 1 demo honesty |
| Storefront visual ACCEPT | Separate critical-storefront visual review track |

## Honesty commitment

Do **not** fake implementation to raise readiness percentages. Wave 1 raises Customer/Seller/Admin via **nav honesty + live settings subsets only**.
