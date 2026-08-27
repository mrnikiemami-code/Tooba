# 02 — Panel gap input matrix (TB-P06-T018)

Sources:

- `docs/evidence/TB-P06-T014/18-commercial-readiness-after-gap-closure.md`
- TB-P06-T017 Story live acceptance (~85% Story readiness)
- Live panel shells / capability routes at claim

## Commercial readiness entering Wave 1

| Surface | T014 status | Est. % entering Wave 1 | Notes |
|---|---|---|---|
| Storefront | LIVE_AUDITED | ~85% | Visual review still open; coupon apply deferred |
| Customer | LIVE_AUDITED | ~75% | Wallet / tickets / gift-cards / notifications deferred; settings partial |
| Seller | LIVE_AUDITED | ~70% | Customers / coupons / reviews / tickets / gift-cards / settings honesty gaps |
| Admin | LIVE_AUDITED | ~80% | Settings module deferred; nav still advertised settings |
| Blog/Content | LIVE | ~90% | Engagement likes/views not ported |
| Story | LIVE (T017) | ~85% | Exact Shopeiva Story UI + live Host; not full product ready |

## Panel capability matrix (entering Wave 1)

### Customer

| Capability | Class | Notes |
|---|---|---|
| Dashboard | LIVE | Live order/wishlist metrics; quick actions mixed live/stub |
| Orders | LIVE | Host-backed |
| Addresses | LIVE | Host-backed |
| Wishlist | LIVE / PARTIAL | Depends on wishlist availability flag |
| Profile | LIVE | `/v1/customer/profile` |
| Settings | PARTIAL | Profile bridge needed; security/notification prefs unavailable |
| Wallet | DEFERRED | No Host wallet ledger for customer |
| Tickets/Support | DEFERRED | No Host tickets |
| Notifications | DEFERRED | No Host notification inbox |
| Gift Cards | DEFERRED | No Host gift-cards |
| Reviews (customer) | DEFERRED / N/A for Wave 1 | Not selected |

### Seller

| Capability | Class | Notes |
|---|---|---|
| Dashboard | LIVE | Operational metrics from seller API |
| Products / Orders / Fulfillments / Returns | LIVE | Host-backed |
| Analytics | LIVE / PARTIAL | Live metrics; chart series still limited |
| Wallet | LIVE | Seller settlement/wallet path exists |
| Settings | PARTIAL → Wave 1 target | Must become live operational page (no fake business profile save) |
| Customers | DEFERRED | No Host seller-customers module |
| Coupons/Discounts | DEFERRED | Promotion owner not selected this wave |
| Reviews | DEFERRED | Seller review response not selected |
| Tickets/Support | DEFERRED | No Host tickets |
| Gift Cards | DEFERRED | No Host gift-cards |

### Admin

| Capability | Class | Notes |
|---|---|---|
| Dashboard / catalog / orders / fulfillments / returns | LIVE | Operational |
| Settlement / payouts | LIVE | T012 |
| Content / stories / page composition | LIVE | T013 / T017 / T015 |
| Sellers / customers / reviews | LIVE | Existing admin surfaces |
| Settings | MISSING / DEFERRED | Route honest-unavailable; hide from primary nav in Wave 1 |

## Classification legend

- **LIVE** — visible, Host-backed, no fake mutation
- **PARTIAL** — real shell or subset live; remaining prefs/actions honestly unavailable
- **MISSING** — Shopeiva has it; Tooba lacks commercial module
- **DEFERRED** — intentionally out of Wave 1 (large foundation or no Host)
