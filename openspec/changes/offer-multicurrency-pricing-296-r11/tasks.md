# Tasks — offer-multicurrency-pricing-296-r11

- [ ] MC-01 Pricing authoritative currency reference (`Pricing:Currencies` config + `ICurrencyReferenceReader`) and unknown-currency fail-closed.
- [ ] MC-02 Pricing `SetPriceAsync` accepts an active system currency, uses the trusted store market, and `ListBasePricesAsync` returns per-currency current base rows with revisions.
- [ ] MC-03 IRR integer enforcement server-side (Money scale) and in the shared amount input/validation.
- [ ] MC-04 Offer read model exposes `Prices`; Offer currencies query/endpoints for admin and seller.
- [ ] MC-05 Shared pricing tab: multi-price table + currency selector + amount editor; remove the single read-only currency field; preserve UAT corrections.
- [ ] MC-06 Frontend adapters/model/actions: per-currency revision/dirty/save verification; registry messages fa/en/ru; RTL.
- [ ] MC-07 Focused backend/frontend tests + mounted regression; fix failures.
- [ ] MC-08 Migration only if a schema change is actually required; otherwise record "not required" (no new table in this design).
- [ ] MC-09 Build/serve Host 5088 + Next 3000; real Playwright/HTTP/PostgreSQL proof of two-currency prices, reload, IRR integer, locale/RTL, and prior offer regressions.
- [ ] MC-10 Evidence, commit and push; STOP_FOR_ARCHITECT (no self-acceptance).
