# Delivery minimum formula

```text
minimumDeliveryDate = today(UTC date)
  + max(sellerPreparationDays for each distinct SellerPartyId in cart)
  + methodLeadDays(selected Store-enabled method)
```

- `sellerPreparationDays` = `ShippingMethodsOptions.SellerPreparationDaysByPartyId[seller]` or `DefaultSellerPreparationDays` (default 1).
- `methodLeadDays` from `ShippingMethodsOptions.Rates` matched by full method code (`post:express`) then service code (`post`).
- Multi-seller: **max** preparation across sellers (slowest readiness wins).
- Customer may select any date **≥** minimum; backend rejects earlier with `shipping.delivery.too_early`.
- Time windows are day-part estimates (9–12 / 12–15 / 15–18 / 18–21); no fabricated hourly calendar infrastructure.
