# 21 — Commercial readiness after panel Wave 1 (TB-P06-T018)

Honest recalculation after Wave 1 (nav honesty + live settings subsets). **Do not claim `PRODUCT_FULLY_READY`.**

| Surface | Est. readiness | Delta vs entering Wave 1 | Notes |
|---|---|---|---|
| Storefront | ~85% | flat | Visual review still open; coupon apply deferred |
| Customer | ~82% | from ~75% | Nav honesty + settings profile bridge / locale cookie |
| Seller | ~78% | from ~70% | Nav honesty + live operational settings; no coupons/reviews yet |
| Admin | ~82% | from ~80% | Settings hidden from nav (module still deferred) |
| Blog | ~90% | flat | Engagement likes/views not ported |
| Story | ~85% | from T017 | Live Host + exact Shopeiva UI |

## Sale / demo blockers

- Critical storefront **visual review still open** (functional PASS ≠ Visual ACCEPT).
- Deferred customer **wallet / tickets** (and related honesty shells) still missing for full Shopeiva account parity.
- Seller **coupons / reviews** and related commercial CRM still deferred.
- Admin **settings module** still deferred (honestly hidden).

## Next largest UI gaps

1. Coupons / discounts (correct Promotion/Pricing owner)  
2. Notifications foundation + inbox UI  
3. Support / tickets foundation + inbox UI  

## Backend-only gaps affecting presentation

- Customer wallet ledger / gift-cards Host APIs  
- Notifications Host module  
- Tickets Host module  
- Seller business profile edit API  
- Admin tenant/settings module  

## Intentionally deferred (Wave 1)

- Customer wallet / tickets / notifications / gift-cards (no Host)  
- Seller customers / coupons / reviews / tickets / gift-cards; business profile edit  
- Admin settings module  
- Notifications foundation **NOT selected**  
- Support/Tickets foundation **NOT selected**  

## Worker may report

`COMMERCIAL_PANEL_WAVE1_LIVE`

Must **not** report `PRODUCT_FULLY_READY`.
