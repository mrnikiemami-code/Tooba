# 18 — Commercial readiness after gap closure (TB-P06-T014)

| Surface | Status | Est. readiness | Sale/demo blockers |
|---|---|---|---|
| Storefront | LIVE_AUDITED | ~85% | Visual review open; coupon apply deferred |
| Customer | LIVE_AUDITED | ~75% | Wallet/tickets/gift-cards/notifications deferred |
| Seller | LIVE_AUDITED | ~70% | Customers/coupons/reviews/settings charts deferred |
| Admin | LIVE_AUDITED | ~80% | Settings deferred |
| Blog/Content | LIVE | ~90% | Engagement likes/views not ported |

## Blocking for polished demo

- Remaining honest stubs (no fake fill)
- Full English string catalog for panels
- Locale-prefixed public routing ADR productization
- hreflang when second locale published

## Non-blocking enhancements

- Seller coupon CRUD
- Customer wallet ledger
- Admin settings module
- Chart visualizations when metrics series exist

Do **NOT** claim `PRODUCT_FULLY_READY`.
Worker may report: `COMMERCIAL_UI_GAPS_CLOSED_AND_I18N_AUDITED`.
